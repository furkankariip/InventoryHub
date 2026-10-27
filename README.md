# InventoryHub — Stok ve Sipariş Yönetim Sistemi

![CI](https://github.com/furkankariip/InventoryHub/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-9-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB%20%7C%202022-CC2927)

KOBİ'ler için geliştirilmiş, **ASP.NET Core MVC**, **Entity Framework Core** ve **SQL Server** tabanlı bir stok, müşteri ve sipariş yönetim uygulamasıdır. Katmanlı mimari, rol tabanlı yetkilendirme, işlem güvenliği ve birim testleri üzerine kurulmuştur.

> *English summary:* A layered ASP.NET Core MVC (.NET 9) inventory & order management app using EF Core 9 Code-First with SQL Server, ASP.NET Core Identity (role-based authorization), soft delete, optimistic concurrency, audit trail of stock movements, a Chart.js dashboard, xUnit tests, Docker Compose and GitHub Actions CI.

---

## Özellikler

| Modül | Açıklama |
|---|---|
| **Gösterge paneli** | Aylık ciro grafiği, kategori bazlı satış dağılımı, en çok satanlar, kritik stok uyarıları, geçen aya göre değişim |
| **Ürünler** | Arama, kategori filtresi, sütuna göre sıralama, sayfalama, kritik stok işaretleme |
| **Stok hareketleri** | Her stok değişikliği (giriş, satış, iade, sayım düzeltmesi) kullanıcı ve zaman bilgisiyle değiştirilemez kayıt olarak tutulur |
| **Siparişler** | Çok satırlı sipariş formu (canlı toplam hesaplama), durum akışı (Beklemede → Onaylandı → Kargoda → Teslim), iptalde otomatik stok iadesi |
| **Müşteriler** | Müşteri kartı, sipariş geçmişi ve toplam alışveriş tutarı |
| **Kullanıcılar** | Yönetici kullanıcı ekleyebilir, hesapları devre dışı bırakabilir |

## Teknik öne çıkanlar

- **Katmanlı (Clean) mimari** — `Domain` → `Application` → `Infrastructure` → `Web`. İş kuralları altyapıdan bağımsızdır; Application katmanı yalnızca `IApplicationDbContext` arayüzünü bilir.
- **Zengin domain modeli** — Stok düşme/artırma ve sipariş durum geçişleri entity metotlarıyla korunur (`Product.DecreaseStock`, `Order.ChangeStatus`). Geçersiz durum geçişleri `DomainException` fırlatır.
- **EF Core Code-First + Fluent API** — Ayrı `IEntityTypeConfiguration` sınıfları, `decimal(18,2)` hassasiyeti, enum'ların string olarak saklanması, unique ve **filtreli unique index'ler**, **check constraint'ler** (`StockQuantity >= 0`).
- **Soft delete** — `ISoftDelete` uygulayan tüm varlıklara global query filter, reflection ile otomatik eklenir; `Remove()` çağrısı `SaveChanges` içinde güncellemeye çevrilir.
- **Audit alanları** — `CreatedAt` / `UpdatedAt` değerleri `SaveChanges` override'ında merkezi olarak doldurulur.
- **Optimistic concurrency** — Ürünlerde `ConcurrencyStamp` token'ı sayesinde eşzamanlı düzenleme ve aynı ürüne gelen eşzamanlı siparişler stoğu bozamaz.
- **Atomik sipariş işlemi** — Sipariş, kalemler, stok düşümü ve stok hareketleri tek `SaveChanges` (tek transaction) ile kaydedilir. Birim fiyatlar istemciden değil veritabanından alınır.
- **Performans** — Listeleme sorgularında `AsNoTracking` ve DTO projeksiyonu, veritabanı tarafında sayfalama, dashboard'da SQL tarafında `GROUP BY`.
- **ASP.NET Core Identity** — Admin / Personel rolleri, policy tabanlı yetkilendirme, hesap kilitleme, Türkçe hata mesajları, varsayılan olarak tüm sayfalarda giriş zorunluluğu (global `AuthorizeFilter`).
- **Güvenlik** — Global CSRF koruması (`AutoValidateAntiforgeryToken`), open-redirect kontrolü, sunucu taraflı doğrulama.
- **Merkezi hata yönetimi** — `ApplicationExceptionFilter`: `NotFoundException` → 404 sayfası, `DomainException` → kullanıcı dostu uyarı.
- **Health check** — `/health` uç noktası veritabanı bağlantısını kontrol eder.
- **Testler** — xUnit ile domain kuralları ve servisler; testler bellek içi **SQLite** üzerinde çalışır, böylece unique index, FK ve check constraint gibi ilişkisel kurallar da gerçekten test edilir.
- **DevOps** — Dockerfile (multi-stage), SQL Server içeren Docker Compose, GitHub Actions ile otomatik build ve test.

## Mimari

```
src/
├── InventoryHub.Domain          # Entity'ler, enum'lar, iş kuralları (bağımlılık yok)
├── InventoryHub.Application     # Servisler, DTO/form modelleri, IApplicationDbContext
├── InventoryHub.Infrastructure  # EF Core DbContext, konfigürasyonlar, migration'lar, Identity, seed
└── InventoryHub.Web             # MVC controller'lar, Razor view'lar, filtreler
tests/
└── InventoryHub.Tests           # xUnit + SQLite in-memory
```

### Veritabanı şeması

```
Categories 1───* Products 1───* StockMovements *───0..1 Orders
                    │                                     │
                    └──────────* OrderItems *─────────────┘
                                                          │
Customers 1───────────────────────────────────────────* Orders
AspNetUsers / AspNetRoles (Identity)
```

## Kurulum

### Gereksinimler
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (Visual Studio ile birlikte gelir) **veya** Docker

### 1) Visual Studio / LocalDB ile

```bash
git clone https://github.com/furkankariip/InventoryHub.git
cd InventoryHub
dotnet run --project src/InventoryHub.Web
```

İlk çalıştırmada migration'lar otomatik uygulanır ve örnek veriler (6 kategori, 23 ürün, 15 müşteri, son 6 aya yayılmış ~140 sipariş) oluşturulur. Visual Studio'da `InventoryHub.sln` dosyasını açıp **F5** ile de çalıştırabilirsiniz.

### 2) Docker ile

```bash
docker compose up --build
```

Uygulama: http://localhost:8080

### Demo hesapları

| Rol | E-posta | Şifre |
|---|---|---|
| Yönetici | `admin@inventoryhub.local` | `Admin123!` |
| Personel | `personel@inventoryhub.local` | `Personel123!` |

**Personel** rolü sipariş, müşteri ve stok işlemleri yapabilir. Ürün ve kategori tanımlama, silme ve kullanıcı yönetimi yalnızca **Yönetici** rolüne açıktır.

## Geliştirme komutları

```bash
dotnet test                                   # testleri çalıştır
dotnet tool restore                           # dotnet-ef aracını yükle
dotnet ef migrations add <Ad> -p src/InventoryHub.Infrastructure -s src/InventoryHub.Web -o Persistence/Migrations
dotnet ef database update -p src/InventoryHub.Infrastructure -s src/InventoryHub.Web
```

## Kullanılan teknolojiler

ASP.NET Core MVC 9 · Entity Framework Core 9 · SQL Server · ASP.NET Core Identity · Bootstrap 5 · Chart.js · xUnit · SQLite (test) · Docker · GitHub Actions
