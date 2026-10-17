# GiysiGO tablo rehberi

Modüllerin bu tablolara erişirken kullandığı public arayüzler
[modül sözleşmelerinde](MODULE-CONTRACTS.md), aktör ve izin verilen geçişler
[operasyon sözleşmesinde](WORKFLOW-CONTRACTS.md) tanımlıdır. Şemada iptal/yeniden
atama için alan bulunması, o komutun MVP'de etkin olduğu anlamına gelmez. Ürün
inceleme reddinin telafi/iade kararı henüz açık iş kararıdır; müşteri teklifinin
tam reddi ise tanımlanmış tam sipariş iade akışıdır.

Bu rehber SQL projesindeki **39 mevcut tabloyu** açıklar: Identity 9, Catalog 6, Ordering 14, Operations 9, building_blocks 1. Amaç, bir geliştiricinin bir kaydın neden ayrı tabloda tutulduğunu, hangi işlemde oluşturulduğunu ve hangi işlemde okunduğunu SQL dosyalarını tek tek çözmeden anlayabilmesidir. Kesin sütun türleri, CHECK/UNIQUE/FK ve indeksler için kaynak dosyalar database/GiysiGo.Database altındadır. Bu belge tasarlanan akışı anlatır; modül iş akışlarının tamamının kodlandığı anlamına gelmez.

Bütün tablolarda GUID id ile create_date, update_date ve delete_date bulunur. Zaman alanları uygulamada UTC kabul edilir. delete_date genel yumuşak silme altyapısıdır; sipariş, ödeme, teklif ve geçmiş kayıtları normal kullanıcı CRUD'u ile silinmemelidir. is_active veya status ayrı bir kavramdır: kayıt veritabanında dururken hizmete/işleme açık olup olmadığını belirtir. FK'ler referans bütünlüğünü sağlar; adres sahipliği, dükkân kapsamı, toplam tutar ve izin verilen durum geçişleri ayrıca uygulama transaction'ında doğrulanır.

## Uçtan uca hangi tablo ne zaman devreye girer?

| Aşama | Yazılan veya değişen kayıtlar | Başlıca okumalar |
| --- | --- | --- |
| Kayıt/giriş ve personel yetkisi | identity.users, roles, permissions, role_permissions, user_roles, corporate_memberships, store_assignments | Kullanıcı, aktif rol, permission ve corporate/store kapsamı |
| Adres seçimi ve dükkân keşfi | identity.customer_addresses; dükkân yöneticisi catalog.stores ve store_offerings yönetir | Adres koordinatı, dükkân yarıçapı, ürün-hizmet uygunluğu ve fiyat |
| Sepet düzenleme | ordering.carts, cart_items, cart_item_services, cart_item_photos | Aktif store_offerings ve product_type_services |
| Sipariş verme | ordering.orders, order_address_snapshots, order_items, order_item_services, order_item_photos, order_status_history, building_blocks.outbox_messages | Sepet, adres sahipliği, dükkân/hizmet/fiyat; outbox'tan OrderPlaced |
| Müşteriden ürün alımı | operations.delivery_tasks, delivery_assignments, delivery_events | Sipariş ve adres kopyası, dükkân adresi, courier_profiles ve courier_availability |
| Dükkânda kontrol | operations.item_inspections, inspection_photos; ordering.order_items.inspection_status | Sipariş ürünleri ve fotoğrafları; bütün ürünlerin incelenip incelenmediği |
| Kesin fiyat ve müşteri kararı | ordering.price_quotes, price_quote_lines, quote_approval_history, order_status_history, outbox_messages | İnceleme sonuçları, sipariş hizmetleri, son geçerli teklif |
| Ödeme ve hizmet üretimi | ordering.payment_attempts, operations.item_work, item_work_history, outbox_messages | Kabul edilen teklif, başarılı ödemenin tutarı ve sipariş hizmetleri |
| Teslim veya teklif reddinde iade | operations.delivery_tasks, delivery_assignments, delivery_events; ordering.order_status_history | Hazır/iadeye alınmış sipariş, adres ve kurye uygunluğu |

## Identity: kullanıcı, şirket ve yetki

### 1. identity.users

**Neden var?** Müşteri, şirket sahibi ve dükkân çalışanı için tek hesap kaynağıdır; aynı kişi hem müşteri hem personel olabilir. email giriş kimliğidir, password_hash parolanın kendisi yerine doğrulama verisini tutar. is_active hesabın kullanımını kapatır; case-insensitive benzersiz e-posta indeksi aynı e-postayla iki canlı hesabı engeller.

