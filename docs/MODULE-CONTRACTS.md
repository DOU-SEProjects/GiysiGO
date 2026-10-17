# Modüller arası sözleşmeler — #5

Bu belge implementasyon sözleşmesidir. Arayüz/DTO/olay tanımları bu branch'te vardır;
veritabanı implementasyonları, controller'lar ve handler'lar aşağıdaki issue'larda
modül sahiplerince yazılır. Bir sözleşmenin varlığı çalışan endpoint anlamına gelmez.
HTTP örneklerinin makine tarafından okunabilen kaynağı `openapi/mvp-contracts.json`,
durum ve olay matrisi `WORKFLOW-CONTRACTS.md` dosyasıdır.

## Sağlayan ve kullanan

| Sözleşme | Sağlayan / teslim | Kullanan / amaç |
| --- | --- | --- |
| `ICurrentUser` | Identity #10, JWT bağlama #13 | Kullanıcı komutlarında aktör kimliği |
| `IPermissionChecker` | Identity #10 | Catalog, Ordering, Operations: izin + şirket/dükkân kapsamı |
| `ICustomerAddressReader` | Identity #16 | Catalog keşif, Ordering sepet önizlemesi |
| `ITransactionalCustomerAddressReader` | Identity #16 | Ordering checkout adres snapshot'ı |
| `IStoreCatalogReader` | Catalog #21 | Liste/önizleme fiyat ve konum kontrolü |
| `ITransactionalStoreCatalogReader` | Catalog #21 | Checkout fiyatı ve görev başlangıcında dükkân konumu |
| `IStoreScopeReader` | Catalog #14/#21 | Identity #10/#15: dükkânın şirketi ve aktifliği |
| `IOrderPickupReader` | Ordering #26/#28 | Alım bilgisi önizlemesi |
| `ITransactionalOrderPickupReader` | Ordering #26/#28 | Operations #29: alım, teslim ve iade adres kopyaları |
| `IOrderOperationsReader` | Ordering #28 | Operations #31/#38: ürün inceleme ve ödenmiş hizmet listesi |
| `IOrderInspectionWriter` | Ordering #28, kullanan Operations #31 | İnceleme sonucunu Ordering alanına aynı transaction'da yansıtma |
| `IDeliveryTaskReader` | Operations #29/#34 | Ordering #28: kurye olayındaki task/order/type ilişkisini doğrulama |

Eski beş interface'in imzası ve mevcut snapshot constructor'ları korunur. Transaction
alan yeni arayüzler ayrı DI kayıtlarıdır; aynı implementasyon hem eski hem yeni
arayüzü sağlayabilir. Eski metot kendi bağlantısını açabilir; yeni metot verilen
bağlantıyı kullanır. Eski metodu yeni metottan çağırarak başka bağlantı açmak yasaktır.
Yeni arayüzlerin boş üretim implementasyonu eklenmez; her sağlayıcı kendi issue'sunda
kayıt ve davranış testlerini teslim eder.

## Kimlik ve izin davranışı

- `ICurrentUser.UserId`, doğrulanmış JWT `sub` claim'inin Guid karşılığıdır. Host #13'te
  bunu mevcut controller tabanının kullandığı `ClaimTypes.NameIdentifier` ile eşler.
  Anonimde `IsAuthenticated=false`, `UserId=null`; bozuk/eksik Guid token reddedilir.
- HTTP body/query'deki `userId`, `source=SYSTEM`, rol veya şirket ID'si aktör kimliği
  oluşturmaz. Worker'ın HttpContext'i yoktur; event handler ayrı bir güvenilir giriş
  kullanır. Genel HTTP durum-değiştir endpoint'i sistem işlemi kabul etmez.
- `HasPermissionAsync` bilinmeyen permission, pasif/silinmiş kullanıcı, silinmiş rol
  veya geçersiz kapsam için `false` döner. Kullanıcı yokluğu bilgi sızdırmaz.
- Store kapsamı verildiğinde Catalog'dan gerçek corporate bulunur; aynı istekte
  corporate da verilmişse eşleşmelidir. Aktif corporate, aktif store, aktif üyelik
  ve gerekli store ataması değerlendirilir. Corporate rolü başka şirkete taşmaz.
- Global PlatformAdmin yetkisi yalnızca seed'de o role verilmiş permission içindir;
  pasif mağazada yeni iş yaratma gibi iş kurallarını kaldırmaz. Kapsamsız çağrı sadece
  GLOBAL rolü kontrol eder; tüm şirket/dükkân yetkilerinin birleşimi değildir.
