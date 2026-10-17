# Durum, aktör ve olay sözleşmesi — #5

Bu matris mevcut MSSQL CHECK değerlerini kullanır. Tabloda olmayan geçiş reddedilir.
İş kuralları ilgili modülün komut/prosedüründe uygulanır; bu belge bir state-machine
implementasyonu değildir. Dış istemci serbest `newStatus` / `source` göndermez.
Aktör kimliği token'dan, event aktörü yalnızca güvenilir outbox handler'ından gelir.

## Sipariş — Ordering #26/#28/#33/#35

| Başlangıç → sonuç | Aktör / tetikleyici | Zorunlu koşul |
| --- | --- | --- |
| Yeni → AWAITING_PICKUP_ASSIGNMENT | Müşteri checkout; OrderPlaced | Tek store, sahip olunan adres, uygun hizmetler, değişmez fiyat/adres kopyası |
| AWAITING_PICKUP_ASSIGNMENT → PICKUP_ASSIGNED | DeliveryTaskAssigned / CUSTOMER_PICKUP | Gerçek kurye ataması ve kapasite rezervasyonu |
| PICKUP_ASSIGNED → PICKED_UP | DeliveryTaskStarted / CUSTOMER_PICKUP | Mallar müşteriden alınmış |
| PICKED_UP → AT_STORE | PickupAtStore | Alım görevi COMPLETED |
| AT_STORE → INSPECTING | İlk ürün incelemesi / order.inspect | Store kapsamı; inspection writer aynı transaction'da |
| INSPECTING → AWAITING_STORE_APPROVAL | AllItemsInspected | Tüm fiziksel ürünler incelenmiş ve ACCEPTED |
| AWAITING_STORE_APPROVAL → AWAITING_CUSTOMER_APPROVAL | Yetkili / quote.approve | Eksiksiz tek güncel teklif PUBLISHED |
| AWAITING_CUSTOMER_APPROVAL → AWAITING_STORE_APPROVAL | Yetkili / quote.prepare ile yeni revizyon | Henüz müşteri kabulü/ödeme yok; eski aktif teklif SUPERSEDED veya süresi dolmuş EXPIRED |
| AWAITING_CUSTOMER_APPROVAL → AWAITING_PAYMENT | Sipariş sahibi kabulü | Güncel, süresi dolmamış teklif; tutar değişmez |
| AWAITING_CUSTOMER_APPROVAL → RETURN_REQUESTED | Sipariş sahibi reddi / QuoteRejected | Tam sipariş reddi; ödeme/iş başlatılmaz |
| AWAITING_PAYMENT → PROCESSING | Doğrulanmış başarılı ödeme / PaymentSucceeded | Kabul edilen quote, TRY ve tutar eşleşir; idempotent |
| PROCESSING → READY_FOR_DELIVERY | AllWorkCompleted | Her beklenen hizmetin işi COMPLETED; boş küme tamamlanma değildir |
| READY_FOR_DELIVERY → DELIVERY_ASSIGNED | DeliveryTaskAssigned / CUSTOMER_DELIVERY | Normal teslim görevi ve kurye rezervasyonu |
| DELIVERY_ASSIGNED → OUT_FOR_DELIVERY | DeliveryTaskStarted / CUSTOMER_DELIVERY | Kurye malları dükkândan aldı |
| OUT_FOR_DELIVERY → DELIVERED | DeliveryCompleted | Sadece CUSTOMER_DELIVERY görevi COMPLETED |
| RETURN_REQUESTED → RETURN_ASSIGNED | DeliveryTaskAssigned / REJECTED_ORDER_RETURN | Bütün ürünler için tek iade görevi |
| RETURN_ASSIGNED → RETURN_IN_TRANSIT | DeliveryTaskStarted / REJECTED_ORDER_RETURN | Kurye bütün ürünleri dükkândan aldı |
| RETURN_IN_TRANSIT → RETURNED | ReturnCompleted | İade görevi COMPLETED; DeliveryCompleted kullanılmaz |