**Ne zaman yazılır/okunur?** Kayıt, parola/hesap yönetimi ve yönetici işlemlerinde yazılır. Girişte e-posta/hash/aktiflik; adres, sipariş, onay ve personel işlemlerinde UserId okunur. Kullanıcı kaydı tek başına rol veya belirli dükkâna erişim vermez; bunun için aşağıdaki ilişki tabloları gerekir. E-posta/parola değişimi sıradan genel CRUD olarak sunulmamalıdır.

### 2. identity.roles

**Neden var?** Customer, PlatformAdmin, CorporateOwner, StoreManager ve StoreStaff gibi görev paketlerini tanımlar. code sabit uygulama anahtarıdır; scope yalnızca GLOBAL, CORPORATE veya STORE olabilir. is_active rolün yeni yetki değerlendirmelerinde kullanımını kontrol eder.

**Ne zaman yazılır/okunur?** Başlangıç seed'i ve yetkili platform yönetimi yazar. Yetki denetimi rolün scope'unu ve ona bağlı izinleri okur. Roller kullanıcıya doğrudan her yerde aynı anlamda atanmaz: GLOBAL için user_roles, şirket için corporate_memberships, dükkân için store_assignments kullanılır.

### 3. identity.permissions

**Neden var?** Bir rolün yapabileceği tekil eylemleri (ör. quote.prepare, quote.approve, store.offering.manage) isimlendirir. code API ve seed arasında sabit sözleşmedir; açıklama ve is_active idari alanlardır. Dükkân kendi permission kodunu üretemez.

**Ne zaman yazılır/okunur?** Platform seed'i/yönetimi yazar. İstek sırasında IPermissionChecker, rol-permission ilişkisi üzerinden kodu denetler. Permission varlığı tek başına kullanıcıya yetki vermez; rol ve şirket/dükkân kapsamı birlikte gerekir.

### 4. identity.role_permissions

**Neden var?** Role ile Permission arasında çoktan çoğa eşlemedir. Rolün “ne yapabileceğini” söyler; “hangi dükkânda yapabileceğini” söylemez. role_id ve permission_id FK'dir; canlı çift için benzersiz indeks yinelenen eşlemeyi engeller.

**Ne zaman yazılır/okunur?** Yetkili rol yapılandırması/seed yazar; her izin denetiminde okunur. Örneğin StoreStaff'a quote.prepare verilip quote.approve verilmemesi bu tabloyla belirlenir. Atama geri alınırken geçmişi koruma/yumuşak silme kuralı uygulanır.

### 5. identity.user_roles

**Neden var?** Kullanıcıya şirket veya dükkândan bağımsız GLOBAL kapsamlı rol atar; Customer veya PlatformAdmin bunun örneğidir. role_scope alanının GLOBAL olmasını CHECK ve birleşik FK korur. Canlı kullanıcı-rol çiftinin tekrarı engellenir.

**Ne zaman yazılır/okunur?** Kayıt/ilk rol ataması veya platform yönetimi yazar; global yetki kontrolü okur. Bir StoreManager rolünü buraya koymak şema kapsamı kuralına aykırıdır; o yetki store_assignments içindedir.

### 6. identity.corporates

**Neden var?** Hizmet veren tüzel/organizasyonel şirketi temsil eder. Bir corporate birçok fiziksel stores kaydına sahip olabilir; şirket adı ve is_active burada tutulur. Müşteri adresi veya dükkân hizmet tarifesi burada tutulmaz.

**Ne zaman yazılır/okunur?** Platformun şirket oluşturma/kapatma akışı yazar. Dükkân oluşturma, CorporateOwner yetkisi ve şirket paneli okur. Pasif şirketin dükkânları keşif ve checkout'ta elenmelidir; tek başına corporate FK'si aktiflik garantisi vermez.

### 7. identity.corporate_memberships

**Neden var?** Kullanıcının hangi şirkette hangi CORPORATE kapsamlı rolle bulunduğunu tutar. user_id, corporate_id ve role_id ilişkisidir; role_scope = CORPORATE birleşik FK ile korunur. Aynı kullanıcı farklı şirketlerde farklı rollere sahip olabilir; is_active üyeliği kapatır.

**Ne zaman yazılır/okunur?** Platform yöneticisi veya yetkili şirket personel yönetimi yazar. Şirket/dükkân verisine erişirken şirket kapsamı burada doğrulanır. Bu tablo rolün eylemlerini içermez; role_permissions ile birlikte okunur. Canlı aynı üçlünün tekrarı engellenir.

### 8. identity.store_assignments

**Neden var?** Kullanıcının belirli bir fiziksel dükkândaki STORE kapsamlı rolünü tutar. StoreManager/StoreStaff ataması burada bulunur; role_scope = STORE FK ile denetlenir. Bir çalışanın iki şubedeki yetkisi ayrı satırdır.

