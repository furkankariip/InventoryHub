using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InventoryHub.Web.Infrastructure;

public static class TempDataExtensions
{
    public const string SuccessKey = "Alert.Success";
    public const string ErrorKey = "Alert.Error";

    public static void SetSuccess(this ITempDataDictionary tempData, string message) => tempData[SuccessKey] = message;

    public static void SetError(this ITempDataDictionary tempData, string message) => tempData[ErrorKey] = message;
}
