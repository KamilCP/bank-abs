namespace BankAbs.Domain.Security;

/// <summary>
/// Коди функцій системи, доступ до яких налаштовується для ролей
/// (таблиці <c>app_permission</c> і <c>role_permission</c>).
/// </summary>
public static class Permissions
{
    /// <summary>Тип claim, у якому зберігаються права користувача після входу.</summary>
    public const string ClaimType = "bankabs/permission";

    /// <summary>Префікс назви політики авторизації для права.</summary>
    public const string PolicyPrefix = "perm:";

    /// <summary>Перегляд клієнтів.</summary>
    public const string ClientsView = "clients.view";

    /// <summary>Додавання і редагування клієнтів.</summary>
    public const string ClientsEdit = "clients.edit";

    /// <summary>Перегляд рахунків.</summary>
    public const string AccountsView = "accounts.view";

    /// <summary>Відкриття і закриття рахунків.</summary>
    public const string AccountsManage = "accounts.manage";

    /// <summary>Касові операції (внесення і видача готівки).</summary>
    public const string CashOperate = "cash.operate";

    /// <summary>Перекази між рахунками.</summary>
    public const string TransfersMake = "transfers.make";

    /// <summary>Перегляд журналу операцій.</summary>
    public const string OperationsView = "operations.view";

    /// <summary>Перегляд депозитів.</summary>
    public const string DepositsView = "deposits.view";

    /// <summary>Відкриття, розірвання депозитів і нарахування відсотків.</summary>
    public const string DepositsManage = "deposits.manage";

    /// <summary>Перегляд кредитів.</summary>
    public const string CreditsView = "credits.view";

    /// <summary>Видача кредитів і прийом платежів.</summary>
    public const string CreditsManage = "credits.manage";

    /// <summary>Формування звітів.</summary>
    public const string ReportsView = "reports.view";

    /// <summary>Перегляд статистики.</summary>
    public const string StatsView = "stats.view";

    /// <summary>Редагування довідників (валюти і курси, продукти).</summary>
    public const string DirectoriesEdit = "directories.edit";

    /// <summary>Ведення користувачів.</summary>
    public const string AdminUsers = "admin.users";

    /// <summary>Налаштування ролей і прав доступу.</summary>
    public const string AdminRoles = "admin.roles";

    /// <summary>Перегляд журналу системи.</summary>
    public const string AdminLog = "admin.log";

    /// <summary>Зміна системних параметрів і закриття дня.</summary>
    public const string AdminSettings = "admin.settings";

    /// <summary>Назва політики авторизації для права.</summary>
    public static string Policy(string permission) => PolicyPrefix + permission;

    /// <summary>Усі коди прав (використовується для реєстрації політик).</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ClientsView, ClientsEdit, AccountsView, AccountsManage, CashOperate, TransfersMake, OperationsView,
        DepositsView, DepositsManage, CreditsView, CreditsManage, ReportsView, StatsView, DirectoriesEdit,
        AdminUsers, AdminRoles, AdminLog, AdminSettings,
    ];
}