**Ne zaman yazılır/okunur?** Yetkili personel atama/kaldırma akışı yazar. Dükkân panelindeki sipariş görme, teklif hazırlama/onaylama ve hizmet fiyatı değiştirme isteklerinde okunur. İstemciden gelen store_id'ye güvenilmez; canlı atama ve rol permission'ı birlikte kontrol edilir.

### 9. identity.customer_addresses

**Neden var?** Müşterinin düzenlenebilir adres defteridir. user_id sahipliği; label, address_text, latitude/longitude ve contact_name/contact_phone alım/teslim bilgilerini tutar. Aynı müşteri birden fazla adres saklayabilir.

**Ne zaman yazılır/okunur?** Müşteri adres ekler, günceller veya pasifleştirir. Dükkân keşfi mesafeyi, sepet seçili address_id'yi, checkout ise sahiplik ve hizmet yarıçapını buradan kontrol eder. Checkout anında veri order_address_snapshots'a kopyalanır; müşteri bu adresi sonradan değiştirince mevcut sipariş değişmez.

## Catalog: platform sözlüğü, dükkân ve tarife

### 10. catalog.product_categories

**Neden var?** Günlük giyim gibi üst ürün gruplarını önceden belirlenmiş katalog olarak saklar. code sabit kimlik, name görünen ad, sort_order ekrandaki sıra, is_active görünürlük sağlar. Dükkân veya müşteri yeni kategori açamaz.

**Ne zaman yazılır/okunur?** Platform seed'i/yönetimi yazar; müşteri ürün seçimi ve katalog ekranları okur. Bir kategori çok sayıda product_types içerir. Pasifleştirme geçmiş siparişlerin ürün adı kopyalarını silmez.

### 11. catalog.product_types

**Neden var?** Gömlek, pantolon gibi fiziksel ürün türlerini tanımlar; category_id ile kategoriye bağlanır. base_unit PIECE/M2 temel ölçü birimidir. Aynı ürün tipinden sepete iki fiziksel parça konursa iki cart_items kaydı açılır; bu katalog satırı çoğaltılmaz.

**Ne zaman yazılır/okunur?** Platform seed'i/yönetimi yazar. Müşteri ürün türü seçerken, sepet doğrulamasında ve ürün-hizmet kombinasyonu bulunurken okunur. Bir ürünün hangi hizmetleri alabileceğini tek başına belirlemez; product_type_services gerekir.

### 12. catalog.services

**Neden var?** Kuru temizleme, ütü, paça kısaltma gibi platformca tanımlanan hizmet sözlüğüdür. code/name ortak anlamı verir; tek bir service kaydı bütün ürünlerde aynı fiyatı ifade etmez. Dükkân yeni service oluşturamaz.

**Ne zaman yazılır/okunur?** Platform seed'i/yönetimi yazar; hizmet filtresi ve ürün-hizmet seçimi okur. Ürün tipine izin ve varsayılan fiyat product_type_services'te, dükkânın sunma/fiyat kararı store_offerings'tedir.

### 13. catalog.product_type_services

**Neden var?** “Bu ürün tipine bu hizmet uygulanabilir mi?” sorusunun ve platform varsayılan fiyatının cevabıdır. product_type_id + service_id benzersizdir. default_price, currency=TRY, price_unit PIECE/M2, selection_kind BASE/ALTERNATIVE/ADD_ON ve is_default_selected seçim/fiyat hesabını tanımlar; ürün başına en fazla bir varsayılan seçime izin veren indeks vardır.

**Ne zaman yazılır/okunur?** Platform onaylı katalog seed'i/yönetimi yazar. Ürün ekleme ekranı uygun hizmetleri, tahmini fiyat hesabı birim fiyatı ve checkout geçerliliği buradan okur. Aynı service farklı ürün tipinde farklı fiyatla eşlenebilir. is_default_selected başlangıç işaretidir; “zorunlu” anlamı ancak ayrıca karar verilirse uygulanır.

### 14. catalog.stores

**Neden var?** Corporate'a bağlı fiziksel hizmet noktasını, adres/koordinatını ve service_radius_km sınırını tutar. corporate_id şirket ilişkisidir; is_active dükkânın faaliyette olup olmadığını belirtir. Şirket ile dükkân ayrı tutulur çünkü bir şirketin farklı konum ve yarıçaplı şubeleri olabilir.

**Ne zaman yazılır/okunur?** Yetkili dükkân/şirket yönetimi yazar. Müşteri adresiyle keşif, sepetin tek dükkân kuralı, checkout ve kurye görevinin varış/başlangıç noktası okur. Aktif dükkânın bağlı şirketi de aktif olmalıdır; bu uygulamada ayrıca kontrol edilir.

