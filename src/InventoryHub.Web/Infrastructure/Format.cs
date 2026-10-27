using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using InventoryHub.Domain.Enums;

namespace InventoryHub.Web.Infrastructure;

/// <summary>Görünümlerde kullanılan Türkçe biçimlendirme yardımcıları.</summary>
public static class Format
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo TurkeyTime = FindTurkeyTimeZone();

    public static string Money(decimal value) => value.ToString("C2", Turkish);

    public static string Number(int value) => value.ToString("N0", Turkish);

    public static string Date(DateTime utc) => ToLocal(utc).ToString("dd.MM.yyyy", Turkish);

    public static string Timestamp(DateTime utc) => ToLocal(utc).ToString("dd.MM.yyyy HH:mm", Turkish);

    public static string DisplayName(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();

    public static string BadgeClass(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "text-bg-warning",
        OrderStatus.Confirmed => "text-bg-info",
        OrderStatus.Shipped => "text-bg-primary",
        OrderStatus.Delivered => "text-bg-success",
        OrderStatus.Cancelled => "text-bg-secondary",
        _ => "text-bg-light"
    };

    public static string BadgeClass(this StockMovementType type) => type switch
    {
        StockMovementType.Purchase => "text-bg-success",
        StockMovementType.Sale => "text-bg-primary",
        StockMovementType.Return => "text-bg-info",
        _ => "text-bg-warning"
    };

    private static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TurkeyTime);

    private static TimeZoneInfo FindTurkeyTimeZone()
    {
        // Windows ve Linux (Docker) farklı saat dilimi kimlikleri kullanır.
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
                return tz;
        }
        return TimeZoneInfo.Utc;
    }
}
