# Dokümantasyon rehberi

Güncel ekip kararı: dört iş modüllü modüler monolit, ASP.NET Core / .NET 10,
MSSQL ve Dapper. Müşteri istemcisi Flutter, dükkân paneli React + TypeScript
olarak planlanır. MVP'de kurye paneli yerine mock kurye verileri kullanılır.

## Hangi belge neyi anlatır?

| Kaynak | Kullanım |
| --- | --- |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Teknoloji ve modül sınırları |
| [BRANCHING.md](BRANCHING.md) | Dal adları ve PR iş akışı |
| [SHARED-START.md](SHARED-START.md) | Ortak altyapı, contract'lar ve tarihli main durumu |
| [FOUR-PERSON-PLAN.md](FOUR-PERSON-PLAN.md) | Kişi/modül sorumlulukları ve hedef kabul ölçütleri |
| [DATABASE-DRAFT.md](DATABASE-DRAFT.md) | İş kuralları, model taslağı ve açık kararlar |
| [TABLE-PURPOSES.md](TABLE-PURPOSES.md) | Tabloların amacı ve planlanan operasyondaki yeri |
| [STORED-PROCEDURE-CONVENTIONS.md](STORED-PROCEDURE-CONVENTIONS.md) | Repository/SP sözleşmesi |
| [Veritabanı kurulumu](../database/README.md) | SSDT, DACPAC ve yerel yayınlama |

Kesin tablo, sütun ve kısıt tanımları SQL projesindedir. Derlenen C# arayüzleri
ve IntegrationEvents tipleri mevcut kod sözleşmesidir. GitHub issue'ları işin
teslim kapsamını belirler; modülün bütün hedefleri tek bir issue'nun şartı değildir.
Kod ile belge çeliştiğinde fark incelenip ilgili PR'da giderilir.

## Güncel tutma kuralı

- Açık PR'daki özellik, main'de tamamlandı olarak yazılmaz.
- İş akışı anlatımı hedef davranıştır; uygulama durumu ayrıca belirtilir.
- Özellik PR'ı, değiştirdiği sözleşme ve ilgili durum açıklamalarını da günceller.
- Durum özetleri kontrol tarihi ve referans commit ile yazılır.
- Ticari Word belgesi ilk ürün vizyonudur. Ekipçe değiştirilen teknoloji ve MVP
  kararlarının güncel kaynağı bu Markdown belgeleridir; Word bu çalışmada değişmez.