### 15. catalog.store_offerings

**Neden var?** Belirli dükkânın platformca geçerli bir product_type_service kombinasyonunu sunup sunmadığını ve kendi fiyatını tutar. store_id + product_type_service_id benzersizdir. price_override null ise product_type_services.default_price kullanılır; is_active kapalıysa teklif müşteriye sunulmaz.

**Ne zaman yazılır/okunur?** Yetkili dükkân yöneticisi mevcut kombinasyonu açar/kapatır veya fiyatını değiştirir. Dükkân keşfi filtreleme, ürünün hizmet seçimi, tahmini toplam ve checkout buradan okur. Sepetteki hizmet store_offering_id'ye bağlanır; fiyat checkout'ta sipariş hizmetine kopyalandığı için sonradan tarife değişimi eski siparişi etkilemez.

## Ordering: sepetten kesin fiyata ve ödemeye

### 16. ordering.carts

**Neden var?** Müşterinin henüz kesinleşmemiş, **tek dükkâna bağlı** sepetinin başlığıdır. customer_id, store_id, address_id, opsiyonel pickup_window_start/end, OPEN/CHECKED_OUT/ABANDONED status ve version taşır. Aynı müşteri-dükkân için tek açık sepet indeksi vardır; bir müşteri farklı dükkânlar için ayrı sepet açabilir.

**Ne zaman yazılır/okunur?** Dükkân seçilince açılır, adres/zaman ve kalemler değiştikçe güncellenir; checkout sonunda CHECKED_OUT olur. Sepet ekranı, checkout ve idempotency kontrolü okur. address_id'nin aynı customer_id'ye aitliği ve dükkânın o adrese hizmeti uygulamada doğrulanır.

### 17. ordering.cart_items

**Neden var?** Sepetteki **tek fiziksel ürün** kaydıdır: cart_id, product_type_id, müşteri notu ve gerekirse customer_measurement. İki gömlek iki satırdır; hizmet adediyle fiziksel ürün adedi karıştırılmaz. Fotoğraflar ve seçili hizmetler bu satıra bağlanır.

**Ne zaman yazılır/okunur?** Müşteri ürün ekler/değiştirir/çıkarır. Sepet ve tahmini fiyat ekranı ile checkout tüm kalemleri okur. Ölçü tahminidir; dükkânın doğruladığı ölçü sipariş kaleminde ayrıca tutulur.

### 18. ordering.cart_item_services

**Neden var?** Bir cart_item için müşterinin istediği store_offering seçimlerini tutar. Bir ürün birden çok hizmet alabilir; aynı offering aynı ürüne canlı olarak iki kez eklenemez. FK offering'in varlığını garanti eder, uygun ürün tipi ve aynı dükkân kuralını tek başına garanti etmez.

**Ne zaman yazılır/okunur?** Hizmet kutucuğu seçilip kaldırıldığında yazılır. Sepet tahmini, checkout ve dükkânın sunabileceği hizmet doğrulaması okur. checkout, seçimin güncel aktif/uygun/yarıçap içi durumunu yeniden kontrol eder.

### 19. ordering.cart_item_photos

**Neden var?** Müşterinin henüz siparişe çevrilmemiş ürün fotoğrafı referansıdır. storage_key dosya depolama anahtarı, photo_type görselin amacıdır; görsel baytları MSSQL'de tutulmaz. cart_item silinirse FK'nin ON DELETE CASCADE kuralı bu referansı da kaldırabilir.

**Ne zaman yazılır/okunur?** Müşteri fotoğraf yükler veya sepetten çıkarır. Sepet önizlemesi ve checkout okur. Sipariş verildiğinde ayrı order_item_photos referansına kopyalanır; sepet fotoğrafının sonradan düzenlenmesi sipariş kanıtını değiştirmez.

### 20. ordering.orders

**Neden var?** Checkout sonrası bağlayıcı sipariş başlığıdır: tek customer, tek store, source_cart_id, alım zaman aralığı, estimated_total/currency, status ve version. source_cart_id benzersizliği ve müşteri + idempotency_key benzersizliği tekrar checkout'ta ikinci siparişi engellemeye yardım eder.

**Ne zaman yazılır/okunur?** Checkout transaction'ında oluşturulur; kurye, inceleme, teklif, ödeme ve teslim olaylarıyla yalnızca izin verilen status geçişleri yapılır. Müşteri sipariş takibi, dükkân kuyruğu ve Operations görev açma okur. status güncellemesi order_status_history ile birlikte yapılır; version eşzamanlı değişiklik kontrolünde kullanılır.

