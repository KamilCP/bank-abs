namespace BankAbs.Domain.Common;

/// <summary>
/// Параметри вибірки для табличних форм: пошук, сортування, сторінка і фільтри.
/// Назви колонок сортування перевіряються репозиторієм за білим списком.
/// </summary>
public sealed class ListQuery
{
    /// <summary>Розмір сторінки за замовчуванням, якщо параметр системи недоступний.</summary>
    public const int DefaultPageSize = 15;

    /// <summary>Рядок пошуку (частина назви, номера, коду).</summary>
    public string? Search { get; set; }

    /// <summary>Ключ колонки сортування.</summary>
    public string? SortBy { get; set; }

    /// <summary>Сортування за спаданням.</summary>
    public bool Descending { get; set; }

    /// <summary>Номер сторінки, починаючи з 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Кількість записів на сторінці.</summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Фільтр за станом (значення залежить від таблиці).</summary>
    public string? Status { get; set; }

    /// <summary>Фільтр за видом/типом запису.</summary>
    public string? Kind { get; set; }

    /// <summary>Фільтр за валютою.</summary>
    public string? Currency { get; set; }

    /// <summary>Початок періоду (включно).</summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>Кінець періоду (включно).</summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>Фільтр за ідентифікатором пов'язаного запису (клієнт, рахунок, користувач).</summary>
    public int? OwnerId { get; set; }

    /// <summary>Зсув першого запису сторінки.</summary>
    public int Offset => (Math.Max(1, Page) - 1) * PageSize;
}
