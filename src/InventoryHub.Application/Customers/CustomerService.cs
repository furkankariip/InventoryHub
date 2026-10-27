using InventoryHub.Application.Common;
using InventoryHub.Domain.Entities;
using InventoryHub.Domain.Enums;
using InventoryHub.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryHub.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItem>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default);
    Task<CustomerDetail> GetDetailAsync(int id, CancellationToken ct = default);
    Task<CustomerFormModel> GetForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CustomerFormModel model, CancellationToken ct = default);
    Task UpdateAsync(CustomerFormModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class CustomerService(IApplicationDbContext db) : ICustomerService
{
    public async Task<PagedResult<CustomerListItem>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default)
    {
        var customers = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            customers = customers.Where(c =>
                c.FullName.Contains(term) || c.Email.Contains(term) || (c.City != null && c.City.Contains(term)));
        }

        return await customers
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerListItem(
                c.Id, c.FullName, c.Email, c.Phone, c.City,
                c.Orders.Count(o => o.Status != OrderStatus.Cancelled),
                c.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => (decimal?)o.TotalAmount) ?? 0))
            .ToPagedResultAsync(query.Page, query.PageSize, ct);
    }

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct = default) =>
        await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new LookupItem(c.Id, c.FullName))
            .ToListAsync(ct);

    public async Task<CustomerDetail> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
                       ?? throw new NotFoundException(nameof(Customer), id);

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == id)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new CustomerOrderItem(o.Id, o.OrderNumber, o.OrderDate, o.Status, o.TotalAmount))
            .ToListAsync(ct);

        var totalSpent = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

        return new CustomerDetail(
            customer.Id, customer.FullName, customer.Email, customer.Phone, customer.City,
            customer.Address, customer.CreatedAt, totalSpent, orders);
    }

    public async Task<CustomerFormModel> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NotFoundException(nameof(Customer), id);

        return new CustomerFormModel
        {
            Id = c.Id, FullName = c.FullName, Email = c.Email, Phone = c.Phone, City = c.City, Address = c.Address
        };
    }

    public async Task<int> CreateAsync(CustomerFormModel model, CancellationToken ct = default)
    {
        await EnsureUniqueEmailAsync(model.Email, null, ct);

        var customer = new Customer();
        Map(model, customer);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer.Id;
    }

    public async Task UpdateAsync(CustomerFormModel model, CancellationToken ct = default)
    {
        var customer = await db.Customers.FindAsync([model.Id], ct)
                       ?? throw new NotFoundException(nameof(Customer), model.Id);

        await EnsureUniqueEmailAsync(model.Email, model.Id, ct);
        Map(model, customer);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await db.Customers.FindAsync([id], ct)
                       ?? throw new NotFoundException(nameof(Customer), id);

        var hasOpenOrders = await db.Orders.AnyAsync(o =>
            o.CustomerId == id && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed ||
                                   o.Status == OrderStatus.Shipped), ct);
        if (hasOpenOrders)
            throw new DomainException("Müşterinin tamamlanmamış siparişleri var; önce siparişleri kapatın.");

        db.Customers.Remove(customer);
        await db.SaveChangesAsync(ct);
    }

    private static void Map(CustomerFormModel model, Customer customer)
    {
        customer.FullName = model.FullName.Trim();
        customer.Email = model.Email.Trim().ToLowerInvariant();
        customer.Phone = model.Phone?.Trim();
        customer.City = model.City?.Trim();
        customer.Address = model.Address?.Trim();
    }

    private async Task EnsureUniqueEmailAsync(string email, int? excludeId, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (await db.Customers.AnyAsync(c => c.Email == normalized && c.Id != excludeId, ct))
            throw new DomainException($"'{normalized}' e-posta adresi başka bir müşteriye ait.");
    }
}