### 21. ordering.order_address_snapshots

**Neden var?** Sipariş anındaki müşteri adresi ve contact_name/contact_phone için değişmez kopyadır. order_id benzersizdir: sipariş başına bir müşteri adres kopyası. Müşterinin address book satırına FK yerine metin/koordinat kopyası tutulur; sonraki adres değişikliği kurye rotasını değiştirmemelidir.

**Ne zaman yazılır/okunur?** Checkout ile aynı transaction'da customer_addresses'tan doldurulur. CustomerPickup, CustomerDelivery ve RejectedOrderReturn görevleri, sipariş detayı ve geçmiş kayıtları okur. Dükkân adresi burada değil, catalog.stores'tedir; delivery_tasks görev anında iki uç noktanın adresini ayrıca kopyalar.

### 22. ordering.order_items

**Neden var?** Siparişteki her fiziksel ürünün kalıcı kaydıdır. order_id/product_type_id, product_type_name_snapshot, müşteri not/ölçüsü, verified_measurement, benzersiz tracking_code ve inspection_status saklanır. Ürün türünün adı sonradan katalogda değişse bile sipariş ekranı o günkü adı gösterebilir.

**Ne zaman yazılır/okunur?** Checkout'ta cart_items'tan kopyalanır; dükkân incelemesinde doğrulanmış ölçü ve durum güncellenir. Ürün etiketi, gelen iş kuyruğu, kesin fiyat hesabı ve müşteri takibi okur. İncelemenin ayrıntılı personel/bulgu kaydı operations.item_inspections'tadır; buradaki inspection_status özet durumdur ve iki kayıt tutarlı tutulmalıdır.

### 23. ordering.order_item_services

**Neden var?** Tek sipariş ürününün istenen her hizmeti ve **sipariş anındaki tahmini fiyatını** kalıcılaştırır. service_name_snapshot, estimated_unit_price, price_unit_snapshot, estimated_quantity, estimated_price ve REQUESTED/APPROVED/IN_PROGRESS/COMPLETED/CANCELLED status bulunur. Bir ürün-hizmet çifti aynı order_item'ta yinelenemez.

**Ne zaman yazılır/okunur?** Checkout'ta cart_item_services + o anki aktif katalog fiyatından üretilir. Teklif taslağı ve ödeme sonrası ItemWork başlatma okur. Kesin fiyat bu tabloda tahmini tutarın üzerine yazılmaz; price_quote_lines revizyonunda saklanır. FK'nin varlığı hizmetin ürün tipine uygunluğunu tek başına doğrulamaz.

### 24. ordering.order_item_photos

**Neden var?** Siparişe bağlanmış ürün fotoğrafı referansını sepet fotoğrafından bağımsız korur. order_item_id, storage_key ve photo_type içerir. Fikir, sonradan sepet düzenlenmesi/temizlenmesi geçmiş siparişin görsel kanıtını değiştirmemesidir.

**Ne zaman yazılır/okunur?** Checkout'ta cart_item_photos'tan kopyalanır. Dükkân incelemesi, teklif uyuşmazlığı ve müşteri sipariş detayı okur. Fiziksel fotoğraf dosyası dış depoda tutulur; bu tablo yalnızca referanstır. Dükkânın inceleme sırasında çektiği yeni fotoğraflar operations.inspection_photos'tadır.

### 25. ordering.order_status_history

**Neden var?** orders.status alanının değişim günlüğüdür. from_status, to_status, actor_user_id, source ve note içerir; source müşteri, dükkân, sistem, mock kurye veya ödeme sağlayıcısını ayırır. Geçmişi tek güncel status sütunundan çıkaramayız.

**Ne zaman yazılır/okunur?** Her geçerli sipariş durum geçişiyle birlikte eklenir. Takip zaman çizelgesi, destek ve hata incelemesi okur. Normal akışta mevcut geçmiş satırı değiştirilmez veya silinmez; durum geçişi doğrulaması uygulamadadır.

### 26. ordering.price_quotes

**Neden var?** Dükkân incelemesinden çıkan **kesin teklifin sürüm başlığıdır**. order_id + revision benzersizdir; DRAFT → PENDING_STORE_APPROVAL → PUBLISHED → CUSTOMER_ACCEPTED/REJECTED gibi karar status'ları, subtotal, pickup_fee, delivery_fee, total_amount, expires_at ve müşteri karar zaman/kullanıcısı burada bulunur. Yeni fiyat eski teklifi değiştirmek yerine yeni revision oluşturur.

