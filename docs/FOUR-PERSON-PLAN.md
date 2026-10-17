# GiysiGO dört kişilik geliştirme planı

Bu belge dört modülün hedef iş dağılımını ve kabul ölçütlerini anlatır; tamamlanan işler listesi değildir. Ortak iskelet main içinde kurulmuştur; tarihli uygulama durumu SHARED-START.md içinde tutulur. Backend ASP.NET Core ve .NET 10, veritabanı Microsoft SQL Server (MSSQL), veri erişimi Dapper, mimari modüler monolittir. Tek API uygulaması ve dört iş modülü vardır. Müşteri ekranı için mevcut ticari dokümandaki Flutter, dükkân paneli için React + TypeScript temel alınır. MVP'de bu iki arayüz yapılır; kurye paneli yerine gerçek atama algoritması mock kurye verileriyle çalışır. `database/GiysiGo.Database/GiysiGo.Database.sqlproj` ilişkisel şemanın düzenlenebilir kaynağıdır.

Modül sahipleri: Identity @PnterNN, Catalog @sdnrkaleli, Ordering @AhmetEfe0, Operations @selincellkk. Derlenebilir proje iskeleti, ortak contract'lar ve mevcut sınırlar `docs/SHARED-START.md` dosyasında açıklanır.

## Ortak kurallar

1. Her geliştirici kendi `src/Modules/<Module>` ve `tests/Modules/<Module>` klasöründen sorumludur. Birbirinin entity tipini veya repository'sini doğrudan kullanmaz. Modüller `Guid` kimlikleri, public application contract'ları ve integration event'leriyle konuşur.
2. API host, ortak altyapı, CI ve kök yapılandırma değişiklikleri en az bir başka ekip üyesinin incelemesinden geçer. `main` dalına doğrudan gönderim yapılmaz. Branch adları repodaki `docs/BRANCHING.md` kuralına uyar.
3. Her modül kendi MSSQL şemasına (`identity`, `catalog`, `ordering`, `operations`) ve SQL proje klasörüne sahiptir. `building_blocks` outbox tablosu ortak altyapıdır. Tablolar, indeksler ve prosedürler SQL projesinde düzenlenir; DACPAC yayınlama script'i uygulanmadan önce incelenir. Veri taşıma gerektiren özel değişiklikler numaralı SQL script'lerinde tutulur.
4. Ortak başlangıç: `.NET 10` solution, MSSQL SQL projesi, Dapper/SqlClient, API host, health endpoint, OpenAPI ve Problem Details hazırdır. `ApiControllerBase`, `GenericCrudControllerBase<TEntity>`, `EntityBase<TId>`, `IRepository<TEntity>` ve `RepositoryBase<TEntity>` ortak tabandır. Dört modül projesi, permission arayüzü, transaction/outbox yazıcısı ve dağıtıcısı ile test projeleri bağlandı. JWT uygulaması Identity sahibindedir. Olay handler'ları ve otomatik dağıtıcı çalıştırması modül uygulamasıyla tamamlanır. CI API ve modül testlerini, ayrıca SQL DACPAC derlemesini kontrol eder.
5. İlgili iş kuralı veya canlı seed uygulanmadan önce dört karar yazılı hale getirilir; bağımsız altyapı işleri bu kararları beklemez: `[x]` hizmet kaldırılabilir mi; perde tadilat fiyatı adet mi m² mi; “Sadece Terzi” 16 işlemin ürün eşleşmeleri; reddedilen siparişin geri taşıma ücreti. Karar gelmeyen kombinasyonlar canlı katalogda pasif kalır.
6. Müşteri için `apps/customer` altında Flutter, dükkân için `apps/store` altında React + TypeScript açılır. Her iki istemci aynı REST API/OpenAPI sözleşmesini kullanır. Kişi 1 Flutter giriş/oturum iskeletini, kişi 2 React panel kabuğunu kurar; diğerleri kendi ekranlarını bu iskeletlere ekler.

## Kişi 1 — Identity ve uygulama iskeleti

**Sahip olduğu veriler:** `identity.users`, `roles`, `permissions`, `role_permissions`, `user_roles`, `corporates`, `corporate_memberships`, `store_assignments`, `customer_addresses`. Ortak host ve CI altyapısının bakımını da koordine eder.

