using System.Reflection;
using System.Runtime.InteropServices;

namespace BankAbs.Web.Services;

/// <summary>
/// Відомості про версію програми для форми «Про програму» і рядка стану.
/// Номер версії, номер збірки і дата збирання записуються в атрибути збірки під час
/// збирання (див. Directory.Build.props, ціль BankAbsVersionInfo).
/// </summary>
public static class AppInfo
{
    private static readonly Assembly Assembly = typeof(AppInfo).Assembly;

    /// <summary>Назва продукту.</summary>
    public static string Product => Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Bank ABS";

    /// <summary>Повний номер версії: 0.2.0-beta.1+build.15.a1b2c3d.</summary>
    public static string InformationalVersion =>
        Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    /// <summary>Номер версії без метаданих збірки: 0.2.0-beta.1.</summary>
    public static string Version => InformationalVersion.Split('+')[0];

    /// <summary>Версія файлу: 0.2.0.15.</summary>
    public static string FileVersion => Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0.0";

    /// <summary>Номер збірки CI.</summary>
    public static string BuildNumber => Metadata("BuildNumber") ?? "0";

    /// <summary>Хеш коміту.</summary>
    public static string Commit => InformationalVersion.Contains('+', StringComparison.Ordinal)
        ? InformationalVersion.Split('+')[1].Split('.').Last()
        : "local";

    /// <summary>Дата і час збирання (UTC).</summary>
    public static DateTime? BuildDate =>
        DateTime.TryParse(Metadata("BuildDate"), System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal, out var date) ? date : null;

    /// <summary>Автор.</summary>
    public static string Copyright => Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

    /// <summary>Організація.</summary>
    public static string Company => Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;

    /// <summary>Середовище виконання.</summary>
    public static string Runtime => RuntimeInformation.FrameworkDescription + " · " + RuntimeInformation.OSDescription;

    private static string? Metadata(string key) =>
        Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value;
}
