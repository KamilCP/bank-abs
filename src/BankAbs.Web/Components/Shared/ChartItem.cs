namespace BankAbs.Web.Components.Shared;

/// <summary>Значення для діаграми.</summary>
/// <param name="Label">Підпис категорії.</param>
/// <param name="Value">Значення.</param>
/// <param name="Color">Колір (якщо не задано — з палітри).</param>
public sealed record ChartItem(string Label, decimal Value, string? Color = null);

/// <summary>Категорійна палітра діаграм (перевірена на розрізнення при порушеннях колірного зору).</summary>
public static class ChartPalette
{
    /// <summary>Кольори за порядком використання.</summary>
    public static IReadOnlyList<string> Colors { get; } =
        ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#6250d6", "#e34948"];

    /// <summary>Колір за індексом категорії.</summary>
    public static string At(int index) => Colors[index % Colors.Count];
}
