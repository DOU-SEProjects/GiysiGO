# Identity veri erişimi — #6

Bu teslim mevcut dokuz tablo için model ve veri erişimi sağlar. HTTP controller,
JWT, parola hashleme algoritması, refresh oturumu veya permission kararı içermez.
Repository çağrıları güvenilir uygulama servisleri içindir; kullanıcı girdisi
doğrudan bu metotlara bağlanmaz. Yetki denetimi #10/#15, oturum #13, adres API ve
ortak adres okuyucuları #16 kapsamında tamamlanır. #42'nin merge edilmesi bu
veri erişimi kodunun derlenmesi için gerekli değildir.

## Dosyalar ve kayıtlar

- `Domain/Entities`: SQL ile eşleşen dokuz `EntityBase<Guid>` modeli.
- `Application/Interfaces/Repositories`: altı repository arayüzü.
- `Infrastructure/Repositories`: Dapper implementasyonları ve transaction kontrolü.
- `identity/Stored Procedures`: 38 prosedür; SQL metni C# sınıflarında bulunmaz.
- `IdentityModuleRegistration`: altı scoped DI kaydı. Host'un mevcut
  SqlConnectionFactory ve ITransactionRunner kayıtları kullanılır.

| Repository | Tablolar / işlemler | Bağlantı davranışı |
| --- | --- | --- |
| CorporateRepository | corporates; beş temel CRUD prosedürü | RepositoryBase, her çağrıda ayrı bağlantı |
| RoleRepository | roles; beş temel CRUD prosedürü | RepositoryBase; Code/Scope update ile değişmez |
| PermissionRepository | permissions; beş temel CRUD prosedürü | RepositoryBase; Code update ile değişmez |
| UserRepository | users; insert, ID/email okuma, açık parola-hash/aktiflik/silme komutları | Çağıranın connection/transaction'ı zorunlu |
| CustomerAddressRepository | customer_addresses; sahiplik kontrollü insert/liste/tekil okuma/update/delete | Çağıranın connection/transaction'ı zorunlu |
| IdentityAccessRepository | role_permissions, user_roles, corporate_memberships, store_assignments; grant/revoke/list | Çağıranın connection/transaction'ı zorunlu |

Generic repository'ler yönetim verisi erişimidir; yetkilendirilmiş public CRUD API
anlamına gelmez. GetAll pasif fakat silinmemiş yönetim kayıtlarını da getirir.
Giriş sorgusu pasif kullanıcıyı da döndürür: #13 IsActive kontrolünü yapmalıdır.
PasswordHash yalnızca Identity içinde kullanılır; kullanıcı API yanıtına eklenmez.
Bu katman hash algoritmasını doğrulamaz/üretmez; #13 gerçek hash sağlamalıdır.

## Prosedür ve transaction kuralları

Insert boş/null Guid için yeni kimlik oluşturup tek satır `Id` döndürür. SQL
sütunları C# adlarına alias edilir; create_date/updates SQL UTC saatinden gelir.
SQL datetime2 timezone taşımaz; okunan DateTime değerleri UTC olarak yorumlanır.
Kayıtların bulunamadığı okumalarda null/boş liste döner; silinmiş kayıtlar görünmez.
Komut sonuçlarında Dapper ExecuteAsync satır sayısı kontrol edilmez; gerekli
satırın varlığını SP kontrol edip THROW üretir.

IdentityCommandRepositoryBase verilen bağlantının açık ve transaction'ın aynı
bağlantıya ait olduğunu doğrular. Bu metotlar connection/transaction açmaz,
commit/rollback/dispose etmez. İşlem sınırını ITransactionRunner belirler; hata
durumunda bütün yazmalar geri alınır. Generic repository işlemleri bu transaction'a
katılmaz; çok tablolı kayıt/rol verme akışında UserRepository +
IdentityAccessRepository aynı transaction ile kullanılır.

Uzunluğu sınırlı metinler SP girişinde kesilmeden alınır ve sütunun UTF-16
uzunluk sınırına göre doğrulanır; uzun e-posta başka kullanıcıyla eşleşemez.
`51000`: zorunlu alan eksik veya metin fazla uzun. `51001`: kullanıcı/kayıt ilgili kapsamda bulunamadı.
`51002`: aktif referans veya doğru kapsamlı rol bulunamadı. Unique/FK/CHECK
hataları SQL Server'ın kendi kodlarıyla gelir. HTTP Problem Details eşlemesi
controller/uygulama servisinin işidir; bu PR API hata dönüşü eklemez.