**İlk işler:** Dört modülün Domain, Application, Infrastructure ve IntegrationEvents projeleri solution/API iskeletine zaten bağlıdır; bu bağlantıları yeniden oluşturma. Identity Dapper repository'leri ve modüle ait SQL prosedürlerini, parola hashleme, oturum açma/yenileme, kullanıcı profili ve adres CRUD'unu yaz. `Customer`, `CorporateOwner`, `StoreManager`, `StoreStaff`, `PlatformAdmin` rollerini ve permission kodlarını seed et. Bir kullanıcı birden fazla dükkânda farklı rol alabilsin.

**API ve contract:** `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `GET/POST/PUT/DELETE /me/addresses`, şirket ve personel atama endpoint'leri. Diğer modüllere `ICurrentUser`, `IPermissionChecker` ve adres okuma contract'ı ver. Dükkân kapsamını her istekte sunucuda doğrula; sadece UI butonunu gizlemek yeterli değildir.

**Ekran işi:** Flutter uygulama iskeleti, müşteri kayıt/giriş, adres listesi/ekleme; React dükkân panelinde oturum ve yetkili şirket/dükkân seçimi için ortak kimlik bileşenleri.

**Kabul ölçütü:** Müşteri yalnızca kendi adresini görür. Dükkân çalışanı yalnızca atanmış dükkânın verisine erişebilir. `quote.prepare` sahibi `quote.approve` olmadan teklif yayımlayamaz. Kullanıcı kendine veya başka şirkete üst rol veremez. Yetki entegrasyon testleri vardır.

## Kişi 2 — Catalog, dükkân keşfi ve fiyat tarifesi

**Sahip olduğu veriler:** `catalog.stores`, `product_categories`, `product_types`, `services`, `product_type_services`, `store_offerings`. `docs/catalog-draft.json` bu kişinin gözden geçireceği kaynak veridir; geçersiz/belirsiz kombinasyonları seed etmez.

**İlk işler:** Catalog Dapper repository'lerini ve SQL prosedürlerini oluştur. Beş kategori, 48 ürün tipi ve doğrulanan hizmet/fiyat kombinasyonları için idempotent seed yaz. Platform yöneticisi katalog oluşturabilir; dükkân sahibi sadece mevcut kombinasyonları etkinleştirip kendi fiyatını belirleyebilir. Store açma, adres/koordinat ve hizmet yarıçapı yönetimini ekle.

**API ve contract:** `GET /catalog/categories`, `GET /catalog/product-types`, `GET /stores?addressId=...&serviceIds=...`, `GET /stores/{id}/offerings`, dükkân için offering/fiyat yönetimi endpoint'leri. Ordering'e mevcut `IStoreCatalogReader` okuma contract'ını uygula: mağaza aktif mi, ürün-hizmet o mağazada aktif mi, geçerli birim fiyat nedir? Adres mesafesi ve filtreleme tekrar checkout'ta doğrulanmalı.

**Ekran işi:** React dükkân paneli iskeleti ve hizmet açma/kapama/fiyat düzenleme ekranı. Flutter'da müşterinin adresine göre dükkân listesi, hizmet filtreleri ve dükkân detayındaki ürün/hizmet/fiyat görünümü.

**Kabul ölçütü:** Hizmet yarıçapı dışındaki veya pasif dükkân görünmez. Dükkân platformda olmayan hizmeti ekleyemez. Ürün için geçersiz hizmet müşteriye sunulmaz. Fiyat override yoksa platform fiyatı kullanılır; sipariş verilmiş fiyat geriye dönük değişmez.

## Kişi 3 — Ordering, teklif, müşteri kararı ve ödeme

**Sahip olduğu veriler:** `ordering.carts`, `cart_items`, `cart_item_services`, `cart_item_photos`, `orders`, `order_address_snapshots`, `order_items`, `order_item_services`, `order_item_photos`, `price_quotes`, `price_quote_lines`, `quote_approval_history`, `payment_attempts`, `order_status_history`.

**İlk işler:** Ordering Dapper repository'lerini ve SQL prosedürlerini oluştur. Bir sepetin tek dükkâna ait olmasını sağla. Her fiziksel ürünü ayrı CartItem/OrderItem yap; fotoğraf referanslarını dosya depolamada tut. Ürün+hizmetlere göre tahmini fiyat hesapla. Checkout'ta adres, dükkân, hizmet ve fiyatı yeniden doğrula; sepeti snapshot'larla siparişe çevir. Aynı checkout tekrar gelirse ikinci sipariş yaratma.

**Teklif/ödeme:** Operations'tan “bütün ürünler incelendi” bilgisi gelince taslak teklife izin ver. `quote.prepare` ve `quote.approve` yetkilerini ayrı denetle. Onaylanan teklif değiştirilemez; düzeltme yeni revizyondur. Müşteri yalnızca kendi son geçerli teklifinin tamamını kabul veya reddedebilir. Ödeme sağlayıcısı için adapter oluştur; MVP'de test sağlayıcısı kullanılabilir. Başarılı ödeme tutarı kabul edilen teklif tutarıyla aynı olmalı, bildirim tekrarları güvenle işlenmelidir.

**API ve contract:** Sepet CRUD, `POST /orders/checkout`, `GET /me/orders`, sipariş detayı, teklif taslağı/onayı, müşteri kabul/ret ve ödeme denemesi endpoint'leri. Operations'a `OrderPlaced`, `QuoteRejected`, `PaymentSucceeded`, `OrderReadyForDelivery` olayları yayımla. Operations'tan durum ve inceleme sonucunu contract üzerinden al.

**Ekran işi:** Ürün ekleme/fotoğraf/hizmet seçme, sepet, tahmini toplam, sipariş takibi, teklif kabul/ret ve ödeme ekranı. Dükkân panelinde teklif hazırlama/onaylama ekranı.

**Kabul ölçütü:** İki aynı ürün iki OrderItem olur. Hizmetin fiyatı siparişte snapshot olarak kalır. Onaysız veya ödenmemiş teklif için iş başlatılamaz. Ret halinde ödeme açılmaz. Aynı ödeme bildirimi iki başarılı tahsilat üretmez.

## Kişi 4 — Operations, mock kurye ve dükkân iş akışı

**Sahip olduğu veriler:** `operations.courier_profiles`, `courier_availability`, `delivery_tasks`, `delivery_assignments`, `delivery_events`, `item_inspections`, `inspection_photos`, `item_work`, `item_work_history`.

**İlk işler:** Operations Dapper repository'lerini ve SQL prosedürlerini oluştur. Geliştirme/test ortamı için sabit mock kurye seed'i yaz; canlı müşteriye mock kurye gösterme. Sipariş oluşunca CustomerPickup görevi aç. Kurye atamasında zaman aralığı, görevin başlangıç noktasına mesafe, aktiflik, mevcut çakışan görevlerin kapasitesi ve sabit eşitlik bozma kuralını uygula. Aynı göreve çift atamayı ve aynı kuryenin kapasitesini aşmayı transaction içinde engelle. Aday yoksa görev beklesin.

**Dükkân akışı:** Mock alım/dükkâna teslim olayları ayrı komutlarla yürüsün. Her OrderItem için tek tek fotoğraflı inceleme kaydet. Bütün ürünler incelenmeden Ordering'e teklif hazırlama izni verme. Ödeme doğrulanınca her OrderItemService için ItemWork başlat; hepsi tamamlanınca teslim görevi aç. Teklif reddedilirse RejectedOrderReturn görevi aç. Atama, teslim alma ve teslim edildi durumlarını birbirine karıştırma.

**API ve contract:** Dükkân sipariş kuyruğu, ürün incelemesi, iş durumu, mock teslimat geçişi ve bekleyen kurye görevi endpoint'leri. Ordering'e `AllItemsInspected` ve `AllWorkCompleted` olayları gönder. Ordering'den gelen `OrderPlaced`, `QuoteRejected`, `PaymentSucceeded` olaylarını idempotent işle.

**Ekran işi:** Dükkân panelinde gelen sipariş, ürün bazlı inceleme, fotoğraf, işlem listesi, kalite/bitirme ve teslimata hazırlama ekranları. Kurye paneli yapılmaz; mock geçiş komutları yalnızca geliştirme/test ortamında ve yetkili kullanıcıya açılır.

**Kabul ölçütü:** En yakın uygun mock kurye seçilir; kapasitesi yetersiz/meşgul kurye atlanır; aday yoksa görev bekler. Tekrarlanan olay ikinci açık görev üretmez. İşler ödeme doğrulanmadan başlayamaz; bütün ürün hizmetleri bitmeden sipariş ReadyForDelivery olamaz.

## Birleştirme sırası ve ortak uçtan uca kontrol

Modüller arası ilk sözleşmeler aşağıdaki kadar küçük tutulur. Her mesaj `EventId`, `OccurredAtUtc` ve ilgili ID'leri taşır; tüketici `eventId` ile tekrar gelen olayı ikinci kez uygulamaz. Kaynak modül olayını kendi değişikliğiyle aynı transaction içinde outbox'a yazar.

| Olay | Üreten → Tüketen | Sonuç |
| --- | --- | --- |
| `OrderPlaced(orderId, storeId, pickupWindow)` | Ordering → Operations | Alım görevi açılır ve kurye atanır |
| `PickupAtStore(orderId)` | Operations → Ordering | Sipariş inceleme durumuna geçer |
| `AllItemsInspected(orderId)` | Operations → Ordering | Kesin teklif hazırlama açılır |
| `QuoteRejected(orderId, quoteId)` | Ordering → Operations | İade kurye görevi açılır |
| `PaymentSucceeded(orderId, quoteId)` | Ordering → Operations | Onaylı hizmetlerin iş kayıtları başlar |
| `AllWorkCompleted(orderId)` | Operations → Ordering | Sipariş teslimata hazır olur |
| `OrderReadyForDelivery(orderId)` | Ordering → Operations | Geri teslimat görevi açılır |
| `DeliveryCompleted(orderId, taskId)` | Operations → Ordering | Sipariş Delivered olur |

Event yalnızca ID ve gerekli snapshot alanlarını taşır. Başka modülün tablosunu doğrudan okuma ihtiyacı varsa önce public query contract tanımlanır. Sekiz olay tipi IntegrationEvents projelerinde tanımlıdır. Tablodaki adlar akış özetidir; tam payload ve sürümlü EventType için kaynak .cs dosyaları esas alınır. Handler implementasyonları ayrı işlerdir. Sözleşme değişirse ilgili iki modülün testleri birlikte güncellenir.

| Sıra | Çıktı | Bağımlılık |
| --- | --- | --- |
| 1 | Solution, API host, MSSQL, Dapper, CI, ortak hata/kimlik yapısı | Yok; dört kişi ilk PR'ı inceler |
| 2 | Identity ve Catalog temel tabloları, seed, dükkân keşfi | 1 |
| 3 | Ordering sepeti ve checkout; Operations alım görevi ve mock kurye ataması | 2, public contract'lar |
| 4 | Dükkân incelemesi, kesin teklif, yetkili onay, müşteri kararı, ödeme | 3 |
| 5 | Ürün hizmet işlemleri, teslim/iade görevi, iki panelin uçtan uca akışı | 4 |

Ortak son senaryo: müşteri adres seçer → hizmete uygun dükkânı bulur → iki fiziksel ürün ve farklı hizmetler ekler → fotoğraf yükleyip sipariş verir → uygun mock kurye atanır → ürünler dükkâna ulaşır → her ürün incelenir → yetkili kişi kesin fiyatı yayımlar → müşteri kabul edip test ödemesi yapar → her hizmet tamamlanır → teslim görevi atanır → teslim olayı kaydedilir. İkinci senaryoda müşteri teklifi reddeder ve bütün sipariş için dönüş görevi açılır. Bu iki senaryo API entegrasyon testinde çalışmalıdır.

## Şema uygulama notları

`database/GiysiGo.Database/GiysiGo.Database.sqlproj` MSSQL nesnelerinin düzenlenebilir kaynağıdır. Veritabanı CHECK/UNIQUE/FK kuralları temel veri bütünlüğünü sağlar. Şu iş kuralları ayrıca uygulama transaction'larında doğrulanır: müşteri adresinin gerçekten kendisine ait olması; seçilen StoreOffering'in sepetin dükkânına ve ürün tipine ait olması; teklif kalemlerinin aynı siparişe ait olması; tutar toplamlarının satırlarla eşleşmesi; izin verilen durum geçişleri; kurye kapasitesinin çakışan zaman aralıklarında aşılmaması; rol/permission ile şirket/dükkân kapsamının birlikte kontrolü. Modüller arası doğrudan entity/repository bağımlılığı kullanılmaz.

MVP kataloğunda `[x]` işaretinin anlamı, perde ek hizmetlerinin fiyat birimi ve bazı çift ücret riski taşıyan ürünler henüz net değildir. `catalog-draft.json` veri kaynağıdır, doğrulanmamış satırlar doğrudan canlı seed değildir. `ProductTypeService.PriceUnit` SQL'de zorunludur; birimi bilinmeyen üç perde tadilatı cevap gelene kadar aktif olarak eklenmez.
