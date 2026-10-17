# GiysiGO MSSQL veritabanı projesi

`GiysiGo.Database/GiysiGo.Database.sqlproj`, veritabanının tek şema kaynağıdır. Visual Studio Solution Explorer'da `identity`, `catalog`, `ordering`, `operations` ve `building_blocks` klasörleri altında tablolar, indeksler, şemalar ve stored procedure'ler ayrı dosyalarda görünür. SQL projesi derlenince `.dacpac` üretilir. Toplam 39 tablo vardır; hepsinde `id`, `create_date`, `update_date` ve `delete_date` bulunur.

Tabloların işlevi, operasyon sırasında nasıl kullanıldıkları ve aralarındaki farklar [tablo rehberinde](../docs/TABLE-PURPOSES.md) tek tek açıklanır.

SQL projesi her modül klasöründeki `.sql` dosyalarını otomatik dahil eder. Yeni tablo veya stored procedure ekleyen geliştirici `.sqlproj` dosyasını değiştirmez; kendi şema klasörüne dosya ekleyip DACPAC derlemesini çalıştırır. `Scripts/PostDeployment.sql` ayrı PostDeploy öğesidir.

## Yerel kurulum

1. Visual Studio'da `GiysiGo.sln` açıp `GiysiGo.Database` projesini **Build** edin.
2. LocalDB kullanıyorsanız PowerShell'de `SqlLocalDB create GiysiGo` ve `SqlLocalDB start GiysiGo` komutlarını çalıştırın.
3. SQL projesine sağ tıklayıp **Publish** seçin. Hedef sunucu `(localdb)\GiysiGo`, veritabanı `GiysiGo` olsun. Önce **Generate Script** ile değişiklikleri inceleyin, sonra yayınlayın.

Bu makinede komut satırından derleme ve yayınlama örneği:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' database/GiysiGo.Database/GiysiGo.Database.sqlproj /p:VisualStudioVersion=18.0 /p:Configuration=Release
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\SqlPackage.exe' /Action:Publish '/SourceFile:database\GiysiGo.Database\bin\Release\GiysiGo.Database.dacpac' '/TargetConnectionString:Server=(localdb)\GiysiGo;Integrated Security=True;Database=GiysiGo;TrustServerCertificate=True' /p:BlockOnPossibleDataLoss=True
```

Visual Studio sürümü ve kurulum yolu ekip üyelerine göre değişebilir. Üretim ortamında bağlantı bilgileri kodda tutulmaz; yayın hedefi ortam konfigürasyonundan seçilir. DACPAC yayınlama script'i uygulanmadan önce incelenir; veri dönüşümü veya özel sıra gerektiren değişiklikler `database/migrations` altında numaralı SQL script'leri olarak tutulur. Dapper şema üretmez.

Katalog kaynak verisi `docs/catalog-draft.json` dosyasındadır. Belirsiz fiyat birimli işlemler iş kararı gelmeden aktif seed edilmez. Mock kurye seed'i Operations sorumluluğundadır. SQL Server `datetime2` saat dilimi saklamaz; tüm okuma/yazmalar UTC kabul edilir. `DeleteDate` yumuşak silmeyi belirtir; `IsActive` ise ayrı bir aktiflik alanıdır; geçmiş sipariş, teklif ve ödeme kayıtları operasyon boyunca korunur.

Adres sahipliği, dükkân ve ürün-hizmet uyumu, toplam tutar, yetki kapsamı, durum geçişi ve kurye kapasitesi ilgili modül transaction'larında doğrulanır. Genel `RepositoryBase` tek adımlı CRUD içindir; çok tablolı checkout, teklif, ödeme ve atama akışları tek SQL transaction kullanır.