**Ne zaman yazılır/okunur?** Bütün ürünler incelenince taslak açılır; yetkili dükkân onayıyla yayımlanır; müşteri yalnızca güncel geçerli teklifi kabul/ret eder. Ödeme amount'u bu teklifin kabul edilen total_amount'una göre doğrulanır. Filtreli unique indeks aynı siparişte eşzamanlı tek “güncel” teklif durumunu korur; toplam = satırlar + ücretler kontrolü uygulamadadır.

### 27. ordering.price_quote_lines

**Neden var?** Kesin teklifin ürün-hizmet bazında ayrıntısıdır. price_quote_id ve order_item_service_id üzerinden tek kaleme bağlanır; service_name_snapshot, unit_price, price_unit, quantity, amount ve gerekçe bulunur. Tahmini fiyat ile dükkânın doğruladığı kesin fiyat ayrı kalır.

**Ne zaman yazılır/okunur?** Taslak teklif hazırlanırken order_item_services ve doğrulanmış ölçülere göre eklenir. Dükkân onayı, müşteri teklif ekranı ve ödeme toplamı okur. Yayımlanmış teklifin satırları değiştirilmez; düzeltme yeni quote revision ve yeni satırlardır. Satırın aynı siparişe aitliği ve toplam aritmetiği uygulamada kontrol edilir.

### 28. ordering.quote_approval_history

**Neden var?** Dükkân içi teklif hazırlama/yayımlama kararlarının denetim izidir. SUBMITTED, APPROVED, REJECTED, PUBLISHED action; actor_user_id ve gerekirse reason saklanır. Müşterinin kabul/ret kararıyla karıştırılmaz; o karar price_quotes üzerindedir.

**Ne zaman yazılır/okunur?** quote.prepare ve quote.approve akışlarında her karar sırasında eklenir. Dükkân panelindeki onay geçmişi ve yetki denetimi incelemesi okur. Kullanıcının o anda ilgili store/corporate permission kapsamına sahip olması ayrıca doğrulanır.

### 29. ordering.payment_attempts

**Neden var?** Kabul edilen teklif için ödeme sağlayıcısına yapılan **her ayrı tahsilat denemesini** izler. order_id, price_quote_id, provider, provider_payment_id, idempotency_key, amount/currency, INITIATED/SUCCEEDED/FAILED/CANCELLED status ve completed_at içerir. Kart numarası/CVV tutulmaz.

**Ne zaman yazılır/okunur?** Ödeme başlatma ve sağlayıcı bildirimi yazılır; hizmetleri başlatmadan önce doğrulanmış SUCCEEDED kaydı okunur. provider + idempotency_key ve provider_payment_id indeksleri tekrar bildirimleri ayırır; sipariş başına en fazla bir başarılı deneme indeksi vardır. price_quote_id'nin aynı order_id'ye ve tutarın kabul edilen teklife aitliği uygulamada doğrulanır.

## Operations: kurye, inceleme ve hizmet üretimi

### 30. operations.courier_profiles

**Neden var?** Kurye adayının kimliği, is_mock, araç tipi, kapasitesi, son konumu ve max_task_radius_km sınırını tutar. Gerçek kuryede user_id gereklidir; mock kuryede user_id boş olmalıdır. Mock profil giriş hesabı olmadığı için kurye paneli olmadan algoritma test edilebilir.

**Ne zaman yazılır/okunur?** Test ortamında idempotent mock seed veya ileride kurye yönetimi yazar. Her otomatik atama, profilin aktifliğini, kapasitesini, konumunu ve yarıçapını okur. Mock profilin canlı müşteri siparişine atanması uygulama/ortam kuralıyla engellenmelidir.

### 31. operations.courier_availability

**Neden var?** Kuryenin AVAILABLE/UNAVAILABLE çalışma zaman aralıklarını profilden ayrı tutar; bir kurye çok sayıda vardiyaya sahip olabilir. starts_at < ends_at CHECK'i aralık geçerliliğini korur. Profilin aktif olması tek başına o saatte müsait olduğunu söylemez.

**Ne zaman yazılır/okunur?** Mock seed veya kurye vardiya yönetimi yazar. Atama algoritması görevin **tam istenen aralığının** uygun vardiya içinde olup olmadığını okur. Çakışan etkin görev kapasitesi delivery_assignments/delivery_tasks ile birlikte hesaplanır.

### 32. operations.delivery_tasks

**Neden var?** Bir sipariş için yapılacak **taşıma işini** temsil eder; görevin kendisi kurye atamasından ayrıdır. task_type CUSTOMER_PICKUP, CUSTOMER_DELIVERY veya REJECTED_ORDER_RETURN; status, iki uç noktanın adres/koordinat kopyaları, zaman aralığı ve required_capacity_units taşır. Aday kurye yoksa görev PENDING_ASSIGNMENT kalabilir.

