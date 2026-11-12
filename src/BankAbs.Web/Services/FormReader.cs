using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace BankAbs.Web.Services;

/// <summary>
/// Читання полів HTML-форми (POST) з перевіркою формату. Числа приймаються з крапкою або комою.
/// </summary>
public sealed class FormReader(IFormCollection form)
{
    /// <summary>Рядкове поле (обрізане) або порожній рядок.</summary>
    public string Text(string name) => form[name].ToString().Trim();

    /// <summary>Усі значення поля з повторюваною назвою (група прапорців).</summary>
    public IReadOnlyList<string> All(string name) =>
        form[name].Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).ToList();

    /// <summary>Ціле число або <c>null</c>.</summary>
    public int? Int(string name) =>
        int.TryParse(Text(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>Число з дробовою частиною або <c>null</c>.</summary>
    public decimal? Decimal(string name)
    {
        var text = new string(Text(name).Where(c => !char.IsWhiteSpace(c)).ToArray()).Replace(',', '.');
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>Дата у форматі yyyy-MM-dd (поле type="date") або <c>null</c>.</summary>
    public DateOnly? Date(string name) =>
        DateOnly.TryParseExact(Text(name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : null;

    /// <summary>Прапорець (checkbox): поле присутнє — <c>true</c>.</summary>
    public bool Bool(string name) => form.ContainsKey(name) && Text(name) is "on" or "true";
}
