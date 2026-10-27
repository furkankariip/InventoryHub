using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryHub.Infrastructure.Persistence;

/// <summary>
/// Uygulama ilk açıldığında migration'ları uygular; rolleri, demo kullanıcılarını ve
/// gerçekçi örnek verileri (son 6 aya yayılmış siparişler dahil) oluşturur.
/// </summary>
public class DbInitializer(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<DbInitializer> logger)
{
    public const string AdminEmail = "admin@inventoryhub.local";
    public const string AdminPassword = "Admin123!";
    public const string StaffEmail = "personel@inventoryhub.local";
    public const string StaffPassword = "Personel123!";

    public async Task InitializeAsync(bool seedSampleData)
    {
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();

        await SeedIdentityAsync();

        if (seedSampleData && !await db.Categories.IgnoreQueryFilters().AnyAsync())
        {
            await SeedSampleDataAsync();
            logger.LogInformation("Örnek veriler oluşturuldu.");
        }
    }

    private async Task SeedIdentityAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(AdminEmail, AdminPassword, "Sistem Yöneticisi", Roles.Admin);
        await EnsureUserAsync(StaffEmail, StaffPassword, "Depo Personeli", Roles.Staff);
    }

    private async Task EnsureUserAsync(string email, string password, string fullName, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
    }

    private async Task SeedSampleDataAsync()
    {
        var random = new Random(42); // Deterministik: her kurulumda aynı demo verisi.
        var now = DateTime.UtcNow;
        var openingDate = now.AddDays(-200);

        var catalog = new Dictionary<string, (string Name, string Sku, decimal Price, int Stock, int Reorder)[]>
        {
            ["Bilgisayar"] =
            [
                ("Dizüstü Bilgisayar 14\" i5 16GB", "NB-I5-14", 32499m, 25, 5),
                ("Dizüstü Bilgisayar 15.6\" i7 32GB", "NB-I7-15", 48999m, 12, 4),
                ("Mini PC Ryzen 5", "PC-MINI-R5", 18750m, 18, 5),
                ("Tablet 11\" 128GB", "TB-11-128", 13999m, 30, 8)
            ],
            ["Çevre Birimleri"] =
            [
                ("Kablosuz Mouse", "PR-MOUSE-W", 549m, 180, 40),
                ("Mekanik Klavye TR Q", "PR-KB-MECH", 2199m, 60, 15),
                ("27\" IPS Monitör", "PR-MON-27", 8499m, 22, 6),
                ("Full HD Web Kamerası", "PR-CAM-FHD", 1499m, 45, 10),
                ("USB-C Dock İstasyonu", "PR-DOCK-C", 3299m, 35, 8)
            ],
            ["Ağ Ürünleri"] =
            [
                ("Wi-Fi 6 Router", "NW-RT-AX", 3899m, 28, 6),
                ("24 Port Gigabit Switch", "NW-SW-24", 5799m, 10, 3),
                ("Cat6 Ağ Kablosu 305m", "NW-CBL-C6", 2650m, 40, 10),
                ("Access Point PoE", "NW-AP-POE", 2999m, 16, 4)
            ],
            ["Depolama"] =
            [
                ("NVMe SSD 1TB", "ST-NVME-1T", 2899m, 70, 15),
                ("Harici Disk 2TB", "ST-HDD-2T", 2499m, 38, 10),
                ("USB Bellek 64GB", "ST-USB-64", 249m, 250, 50),
                ("NAS Cihazı 2 Yuvalı", "ST-NAS-2B", 11499m, 6, 3)
            ],
            ["Ofis Mobilyası"] =
            [
                ("Ergonomik Ofis Koltuğu", "OF-CHR-ERG", 7999m, 14, 4),
                ("Ayarlanabilir Çalışma Masası", "OF-DSK-ADJ", 12499m, 8, 3),
                ("Monitör Kolu Çiftli", "OF-ARM-DBL", 1899m, 26, 6)
            ],
            ["Kırtasiye"] =
            [
                ("A4 Fotokopi Kağıdı (5 top)", "KR-A4-5", 649m, 120, 30),
                ("Lazer Yazıcı Toneri", "KR-TONER", 1799m, 34, 10),
                ("Not Defteri Seti", "KR-NOTE-SET", 189m, 90, 20)
            ]
        };

        var products = new List<Product>();
        foreach (var (categoryName, items) in catalog)
        {
            var category = new Category { Name = categoryName, CreatedAt = openingDate };
            db.Categories.Add(category);

            foreach (var item in items)
            {
                var product = new Product
                {
                    Name = item.Name,
                    Sku = item.Sku,
                    UnitPrice = item.Price,
                    ReorderLevel = item.Reorder,
                    Category = category,
                    CreatedAt = openingDate,
                    Description = $"{categoryName} kategorisinde demo ürün."
                };
                // Satışlar sonrasında stok makul seviyede kalsın diye açılış stoğu yüksek tutulur.
                var openingStock = item.Stock * 3;
                product.IncreaseStock(openingStock);
                product.StockMovements.Add(new StockMovement
                {
                    Type = StockMovementType.Purchase,
                    Quantity = openingStock,
                    StockAfter = openingStock,
                    Description = "Açılış stoğu",
                    CreatedBy = AdminEmail,
                    CreatedAt = openingDate
                });
                products.Add(product);
                db.Products.Add(product);
            }
        }

        string[] cities = ["İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Kocaeli", "Eskişehir", "Konya"];
        string[] companies =
        [
            "Anadolu Yazılım A.Ş.", "Boğaziçi Lojistik", "Ege Mimarlık", "Kuzey Danışmanlık", "Marmara Eğitim Kurumları",
            "Toros Enerji", "Yıldız Muhasebe", "Kapadokya Turizm", "Deniz Medikal", "Atlas İnşaat",
            "Pera Reklam Ajansı", "Kardelen Hukuk Bürosu", "Mavi Bulut Teknoloji", "Doğa Gıda", "Zirve Otomotiv"
        ];

        var customers = companies.Select((name, i) => new Customer
        {
            FullName = name,
            Email = $"satinalma{i + 1}@{Slug(name)}.com.tr",
            Phone = $"0{random.Next(212, 489)} {random.Next(100, 999)} {random.Next(10, 99)} {random.Next(10, 99)}",
            City = cities[random.Next(cities.Length)],
            Address = $"Örnek Mah. {random.Next(1, 120)}. Sok. No:{random.Next(1, 60)}",
            CreatedAt = openingDate.AddDays(random.Next(0, 20))
        }).ToList();
        db.Customers.AddRange(customers);

        for (var i = 0; i < 140; i++)
        {
            var orderDate = now.AddDays(-random.Next(0, 180)).AddMinutes(-random.Next(0, 600));
            var order = new Order
            {
                OrderNumber = $"SP-{orderDate:yyMMdd}-{i + 1:X6}",
                OrderDate = orderDate,
                CreatedAt = orderDate,
                Customer = customers[random.Next(customers.Count)],
                CreatedBy = random.Next(2) == 0 ? AdminEmail : StaffEmail
            };

            foreach (var product in products.OrderBy(_ => random.Next()).Take(random.Next(1, 5)))
            {
                var quantity = Math.Min(product.UnitPrice > 5000 ? random.Next(1, 4) : random.Next(1, 12),
                    product.StockQuantity);
                if (quantity <= 0)
                    continue;

                product.DecreaseStock(quantity);
                order.AddItem(product, quantity);
                db.StockMovements.Add(new StockMovement
                {
                    Product = product,
                    Order = order,
                    Type = StockMovementType.Sale,
                    Quantity = -quantity,
                    StockAfter = product.StockQuantity,
                    Description = $"Sipariş {order.OrderNumber}",
                    CreatedBy = order.CreatedBy,
                    CreatedAt = orderDate
                });
            }

            if (order.Items.Count == 0)
                continue;

            AdvanceStatus(order, (now - orderDate).TotalDays, random);
            if (order.Status == OrderStatus.Cancelled)
            {
                foreach (var item in order.Items)
                {
                    item.Product!.IncreaseStock(item.Quantity);
                    db.StockMovements.Add(new StockMovement
                    {
                        Product = item.Product,
                        Order = order,
                        Type = StockMovementType.Return,
                        Quantity = item.Quantity,
                        StockAfter = item.Product.StockQuantity,
                        Description = $"Sipariş {order.OrderNumber} iptali",
                        CreatedBy = order.CreatedBy,
                        CreatedAt = orderDate.AddHours(5)
                    });
                }
            }

            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Eski siparişler büyük olasılıkla teslim edilmiş, yeniler ise henüz sürecin başında olur.</summary>
    private static void AdvanceStatus(Order order, double ageInDays, Random random)
    {
        if (random.NextDouble() < 0.07)
        {
            order.ChangeStatus(OrderStatus.Cancelled);
            return;
        }

        var steps = ageInDays switch
        {
            > 14 => 3,
            > 5 => random.Next(2, 4),
            > 1 => random.Next(0, 3),
            _ => random.Next(0, 2)
        };

        OrderStatus[] flow = [OrderStatus.Confirmed, OrderStatus.Shipped, OrderStatus.Delivered];
        foreach (var status in flow.Take(steps))
            order.ChangeStatus(status);
    }

    /// <summary>"Boğaziçi Lojistik" → "bogazici" (demo e-posta alan adı için).</summary>
    private static string Slug(string name)
    {
        var ascii = name.ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u')
            .Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
        var firstWord = new string(ascii.TakeWhile(char.IsAsciiLetter).ToArray());
        return firstWord.Length > 0 ? firstWord : "firma";
    }
}