**Ne zaman yazılır/okunur?** OrderPlaced sonrası alım, iş bitince teslim, teklif reddinde dönüş görevi açılır. Otomatik atama, dükkân operasyon kuyruğu ve sipariş takibi okur. Aynı order_id + task_type için yalnızca bir açık görev filtered unique indeksle korunur. Adresler görev açılırken kopyalanır; daha sonraki adres değişikliği açık görevi sessizce değiştirmez.

### 33. operations.delivery_assignments

**Neden var?** DeliveryTask ile seçilen CourierProfile arasındaki **atama ve yeniden atama geçmişidir**. ACTIVE/COMPLETED/CANCELLED status, assigned_at/unassigned_at, assignment_reason ve distance_km kararın nasıl verildiğini saklar. Bir görev yalnızca tek aktif atamaya sahip olabilir; eski atama kaydı kaybolmaz.

**Ne zaman yazılır/okunur?** Otomatik atama veya yetkili yeniden atama transaction'ı yazar. Kurye yük/kapasite hesabı, görev detayı ve destek incelemesi okur. Görev başına tek aktif atama indeksi vardır; aynı kuryenin çakışan görevlerde kapasite aşımı ayrıca kilitli transaction kontrolü gerektirir.

### 34. operations.delivery_events

**Neden var?** Fiziksel teslim alma/taşıma/teslim etme olaylarının zaman çizelgesidir. ASSIGNED, PICKED_UP, AT_STORE, OUT_FOR_DELIVERY, DELIVERED, RETURN_IN_TRANSIT, RETURNED ve CANCELLED event_type; MOCK/COURIER/STORE/SYSTEM source ayrımı vardır. actor_user_id opsiyoneldir.

**Ne zaman yazılır/okunur?** Yetkili mock komutları veya ileride gerçek kurye uygulaması geçişi doğrulayıp ekler. Dükkân ve müşteri takip ekranı ile operasyon denetimi okur. Atanmış olmak ürünün alındığı anlamına gelmez; status ve olay ayrı aşamalardır. Bu tablo fiziksel teslimat günlüğüdür; building_blocks.outbox_messages modüller arası teknik mesaj kuyruğudur.

### 35. operations.item_inspections

**Neden var?** Dükkâna gelen her **fiziksel OrderItem için tek ayrıntılı inceleme sonucunu** tutar. order_item_id benzersizdir; ACCEPTED/REJECTED, findings, inspected_by ve inspected_at içerir. Siparişte birden çok ürün varsa her biri ayrı incelenir.

**Ne zaman yazılır/okunur?** Ürün dükkâna ulaşıp personel kontrol edince yazılır. Bütün ürünler incelendi mi, kesin teklif hazırlanabilir mi ve reddedilen ürün nedeniyle tüm sipariş dönüşe girmeli mi sorularında okunur. ordering.order_items.inspection_status özet alanıyla birlikte tutarlı güncellenmelidir.

### 36. operations.inspection_photos

**Neden var?** Dükkânın ürün kabul/ret sırasında gördüğü durumu fotoğrafla belgeleyen referanstır. inspection_id, storage_key ve photo_type içerir; dosyanın kendisi dış depodadır. Müşterinin yüklediği order_item_photos ile kaynak ve zaman bakımından ayrıdır.

**Ne zaman yazılır/okunur?** İnceleme sırasında fotoğraf çekilirse item_inspections kaydına bağlanır. Personelin kesin fiyat gerekçesi, müşteri ihtilafı ve denetim okur. Fotoğrafın gerçekten ilgili sipariş ürününün incelemesine aitliği servis akışında doğrulanır.

### 37. operations.item_work

**Neden var?** Ödeme sonrasında yapılacak **her OrderItemService işi için tek üretim kaydıdır**. order_item_service_id benzersizdir; PENDING/IN_PROGRESS/COMPLETED/FAILED status, started_at ve completed_at taşır. Bir fiziksel üründeki iki hizmet iki iş kaydıdır.

**Ne zaman yazılır/okunur?** Kabul edilmiş teklif için başarılı ödeme doğrulanınca oluşturulur; personel işi başlatıp bitirdikçe güncellenir. Dükkân iş listesi ve “tüm hizmetler bitti mi?” kontrolü okur. Başarılı ödeme olmadan açılmamalı; tüm işler tamamlanmadan sipariş teslimata hazır yapılmamalıdır.

### 38. operations.item_work_history

**Neden var?** ItemWork durumunun önce/sonra değerini, işlemi yapan kullanıcıyı, zamanı ve notu saklar. Tek güncel item_work.status alanının açıklayamadığı işleme geçmişini verir.