DELIVERED/RETURNED/CANCELLED terminaldir. MVP'de genel müşteri iptal endpoint'i yok;
CANCELLED şemada ayrılmıştır. Checkout öncesi sepet terk edilebilir. Checkout sonrası
iptal, ücret/iade ve mevcut görev iptali birlikte tasarlanmadan etkinleştirilmez.
Kurye atama penceresinin geçmesi kendiliğinden siparişi iptal etmez; Operations
bekleyen görevi yeniden atama sürecine alır (#30).

Her geçiş beklenen status/version ile koşullu yazılır; gerçek değişimde history
ve version artışı aynı transaction'dadır. Worker kullanıcı izni taklit etmez,
kendi event tipine ve mevcut iş durumuna bağlı komutu çağırır. Kullanıcının
`order.fulfill` izni AllWorkCompleted önkoşulunu atlamasına izin vermez.

## Teklif — Ordering #33/#35

| Başlangıç → sonuç | Aktör / koşul |
| --- | --- |
| Yeni → DRAFT | quote.prepare; bütün ürünler kabul edilmiş; ödeme başlamamış |
| DRAFT → PENDING_STORE_APPROVAL | quote.prepare; her seçili hizmete tek satır, geçerli miktar/ücret |
| PENDING_STORE_APPROVAL → PUBLISHED | quote.approve; aynı kilit altında doğrulama; yeni fiyat müşteriye görünür |
| PUBLISHED → CUSTOMER_ACCEPTED | Sipariş sahibi, güncel revizyon, now < expires_at veya expires_at null |
| PUBLISHED → CUSTOMER_REJECTED | Aynı sahiplik/geçerlilik; tam sipariş iadesi |
| PUBLISHED → EXPIRED | Güvenilir süre kontrolü; now >= expires_at |
| DRAFT/PENDING_STORE_APPROVAL/PUBLISHED → SUPERSEDED | quote.prepare ile yeni revizyon; kabul/ödeme yok |

SUPERSEDED/EXPIRED/CUSTOMER_REJECTED sonrasında aynı teklif yeniden açılmaz.
CUSTOMER_ACCEPTED değişmez; fiyat revizyonu yapılmaz. Yeni revizyon yaratılırken
eski aktif teklif önce aynı transaction'da SUPERSEDED yapılır; tek güncel teklif
unique index'i korunur. Yeni revizyon siparişi AWAITING_STORE_APPROVAL'a döndürür;
yalnızca AWAITING_STORE_APPROVAL/AWAITING_CUSTOMER_APPROVAL durumlarında mümkündür.
EXPIRED, siparişi otomatik RETURN_REQUESTED yapmaz. Sipariş
AWAITING_CUSTOMER_APPROVAL'da yeni revizyon bekler; süresi geçmiş teklif kabul/ret
komutu 409 döner. MVP'de otomatik süre sonu iade politikası etkin değildir.

PENDING_STORE_APPROVAL/PUBLISHED teklifin satırları yerinde düzenlenemez. Teklif
kilidi düzenleme, onay, yayımlama, expiry ve müşteri kararı arasında ortaktır.
Kesin ödeme tutarı teklifin total_amount değeridir; ödeme anında Catalog fiyatı
yeniden hesaplanmaz. Başarısız ödeme siparişi AWAITING_PAYMENT'te bırakır.

## Kurye görevi — Operations #29/#30/#34

| Başlangıç → sonuç | Aktör / koşul | Olay |
| --- | --- | --- |
| Yeni → PENDING_ASSIGNMENT | OrderPlaced / OrderReadyForDelivery / QuoteRejected handler'ı | Yok |
| PENDING_ASSIGNMENT → ASSIGNED | Otomatik atayıcı; uygun vardiya, aktif kurye ve kapasite kilidi | DeliveryTaskAssigned |
| ASSIGNED → IN_TRANSIT | Atanmış kurye; MVP Development/Test mock komutu | DeliveryTaskStarted |
| IN_TRANSIT → COMPLETED | Atanmış kurye teslimi; kapasite bir kez serbest bırakılır | TaskType'a göre aşağıdaki tamamlanma olayı |

CUSTOMER_PICKUP → PickupAtStore, CUSTOMER_DELIVERY → DeliveryCompleted,
REJECTED_ORDER_RETURN → ReturnCompleted. Atama/başlama olaylarında TaskType tam
olarak DeliveryTaskTypes sabitlerinden biridir. Bilinmeyen değer reddedilir.
MVP mock uçları production'da route olarak açılmaz; Development/Test'te de kimlik
ve store kapsamlı order.fulfill gerekir. Kurye paneli MVP kapsamı dışındadır.

CANCELLED değeri ayrılmıştır; MVP'de genel iptal/yeniden atama endpoint'i yoktur.
Kurye bulunamayan görev PENDING_ASSIGNMENT kalır, atayıcı daha sonra tekrar dener.
Atanmış görevi iptal/yeniden atama gelecekte rezervasyonu, sipariş durumunu ve
olayları atomik değiştiren ayrı komut gerektirir. Tekrar olay veya worker denemesi
tamamlanmış bir görevden ikinci görev üretmez: açık-task unique index'i tek başına
yeterli değildir; tamamlanmış görevi de kapsayan idempotency kontrolü gerekir.

## Ürün inceleme ve iş — Operations #31/#38

İnceleme kaydı olmayan ürün PENDING sayılır. order.inspect ile her fiziksel ürün
ACCEPTED/REJECTED sonucuna gelir. Herhangi bir REJECTED ürün varken teklif yayımlanmaz;
AllItemsInspected başarı olayı yayımlanmaz. MVP'de bu durum manuel değerlendirmede
kalır; müşterinin tam teklif reddi dışındaki hasarlı ürün telafisi/iadesi ayrı iş
kararı ve denetimli komut gerektirir. Kısmi kabul/ürün eleme sessizce yapılmaz.

| Başlangıç → sonuç | Aktör / koşul |
| --- | --- |
| Yeni → PENDING | PaymentSucceeded handler'ı; gerçek başarılı ödeme ve her kabul edilmiş hizmet için tek iş |
| PENDING → IN_PROGRESS | Store kapsamlı order.process; ödeme hâlâ doğrulanmış |
| IN_PROGRESS → COMPLETED | order.process; iş fiilen tamamlanmış |
| IN_PROGRESS → FAILED | order.process; hata ve history kaydı |
| FAILED → IN_PROGRESS | order.process; denetimli yeniden deneme, aynı iş ID'si |

COMPLETED terminaldir. İş/geçmiş ve AllWorkCompleted outbox kaydı tek transaction'da
yazılır. Son işi tamamlayan yarışan iki işlem aynı siparişe ait işleri kilitleyerek
yalnızca bir tamamlanma olayı üretir. Tüm beklenen hizmet ID'leriyle eşleştirilir;
bir eksik item_work satırı varsa mevcut satırların hepsi COMPLETED olsa da olay yoktur.

## Olay teslimi ve tekrarlar

Mevcut sekiz v1 olayın payload/isimleri korunur. Eklenenler:

| EventType | Üreten | Tüketen |
| --- | --- | --- |
| operations.delivery_task_assigned.v1 | Operations #30 | Ordering #28 |
| operations.delivery_task_started.v1 | Operations #34 | Ordering #28 |
| operations.return_completed.v1 | Operations #34 | Ordering #28 |

OrderPlaced, OrderReadyForDelivery, QuoteRejected, PaymentSucceeded Ordering'den
Operations'a gider. PickupAtStore, AllItemsInspected, AllWorkCompleted,
DeliveryCompleted ve üç yeni olay Operations'tan Ordering'e gider.

EventId aynı olayın tekrarında korunur; yeni olay için yeni Guid üretilir.
OccurredAtUtc UTC'dir. JSON mevcut SqlOutboxWriter gibi PascalCase property adları
ile saklanır; HTTP JSON camelCase'tir. TaskId ve OrderId boş olamaz. Tüketici task
türünü, siparişle ilişkisini ve beklenen aşamayı doğrular; event DTO'su tek başına
public kullanıcı girdisi olarak kabul edilmez.

TaskId taşıyan olaylarda Ordering, `IDeliveryTaskReader` ile aynı transaction'da
görevin OrderId/TaskType bilgisini eşleştirir. Atama olayı işlendiğinde görev daha
ileri aşamaya geçmiş olabilir; ASSIGNED/IN_TRANSIT/COMPLETED kabul edilebilir,
başlama için IN_TRANSIT/COMPLETED gerekir. Tamamlanma olayında COMPLETED şarttır.
Siparişin mevcut aşaması yine yukarıdaki matrise göre denetlenir; görevin ilerlemiş
olması siparişte ara aşamaları atlama izni vermez.

Dispatcher'ın paralel worker'larında sıralama garantisi yoktur. Aynı olay tekrar
geldiyse yan etki yaratmadan başarılı no-op yapılır. Önkoşul olayından önce gelen
geçerli gelecek aşama olayı retry edilir, işlenmiş işaretlenmez. Eski aşama olayı
yalnızca kayıtlı history/task kanıtıyla daha önce uygulanmış olduğu doğrulanırsa
no-op olur; sadece status adına bakıp kayıp olayları yutmayın. İş verisi ve outbox
aynı transaction'dadır. Poison/retry/backoff uygulaması #32'ye aittir; henüz yoktur.