## Sahiplik, rol kapsamı ve tekrarlar

Adres insert'inde userId ayrı parametredir; adres modelinin UserId'si güven kaynağı
değildir. Update adresin sahipliğini değiştiremez. Okuma aktif kullanıcı ve aktif,
silinmemiş adres ister. Update aktif kullanıcıya ait silinmemiş adresi yeniden
etkinleştirebilir; delete sonrası adres geri açılmaz. Yanlış kullanıcıyla update
ve delete aynı bulunamadı hatası verir; adresin varlığını açığa çıkarmaz.

UserRole yalnızca GLOBAL, CorporateMembership CORPORATE, StoreAssignment STORE
rolünü kabul eder. Grant aktif rol ve kullanıcıyı (üyelikte aktif corporate'ı da)
doğrular. RolePermission aktif rol ve permission ister. Kapsam SQL'in mevcut
composite FK/CHECK kısıtlarıyla da korunur. Aynı canlı ilişkiyi tekrar grant etmek
aynı Guid'i döndürür; pasif canlı üyelik/atama yeniden aktif olur. Revoke soft-delete
yapar ve tekrarında no-op'tur. Silinmiş ilişki yeniden grant edilirse eski satır
korunur, yeni Guid oluşur. Eşzamanlı grant, UPDLOCK/HOLDLOCK ve canlı unique indeksle
tek ilişki üretir.

StoreAssignment'ta store'un varlığı mevcut FK ile korunur. Store'un aktifliği,
beklenen corporate'a aitliği ve çağıranın atama yetkisi #10/#15'te Catalog public
contract'ıyla kontrol edilecektir. Bu repository tenant yetkilendirmesinin tamamı
değildir; yabancı modül tablosuna doğrudan sorgu eklenmemiştir. Liste metotları
aktif Identity referanslarını kontrol eder; store aktifliği dış sözleşmenin işidir.

## Gerçek MSSQL doğrulaması

Windows, .NET 10, Visual Studio SSDT/MSBuild, SqlPackage, SqlLocalDB ve sqlcmd gerekir:

```powershell
./scripts/Test-IdentityData.ps1
```

Script DACPAC'i derler; her çalıştırmada yeni `GiysiGoIdentityTest_<guid>` LocalDB
instance'ı ve `GiysiGo_IdentityTest_<guid>` veritabanı oluşturur. Testlerde gerçek
DI repository'leri ve prosedürler kullanılır. Sonunda yalnızca kendi oluşturduğu
veritabanını drop edip instance'ı kaldırır; mevcut geliştirme DB'sini kullanmaz.
Araç konumu gerekirse SqlPackagePath/MSBuildPath/VisualStudioVersion parametreleri
ile verilebilir. Aktarım ve test hatasında da temizlik finally içinde çalışır.

Sadece `dotnet test` çalıştırıldığında GIYSIGO_IDENTITY_TEST_CONNECTION yoksa SQL
testleri açıkça skipped olur; bu, DB testlerinin geçtiği anlamına gelmez. Script
değişkeni yalnızca test süresince ayarlar ve önceki değeri geri yükler. Test helper
başka isimli veritabanlarını reddeder. SQL fixture'ı yalnızca StoreAssignment
FK'sini test etmek için sentetik bir Catalog mağazası oluşturur; üretim seed'i değildir.

Mevcut Linux CI bu DB testlerini skipped olarak gösterir, SQL projesi Windows
job'ında derlenir. Gerçek DB testlerinin CI'a bağlanması #19'daki ortam/fixture
çalışmasında yapılacaktır; #6 için yukarıdaki Windows komutu gerçek DB doğrulamasıdır.

Kontroller: dokuz modelin gerçek veri erişimi, Unicode/decimal/tarih/Guid eşlemesi,
case-insensitive e-posta benzersizliği, adres sahipliği, geçersiz koordinat, aktif
kullanıcı şartı, rol kapsamı, grant tekrarları/eşzamanlılığı, soft-delete, çok tablolı
rollback ve farklı bağlantıya ait transaction'ın reddedilmesi.