- `quote.prepare` ve `quote.approve` ayrıdır. Customer rolü personel izni sağlamaz.
- Kimliksiz HTTP 401, kimliği doğrulanmış ama izinsiz HTTP 403 alır. Müşteriye ait
  olmayan adres/sipariş/teklif ID'si için varlığı gizleyen 404 kullanılır. Dahili
  `HasPermissionAsync` bir HTTP cevabı üretmez.

## Okuma sonuçları

Tüm Guid parametreleri boş olamaz. Koordinatlar latitude [-90,90], longitude
[-180,180] aralığındadır. Geçersiz argüman `ArgumentException`/alt tipi; iptal
`OperationCanceledException`; DB hatası çağırana taşınır. Altyapı hatasını `null`,
`false` veya boş listeye çevirerek yetki/fiyat yokmuş gibi davranılmaz.

| Metot | Başarılı sonuç | Bulunamadı / pasif / kapsam dışı |
| --- | --- | --- |
| `GetOwnedAddressAsync` | Aynı aktif müşteriye ait, silinmemiş adres ve iletişim | `null`; başka müşterinin adresi de aynı sonuç |
| `GetOfferingAsync` | Aktif store + ürün tipi + hizmet + eşleşme + offering; override varsa o, yoksa default fiyat | `null`; çözümlenmemiş default fiyat aktif teklif değildir |
| `GetStoreLocationAsync` | Aktif store'un konumu; `IsActive=true` | `null` |
| `ServesLocationAsync` | Aktif store için küresel kuş uçuşu km uzaklığı ≤ hizmet yarıçapı | `false`; yol mesafesi değildir |
| `GetScopeAsync` | Silinmemiş store'un gerçek CorporateId'si; pasif store `IsActive=false` | Silinmiş/yok store `null`; Identity corporate aktifliğini ayrıca denetler |
| `GetPickupAsync` | Siparişin değişmez adres kopyası, store, fiziksel ürün kapasitesi ve alım penceresi | Yok/silinmiş sipariş `null`; durum geçişi uygunluğunu tüketici denetler |
| `GetOrderAsync` | İstenen store'a ait siparişin ürün/fotoğraf/hizmet snapshot'ları | Yok/silinmiş sipariş veya store uyuşmazlığı `null` |
| `GetTaskAsync` | Silinmemiş kurye görevinin order/type/status bilgisi | Yok/silinmiş görev `null`; terminal görevler tekrar-işleme kontrolü için görünür |

