using System.Globalization;
using InventoryHub.Application.Orders;
using InventoryHub.Application.Common;
using InventoryHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Dashboard;

public record ChartPoint(string Label, decimal Value);

public record LowStockItem(int Id, string Name, string Sku, int StockQuantity, int ReorderLevel);

public record TopProductItem(int Id, string Name, int QuantitySold, decimal Revenue);

public class DashboardData
{
    public int ActiveProducts { get; init; }
    public int TotalCustomers { get; init; }
    public int OpenOrders { get; init; }
    public decimal RevenueThisMonth { get; init; }
    public decimal RevenueLastMonth { get; init; }
    public decimal InventoryValue { get; init; }
    public IReadOnlyList<ChartPoint> MonthlyRevenue { get; init; } = [];
    public IReadOnlyList<ChartPoint> RevenueByCategory { get; init; } = [];
    public IReadOnlyList<TopProductItem> TopProducts { get; init; } = [];
    public IReadOnlyList<LowStockItem> LowStock { get; init; } = [];
    public IReadOnlyList<OrderListItem> RecentOrders { get; init; } = [];

    public decimal? MonthOverMonthChange => RevenueLastMonth == 0
        ? null
        : Math.Round((RevenueThisMonth - RevenueLastMonth) / RevenueLastMonth * 100, 1);
}

public interface IDashboardService
{
    Task<DashboardData> GetAsync(CancellationToken ct = default);
}

public class DashboardService(IApplicationDbContext db, IOrderService orderService) : IDashboardService
{
    private const int MonthsInChart = 6;

    public async Task<DashboardData> GetAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        var chartStart = monthStart.AddMonths(-(MonthsInChart - 1));

        var validOrders = db.Orders.Where(o => o.Status != OrderStatus.Cancelled);
        var validItems = db.OrderItems.IgnoreQueryFilters().Where(i => i.Order!.Status != OrderStatus.Cancelled);

        var monthly = await validOrders
            .Where(o => o.OrderDate >= chartStart)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var monthlyRevenue = Enumerable.Range(0, MonthsInChart)
            .Select(i => chartStart.AddMonths(i))
            .Select(m => new ChartPoint(
                m.ToString("MMM yy", culture),
                monthly.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Total ?? 0))
            .ToList();

        // Sıralama, record'a projeksiyondan önce anonim tip üzerinde yapılır ki SQL'e çevrilebilsin.
        var byCategory = (await validItems
                .GroupBy(i => i.Product!.Category!.Name)
                .Select(g => new { Name = g.Key, Total = g.Sum(i => i.UnitPrice * i.Quantity) })
                .OrderByDescending(x => x.Total)
                .ToListAsync(ct))
            .Select(x => new ChartPoint(x.Name, x.Total))
            .ToList();

        var topProducts = (await validItems
                .GroupBy(i => new { i.ProductId, i.Product!.Name })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.Name,
                    Quantity = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.UnitPrice * i.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToListAsync(ct))
            .Select(x => new TopProductItem(x.ProductId, x.Name, x.Quantity, x.Revenue))
            .ToList();

        var lowStock = await db.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .Take(6)
            .Select(p => new LowStockItem(p.Id, p.Name, p.Sku, p.StockQuantity, p.ReorderLevel))
            .ToListAsync(ct);

        var recent = await orderService.GetPagedAsync(new OrderQuery { PageSize = 6 }, ct);

        return new DashboardData
        {
            ActiveProducts = await db.Products.CountAsync(p => p.IsActive, ct),
            TotalCustomers = await db.Customers.CountAsync(ct),
            OpenOrders = await db.Orders.CountAsync(o =>
                o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed, ct),
            RevenueThisMonth = await validOrders.Where(o => o.OrderDate >= monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0,
            RevenueLastMonth = await validOrders.Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0,
            InventoryValue = await db.Products.SumAsync(p => (decimal?)(p.UnitPrice * p.StockQuantity), ct) ?? 0,
            MonthlyRevenue = monthlyRevenue,
            RevenueByCategory = byCategory,
            TopProducts = topProducts,
            LowStock = lowStock,
            RecentOrders = recent.Items
        };
    }
}
