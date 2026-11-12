using System.Data.Common;

namespace BankAbs.Data.Infrastructure;

/// <summary>Зручне читання полів результату запиту за назвою колонки.</summary>
public static class DataReaderExtensions
{
    /// <summary>Значення обов'язкового поля.</summary>
    public static T Get<T>(this DbDataReader reader, string column)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return reader.GetFieldValue<T>(reader.GetOrdinal(column));
    }

    /// <summary>Значення поля, що може бути NULL (для типів-значень).</summary>
    public static T? GetNullable<T>(this DbDataReader reader, string column)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(reader);
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<T>(ordinal);
    }

    /// <summary>Рядкове поле, що може бути NULL.</summary>
    public static string? GetNullableString(this DbDataReader reader, string column)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