`GetPickupAsync` müşteri adres defterini yeniden okumaz. Aktif siparişin teslim/iade
işlemi müşterinin adresi silmesiyle durmamalıdır. `RequiredCapacityUnits`, fiziksel
ürün sayısıdır; bir ürünün üç hizmeti üç kurye kapasitesi tüketmez. Teslim/iade
görevi bu müşteri konumunu hedef olarak kullanır; alım zaman penceresini teslim/iade
penceresi diye tekrar kullanmaz (#29 kendi talep penceresini belirler). Görev
oluştuktan sonra dükkân taşınsa bile görevdeki adres snapshot'ı değişmez.

## Operations için sipariş görünümü

`OrderOperationsSnapshot` her fiziksel ürünü bir kez içerir; boş sipariş geçerli
operasyon girdisi değildir. Ürün fotoğrafları storage key taşır; kalıcı/public URL
üretilmez. HTTP fotoğraf erişimi store/müşteri yetkisini ayrıca doğrular. Listeler
materialize edilir; entity, lazy query veya Dapper nesnesi dışarı verilmez.

`AcceptedQuoteId` müşterinin kabul ettiği tekliften gelir. `IsPaid=true` ancak o
teklife ait başarılı ödeme varsa döner. Ödeme öncesinde `AcceptedQuantity` null
olabilir; ödeme sonrası hizmet başlatmak için tüm hizmetlerde pozitif miktar,
eşleşen kabul edilmiş teklif ve başarılı ödeme gerekir. Miktar güncel Catalog
tarifesinden değil kabul edilen teklif satırından gelir. `PIECE` miktarı 1,
`M2` miktarı doğrulanmış pozitif ölçüdür. İptal edilmiş hizmet iş listesine alınmaz.
İnceleme için fiziksel ürünler ödeme öncesinde de görünür.

Operations #38, `PaymentSucceededEvent.QuoteId` ile snapshot'ın `AcceptedQuoteId`
değerini eşleştirir; `IsPaid` ve `PROCESSING` durumunu doğrular. Her hizmet ID'si
için tek item_work oluşturur. Sipariş ID'si tek başına ödeme kanıtı değildir.

`IOrderInspectionWriter.RecordInspectionAsync` öncesinde Operations #31
`order.inspect` iznini ve store kapsamını kontrol eder. Ordering implementasyonu
item → order → store ilişkisini, AT_STORE/INSPECTING durumunu ve M2 ölçüsünü aynı
transaction'da doğrular; `inspection_status` ve `verified_measurement` alanlarını
günceller, ilk incelemede AT_STORE → INSPECTING geçişini/geçmişini yazar. Operations
kendi item_inspections kaydını ve gerekirse AllItemsInspected olayını aynı
transaction'da yazar. Aynı sonuç tekrarında no-op; çelişen sonuç 409'a eşlenen
çatışmadır. Yeniden inceleme için denetim geçmişi olan ayrı revizyon işi gerekir.
Bu metot fiyatı, rolü veya hizmetin tamamlanma durumunu değiştirmez.

## Transaction ve fiyat kuralları

1. Dış komut `ITransactionRunner` ile tek connection/transaction açar. Outbox
   handler'ı ise dispatcher'dan gelen çifti kullanır; tekrar runner çağırmaz.
2. Transaction alan public reader/writer ve repository'ler aynı nesneleri kullanır;
   yeni bağlantı/transaction açmaz, commit/rollback/dispose etmez. Bağlantı açık,
   `transaction.Connection == connection` olmalıdır; uyuşmazlık hata verir.
3. Her Dapper çağrısı `CommandType.StoredProcedure`, aynı transaction ve token ile
   çalışır. Modül yalnızca kendi SQL alanını okur/yazar; yabancı tablolara doğrudan
   sorgu yerine sağlayıcının public sözleşmesi çağrılır. FK'ler bu kuralı kaldırmaz.
4. Varsayılan READ COMMITTED aynı satırı iki kez aynı değerle okuyacağımızı garanti
   etmez. Checkout uygun adres ve teklifleri **bir kez materialize eder**; toplam
   ve satır snapshot'ları aynı veri setinden üretilir. Geçerlilik kontrolü ile
   commit arası gerekli kilitleri sağlayıcı prosedürleri tutar; ortak kilit sırası
   order/cart → identity address → catalog store → sıralı offering ID'leridir.
5. İş verisi + history + outbox tek transaction'dadır. Hata/iptal tümünü geri alır.
   Fiyat decimal, para TRY, kimlik Guid, zaman UTC'dir. Satır tutarı miktar × birim
   fiyat, iki haneye `MidpointRounding.AwayFromZero`; toplam yuvarlanmış satırların
   toplamıdır. Kesin teklifte ayrıca pickup_fee ve delivery_fee eklenir.
6. Idempotency sonucunu erken döndürmeden önce sahiplik kontrol edilir. Aynı anahtar
   farklı payload ile kullanılırsa 409; aynı yetkili istek önceki sonucu döndürür.

Fiyat/snapshot yazma SP parametreleri #26'da Ordering tarafından tanımlanır; JSON
veya TVP ile tek veri seti taşınabilir. Ortak sözleşme başka modülün tablosunu
yeniden okuyarak farklı fiyat alma izni vermez. Store/kullanıcı aktifliği yeni iş
yaratmayı sınırlar; eldeki malların teslim/iadesini yönetici üzerinden tamamlama
politikası #29/#34 kapsamında açıkça uygulanmalıdır.

## API ve istemci sahipliği

| Alan | Sahip |
| --- | --- |
| `src/API/GiysiGo.API/Controllers/Identity/`, `/auth`, `/me`, `/corporates` | @PnterNN |
| `src/API/GiysiGo.API/Controllers/Catalog/`, `/catalog`, `/stores` keşif/tarife | @sdnrkaleli |
| `src/API/GiysiGo.API/Controllers/Ordering/`, `/carts`, `/orders`, `/quotes`, `/payments` | @AhmetEfe0 |
| `src/API/GiysiGo.API/Controllers/Operations/`, `/delivery-tasks`, `/inspections`, `/work-items` | @selincellkk |
| `apps/customer/lib/features/identity/` | @PnterNN |
| `apps/customer/lib/features/catalog/` | @sdnrkaleli |
| `apps/customer/lib/features/ordering/` | @AhmetEfe0 |
| Flutter router, tema, API client ve oturum kabuğu (#18) | @PnterNN; tüketenler feature kayıtlarını verir |
| `apps/store/src/features/identity/` | @PnterNN |
| `apps/store/src/features/catalog/` | @sdnrkaleli |
| `apps/store/src/features/ordering/` | @AhmetEfe0 |
| `apps/store/src/features/operations/` | @selincellkk |
| React router, tema ve API client kabuğu (#22) | @sdnrkaleli; oturum sözleşmesi @PnterNN ile |
| Program.cs, controller tabanları, BuildingBlocks, CI, ortak OpenAPI | Koordinatör @PnterNN + etkilenen modül sahibi |

Controller'lar DTO kullanır. Generic CRUD tabanı mevcut kalır; kullanıcı parolası,
rol ataması, ödeme, sipariş/teklif durumu genel entity PUT ile açılamaz. Controller
dosyalarının alt klasörü URL prefix'ine eklenmez. Liste uçları `page` (1),
`pageSize` (20, en çok 100) ve `{ items, page, pageSize, totalCount }` kullanır.

HTTP 400 validation, 401 oturum, 403 permission, 404 kapsamda bulunamayan kaynak,
409 durum/version/idempotency çatışması, 500 beklenmeyen hatadır. Problem Details:
`type`, `title`, `status`, `detail`, `instance`, `code`, `traceId`; validation için
ek `errors` alanı. SQL metni, stack trace veya hassas bilgiler dönülmez. İstisna/SP
hata kodlarını bunlara dönüştürmek endpoint'i sağlayan modülün teslim kriteridir.
Frontend `code` üzerinden karar verir, Türkçe/İngilizce mesaj metnini parse etmez.

## Test double ve teslim planı

Her tüketici testinde yalnızca kullandığı interface'i taklit eder. Fake adres
okuyucu A kullanıcısı + A adresi için snapshot, B kullanıcısı için null döndürür.
Fake catalog aktif/kapalı hizmet ve yarıçap içi/dışı senaryolarını destekler.
Permission fake `(userId, permission, corporateId, storeId)` anahtarlı olur;
her çağrıya true dönen fake kapsam testlerinde kullanılmaz. Operations reader
fake'i ödeme öncesi, ödenmiş ve yanlış store senaryolarını ayrı verir. Fiyatlar
200/500 gibi sabit, ID/zaman deterministik seçilir.

Transaction alan fake'ler gelen connection/transaction referanslarını kaydeder;
tüketicinin aynı nesneleri aktardığı doğrulanır. Bu, MSSQL atomiklik testi yerine
geçmez. Kilit, yarışma ve rollback testleri modül sahibince gerçek test DB'sinde
yapılır. Üretim DI'sında fake kayıt bulunmaz. `tests/Contracts` wire format ve
OpenAPI sınırlarını korur; modül iş kurallarının tamamlandığını iddia etmez.

## PR #41 ile uyumluluk ve sonraki teslimler

Bu değişiklik sekiz eski event'i ve eski interface'leri değiştirmez. #41'in kaynak
koduyla derleme/test karşılaştırması yapılır. #26'da checkout transaction reader'lara
geçirilir; doğrudan identity/catalog SQL okumaları sağlayıcı sözleşmeleriyle
değiştirilir. #28'de yeni Operations event handler'ları, inspection writer ve
Operations reader yazılır. #29/#34 yeni kurye olaylarını, #31/#38 inceleme/iş
tamamlanma önkoşullarını uygular. `source=SYSTEM` HTTP'den alınamaz; #28 güvenilir
handler girişini kullanıcı komutundan ayırır. #32 otomatik worker/retry,
#40 gerçek modüller arası teslim/iade testlerini tamamlar.

OpenAPI dosyası bütün MVP endpoint'lerinin listesi değil, dört modülü kapsayan 16
örnek operasyonun bağlayıcı başlangıç sözleşmesidir. Yeni endpoint'ler aynı kurallarla
modül PR'ında eklenir. Örneklerin `x-implementation-status=planned` değeri mevcut
API'de çalıştıkları izlenimi verilmesini önler. `x-required-permission` ve
`x-environments` runtime güvenlik uygulaması değildir; controller/host bunları
ilgili issue'da gerçekten uygulamalı ve entegrasyon testleriyle doğrulamalıdır.