**Ne zaman yazılır/okunur?** İşin her geçerli geçişinde item_work güncellemesiyle birlikte eklenir. Personel paneli zaman çizelgesi, kalite/hata incelemesi okur. Geçmiş satırı normal akışta değiştirilmez; başarısız iş de FAILED olayıyla izlenir.

## building_blocks: modüller arası güvenilir olay

### 39. building_blocks.outbox_messages

**Neden var?** Bir modüldeki veritabanı değişikliğiyle diğer modüle gönderilecek olayın **aynı transaction'da kalıcılaşmasını** sağlar. id olay kimliği, event_type sürümlü mesaj adı, aggregate_id ilgili sipariş gibi iş kaydı, JSON payload içeriği, processed_at/attempt_count/last_error teslimat durumudur. Bu tablo müşteri mesajlaşması veya sipariş durum geçmişi değildir.

**Ne zaman yazılır/okunur?** Checkout, teklif reddi, ödeme başarısı veya iş bitişi gibi kaynak modül değişikliğiyle birlikte IOutboxWriter satır ekler. IOutboxDispatcher bekleyen satırı kilitleyip handler'ları çalıştırır ve aynı transaction'da processed_at işaretler; hata olursa tekrar deneme için kayıt kalır. Örnek: OrderPlaced → müşteri alım görevi; QuoteRejected → iade görevi; PaymentSucceeded → ItemWork. Handler ve otomatik zamanlayıcı henüz modül implementasyonunda tamamlanacaktır. Harici ödeme gibi veritabanı transaction'ı dışı etkiler ayrıca idempotency gerektirir.

## Birbirine benzeyen kayıtlar neden ayrı?

- **customer_addresses → order_address_snapshots → delivery_tasks adresleri:** İlki değişebilir müşteri adres defteri, ikincisi checkout anı kanıtı, üçüncüsü belirli kurye görevinin iki uç nokta kopyasıdır.
- **product_type_services → store_offerings → order_item_services → price_quote_lines:** İlki platformun izin ve varsayılan fiyatı; ikincisi dükkânın sunma/override kararı; üçüncüsü sipariş anındaki tahmin; dördüncüsü müşteriye onaylatılan kesin fiyat revizyonudur.
- **cart_items / cart_item_photos → order_items / order_item_photos:** Sepet düzenlenebilir, sipariş checkout sonrası geçmişi korur. Ayrı kayıtlar sepet değişimlerinin eski siparişleri etkilemesini önler.
- **orders.status → order_status_history:** Biri hızlı güncel durum, diğeri geçmişteki her geçişin kim/zaman/kaynak izidir.
- **delivery_tasks → delivery_assignments → delivery_events:** Görev yapılacak taşımadır, atama hangi kuryenin sorumlu olduğunu, olay ise fiilen ne gerçekleştiğini anlatır.
- **order_items.inspection_status → item_inspections:** İlki sipariş ürününde hızlı özet, ikincisi personel, bulgu, zaman ve fotoğrafla ayrıntılı karar kaydıdır.
- **order_item_services → item_work → item_work_history:** İstenen hizmet, ödeme sonrası fiili iş ve o işin aşama geçmişi ayrı tutulur.
- **delivery_events / order_status_history / outbox_messages:** İlk ikisi iş süreci denetim geçmişi; outbox ise modüller arası teknik teslimat kaydıdır. Biri diğerinin yerine geçmez.

## Veritabanının tek başına korumadığı kurallar

Şemadaki FK, UNIQUE ve CHECK kuralları önemlidir fakat aşağıdakiler servis ve transaction sorumluluğudur: customer_addresses.user_id'nin isteği yapan müşteri olması; cart_item_services.store_offering_id'nin sepetin dükkânına ve ürün tipine uyması; order_item_services.product_type_service_id'nin ilgili ürün tipine uyması; price_quote_lines satırlarının aynı siparişten gelmesi ve toplam hesabı; payment_attempts içindeki quote/order/tutar eşleşmesi; şirket ve dükkân üyeliğinin rol permission'ıyla birlikte denetlenmesi; pasif şirket/dükkân/katalog kayıtlarının elenmesi; kurye zaman aralığı ve toplam kapasite çakışmaları; geçerli sipariş/teslimat/iş durum geçişleri. Bu kontrolleri yalnızca ekrandaki filtrelere bırakmayın.

Tabloların SQL tanımı database/GiysiGo.Database/<schema>/Tables içinde, sorumlular docs/FOUR-PERSON-PLAN.md içinde, modül sözleşmeleri docs/SHARED-START.md içinde ve prosedür kuralları docs/STORED-PROCEDURE-CONVENTIONS.md içindedir.
