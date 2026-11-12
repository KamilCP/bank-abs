namespace BankAbs.Domain.Common;

/// <summary>Сторінка результатів вибірки з загальною кількістю записів.</summary>
/// <typeparam name="T">Тип запису.</typeparam>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Кількість сторінок (не менше 1).</summary>
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);

    /// <summary>Порожній результат.</summary>
    public static PagedResult<T> Empty(int pageSize) => new([], 0, 1, pageSize);
}
