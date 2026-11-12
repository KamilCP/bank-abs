namespace BankAbs.Data.Infrastructure;

/// <summary>
/// Побудова виразу ORDER BY за білим списком колонок. Назва колонки від користувача
/// ніколи не підставляється в SQL напряму — лише вираз зі словника.
/// </summary>
public static class SqlSort
{
    /// <summary>Повертає фрагмент «ORDER BY ...» для ключа сортування.</summary>
    /// <param name="columns">Допустимі ключі і відповідні SQL-вирази.</param>
    /// <param name="sortBy">Ключ, обраний користувачем.</param>
    /// <param name="descending">Напрям сортування.</param>
    /// <param name="defaultKey">Ключ за замовчуванням.</param>
    /// <param name="tieBreaker">Додатковий вираз для однозначного порядку.</param>
    public static string OrderBy(IReadOnlyDictionary<string, string> columns, string? sortBy, bool descending,
        string defaultKey, string tieBreaker)
    {
        ArgumentNullException.ThrowIfNull(columns);
        var expression = sortBy is not null && columns.TryGetValue(sortBy, out var column) ? column : columns[defaultKey];
        var direction = descending ? "DESC NULLS LAST" : "ASC NULLS LAST";
        return $" ORDER BY {expression} {direction}, {tieBreaker} ";
    }

    /// <summary>Шаблон пошуку для ILIKE з екрануванням спецсимволів.</summary>
    public static string? LikePattern(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var escaped = search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
        return "%" + escaped + "%";
    }
}
