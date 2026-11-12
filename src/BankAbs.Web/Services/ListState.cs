using System.Globalization;
using System.Text;
using BankAbs.Domain.Common;

namespace BankAbs.Web.Services;

/// <summary>
/// Стан табличної форми в рядку адреси: пошук, фільтри, сортування і сторінка.
/// Усі параметри передаються через query string, тому будь-який вигляд таблиці
/// можна відкрити за посиланням або оновити сторінку без втрати фільтрів.
/// </summary>
public sealed record ListState
{
    /// <summary>Адреса сторінки без параметрів (наприклад, «clients»).</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Рядок пошуку.</summary>
    public string? Q { get; init; }

    /// <summary>Ключ колонки сортування.</summary>
    public string? Sort { get; init; }

    /// <summary>Сортування за спаданням.</summary>
    public bool Desc { get; init; }

    /// <summary>Номер сторінки.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Фільтр за видом.</summary>
    public string? Kind { get; init; }

    /// <summary>Фільтр за станом.</summary>
    public string? Status { get; init; }

    /// <summary>Фільтр за валютою.</summary>
    public string? Currency { get; init; }

    /// <summary>Початок періоду.</summary>
    public DateOnly? From { get; init; }

    /// <summary>Кінець періоду.</summary>
    public DateOnly? To { get; init; }

    /// <summary>Ідентифікатор пов'язаного запису.</summary>
    public int? Owner { get; init; }

    /// <summary>Чи відкрито таблицю без параметрів (перше відкриття — записується в журнал).</summary>
    public bool IsInitial => Q is null && Sort is null && Page <= 1 && Kind is null && Status is null && Currency is null
                             && From is null && To is null && Owner is null;

    /// <summary>Параметри вибірки для репозиторію.</summary>
    public ListQuery ToQuery(int pageSize) => new()
    {
        Search = Q,
        SortBy = Sort,
        Descending = Desc,
        Page = Math.Max(1, Page),
        PageSize = pageSize,
        Kind = Kind,
        Status = Status,
        Currency = Currency,
        DateFrom = From,
        DateTo = To,
        OwnerId = Owner,
    };

    /// <summary>Адреса з сортуванням за колонкою (повторне натискання змінює напрям).</summary>
    public string SortUrl(string column) =>
        (this with { Sort = column, Desc = Sort == column && !Desc, Page = 1 }).Url();

    /// <summary>Адреса сторінки з номером <paramref name="page"/>.</summary>
    public string PageUrl(int page) => (this with { Page = page }).Url();

    /// <summary>Повна відносна адреса з усіма непорожніми параметрами.</summary>
    public string Url()
    {
        var parts = new List<string>();
        Add(parts, "q", Q);
        Add(parts, "sort", Sort);
        if (Desc)
        {
            parts.Add("desc=true");
        }

        if (Page > 1)
        {
            parts.Add("page=" + Page.ToString(CultureInfo.InvariantCulture));
        }

        Add(parts, "kind", Kind);
        Add(parts, "status", Status);
        Add(parts, "currency", Currency);
        Add(parts, "from", From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(parts, "to", To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add(parts, "owner", Owner?.ToString(CultureInfo.InvariantCulture));
        var sb = new StringBuilder(Path);
        if (parts.Count > 0)
        {
            sb.Append('?').Append(string.Join('&', parts));
        }

        return sb.ToString();
    }

    private static void Add(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(name + "=" + Uri.EscapeDataString(value));
        }
    }
}
