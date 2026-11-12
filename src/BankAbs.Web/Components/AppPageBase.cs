using BankAbs.Domain.Common;
using BankAbs.Web.Services;
using Microsoft.AspNetCore.Components;

namespace BankAbs.Web.Components;

/// <summary>
/// Базовий клас сторінок: доступ до запиту HTTP, поточного користувача, читання форм
/// і перенаправлення після успішної дії (шаблон Post/Redirect/Get).
/// </summary>
public abstract class AppPageBase : ComponentBase
{
    /// <summary>Поточний запит HTTP (статичний серверний рендеринг).</summary>
    [CascadingParameter]
    protected HttpContext HttpContext { get; set; } = default!;

    /// <summary>Навігація.</summary>
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    /// <summary>Поточний користувач.</summary>
    [Inject]
    protected CurrentUserContext CurrentUser { get; set; } = default!;

    /// <summary>Журнал подій інтерфейсу.</summary>
    [Inject]
    protected AuditService Audit { get; set; } = default!;

    /// <summary>Код повідомлення про успіх після перенаправлення.</summary>
    [SupplyParameterFromQuery(Name = "ok")]
    protected string? OkCode { get; set; }

    /// <summary>Помилки введення або виконання дії для показу на сторінці.</summary>
    protected List<string> Errors { get; } = [];

    /// <summary>Чи є поточний запит відправленням форми.</summary>
    protected bool IsPost => HttpMethods.IsPost(HttpContext.Request.Method);

    /// <summary>Поля відправленої форми.</summary>
    protected FormReader Form => new(HttpContext.Request.Form);

    /// <summary>Текст повідомлення про успіх.</summary>
    protected string? SuccessText => Notices.Text(OkCode);

    /// <summary>Перенаправлення на адресу з кодом повідомлення.</summary>
    protected void RedirectWithNotice(string url, string noticeCode)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        Navigation.NavigateTo(url + separator + "ok=" + noticeCode);
    }

    /// <summary>
    /// Виконує дію і перехоплює порушення бізнес-правил (повідомлення сервера БД),
    /// додаючи їх до <see cref="Errors"/>.
    /// </summary>
    /// <returns><c>true</c>, якщо дію виконано без помилок.</returns>
    protected async Task<bool> TryAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return true;
        }
        catch (BusinessRuleException ex)
        {
            Errors.Add(ex.Message);
            return false;
        }
    }

    /// <summary>Розмір сторінки таблиць (параметр ui.page_size).</summary>
    protected static int PageSizeOrDefault(int value) => value is > 0 and <= 200 ? value : ListQuery.DefaultPageSize;
}
