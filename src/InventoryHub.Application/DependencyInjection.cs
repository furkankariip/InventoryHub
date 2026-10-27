using InventoryHub.Application.Categories;
using InventoryHub.Application.Customers;
using InventoryHub.Application.Dashboard;
using InventoryHub.Application.Orders;
using InventoryHub.Application.Products;
using InventoryHub.Application.Stock;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
