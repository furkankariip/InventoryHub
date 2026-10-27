using InventoryHub.Application.Common;
using InventoryHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InventoryHub.Web.Infrastructure;

/// <summary>
/// Uygulama istisnalarını merkezi olarak ele alır:
/// NotFoundException → 404 sayfası, DomainException → hata mesajıyla bir önceki sayfaya dönüş.
/// </summary>
public class ApplicationExceptionFilter(ITempDataDictionaryFactory tempDataFactory) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case NotFoundException:
                context.Result = new NotFoundResult();
                context.ExceptionHandled = true;
                break;

            case DomainException ex:
                tempDataFactory.GetTempData(context.HttpContext).SetError(ex.Message);
                var referer = context.HttpContext.Request.Headers.Referer.ToString();
                context.Result = Uri.TryCreate(referer, UriKind.Absolute, out var uri)
                                 && uri.Host == context.HttpContext.Request.Host.Host
                    ? new RedirectResult(uri.PathAndQuery)
                    : new RedirectToActionResult("Index", "Home", null);
                context.ExceptionHandled = true;
                break;
        }
    }
}
