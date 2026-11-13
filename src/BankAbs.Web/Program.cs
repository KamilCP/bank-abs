using System.Data.Common;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using BankAbs.Data.Infrastructure;
using BankAbs.Data.Repositories;
using BankAbs.Domain.Entities;
using BankAbs.Web.Components;
using BankAbs.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = Fmt.Culture;
CultureInfo.DefaultThreadCurrentUICulture = Fmt.Culture;

// Підключення до PostgreSQL: рядок зберігається в User Secrets або змінній середовища
// ConnectionStrings__BankAbs, у репозиторії — лише рядок без пароля.
var connectionString = builder.Configuration.GetConnectionString("BankAbs")
    ?? throw new InvalidOperationException("Не задано рядок підключення ConnectionStrings:BankAbs");
builder.Services.AddSingleton<DbDataSource>(_ => NpgsqlDataSource.Create(connectionString));

// Кирилиця в HTML виводиться без перетворення на числові посилання (&#x...;)
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// Дані і сервіси
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<IUserContext>(sp => sp.GetRequiredService<CurrentUserContext>());
builder.Services.AddScoped<Database>();
builder.Services.AddScoped<ClientRepository>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<OperationRepository>();
builder.Services.AddScoped<DepositRepository>();
builder.Services.AddScoped<CreditRepository>();
builder.Services.AddScoped<DirectoryRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RoleRepository>();
builder.Services.AddScoped<AuditRepository>();
builder.Services.AddScoped<ParameterRepository>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<StatsRepository>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddSingleton<DatabaseInitializer>();

// Автентифікація (cookie) і авторизація за правами доступу до функцій
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BankAbs.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = SessionValidator.ValidateAsync;
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddCascadingAuthenticationState();

// Інтерфейс: компоненти Blazor зі статичним серверним рендерингом (SSR)
builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Заголовки безпеки: заборона вбудовування у фрейми, підміни типу вмісту і виконання сторонніх скриптів
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "same-origin";
    headers.ContentSecurityPolicy = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; frame-ancestors 'none'; form-action 'self'";
    await next(context);
});
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();

// Користувач поточного HTTP-запиту (для статичних сторінок і кінцевих точок)
app.Use(async (context, next) =>
{
    context.RequestServices.GetRequiredService<CurrentUserContext>().Set(context.User);
    await next(context);
});
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Вихід із системи: запис у журнал і видалення cookie. Cookie має SameSite=Strict,
// тому перехід за посиланням з іншого сайту не може завершити сеанс користувача.
app.MapGet("/logout", async (HttpContext context, AuditRepository audit, string? reason) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        await audit.LogAsync(AuditEventTypes.Logout, "app_user",
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            reason == "timeout" ? "Вихід із системи (завершення сеансу через неактивність)" : "Вихід із системи");
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect(reason == "timeout" ? "/login?timeout=1" : "/login");
});

// Створення БД під час першого запуску
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    if (await initializer.EnsureCreatedAsync())
    {
        app.Logger.LogInformation("Базу даних створено: таблиці, функції, тригери і початкові дані");
    }
}

app.Run();
