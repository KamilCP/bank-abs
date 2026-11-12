namespace BankAbs.Domain.Entities;

/// <summary>Коди типів клієнтів (поле <c>client.client_type</c>).</summary>
public static class ClientTypes
{
    /// <summary>Фізична особа.</summary>
    public const string Person = "P";

    /// <summary>Юридична особа.</summary>
    public const string Legal = "L";

    /// <summary>Назва типу для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Person => "Фізична особа",
        Legal => "Юридична особа",
        _ => code ?? string.Empty,
    };
}

/// <summary>Стани клієнта.</summary>
public static class ClientStatuses
{
    /// <summary>Активний.</summary>
    public const string Active = "active";

    /// <summary>Заблокований (операції заборонені).</summary>
    public const string Blocked = "blocked";

    /// <summary>Назва стану для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Active => "Активний",
        Blocked => "Заблокований",
        _ => code ?? string.Empty,
    };
}

/// <summary>Типи рахунків.</summary>
public static class AccountTypes
{
    /// <summary>Поточний рахунок.</summary>
    public const string Current = "current";

    /// <summary>Депозитний рахунок.</summary>
    public const string Deposit = "deposit";

    /// <summary>Кредитний рахунок (залишок — заборгованість).</summary>
    public const string Credit = "credit";

    /// <summary>Назва типу для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Current => "Поточний",
        Deposit => "Депозитний",
        Credit => "Кредитний",
        _ => code ?? string.Empty,
    };
}

/// <summary>Стани рахунку.</summary>
public static class AccountStatuses
{
    /// <summary>Відкритий.</summary>
    public const string Open = "open";

    /// <summary>Закритий.</summary>
    public const string Closed = "closed";

    /// <summary>Назва стану для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Open => "Відкритий",
        Closed => "Закритий",
        _ => code ?? string.Empty,
    };
}

/// <summary>Типи фінансових операцій (поле <c>operation.op_type</c>).</summary>
public static class OperationTypes
{
    /// <summary>Внесення готівки.</summary>
    public const string CashIn = "CASH_IN";

    /// <summary>Видача готівки.</summary>
    public const string CashOut = "CASH_OUT";

    /// <summary>Переказ між рахунками.</summary>
    public const string Transfer = "TRANSFER";

    /// <summary>Розміщення вкладу.</summary>
    public const string DepositOpen = "DEPOSIT_OPEN";

    /// <summary>Нарахування відсотків за вкладом.</summary>
    public const string DepositInterest = "DEPOSIT_INTEREST";

    /// <summary>Повернення вкладу.</summary>
    public const string DepositClose = "DEPOSIT_CLOSE";

    /// <summary>Видача кредиту.</summary>
    public const string CreditIssue = "CREDIT_ISSUE";

    /// <summary>Платіж за кредитом.</summary>
    public const string CreditPayment = "CREDIT_PAYMENT";

    /// <summary>Усі типи в порядку відображення.</summary>
    public static IReadOnlyList<string> All { get; } =
        [CashIn, CashOut, Transfer, DepositOpen, DepositInterest, DepositClose, CreditIssue, CreditPayment];

    /// <summary>Назва типу для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        CashIn => "Внесення готівки",
        CashOut => "Видача готівки",
        Transfer => "Переказ",
        DepositOpen => "Розміщення вкладу",
        DepositInterest => "Відсотки за вкладом",
        DepositClose => "Повернення вкладу",
        CreditIssue => "Видача кредиту",
        CreditPayment => "Платіж за кредитом",
        _ => code ?? string.Empty,
    };
}

/// <summary>Стани депозиту.</summary>
public static class DepositStatuses
{
    /// <summary>Діючий.</summary>
    public const string Active = "active";

    /// <summary>Закритий після завершення строку.</summary>
    public const string Closed = "closed";

    /// <summary>Розірваний достроково.</summary>
    public const string ClosedEarly = "closed_early";

    /// <summary>Назва стану для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Active => "Діючий",
        Closed => "Завершений",
        ClosedEarly => "Розірваний достроково",
        _ => code ?? string.Empty,
    };
}

/// <summary>Стани кредиту.</summary>
public static class CreditStatuses
{
    /// <summary>Діючий, без прострочки.</summary>
    public const string Active = "active";

    /// <summary>Є прострочений платіж.</summary>
    public const string Overdue = "overdue";

    /// <summary>Погашений.</summary>
    public const string Repaid = "repaid";

    /// <summary>Назва стану для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Active => "Діючий",
        Overdue => "Прострочений",
        Repaid => "Погашений",
        _ => code ?? string.Empty,
    };
}

/// <summary>Типи подій журналу системи.</summary>
public static class AuditEventTypes
{
    /// <summary>Вхід у систему.</summary>
    public const string Login = "LOGIN";

    /// <summary>Вихід із системи.</summary>
    public const string Logout = "LOGOUT";

    /// <summary>Невдала спроба входу.</summary>
    public const string LoginFailed = "LOGIN_FAILED";

    /// <summary>Додавання запису (тригер БД).</summary>
    public const string Insert = "INSERT";

    /// <summary>Зміна запису (тригер БД).</summary>
    public const string Update = "UPDATE";

    /// <summary>Видалення запису (тригер БД).</summary>
    public const string Delete = "DELETE";

    /// <summary>Робота з таблицею (відкриття форми, пошук).</summary>
    public const string View = "VIEW";

    /// <summary>Формування звіту.</summary>
    public const string Report = "REPORT";

    /// <summary>Службова дія (закриття дня тощо).</summary>
    public const string Action = "ACTION";

    /// <summary>Усі типи подій.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Login, Logout, LoginFailed, Insert, Update, Delete, View, Report, Action];

    /// <summary>Назва типу для відображення.</summary>
    public static string Title(string? code) => code switch
    {
        Login => "Вхід",
        Logout => "Вихід",
        LoginFailed => "Невдалий вхід",
        Insert => "Додавання",
        Update => "Зміна",
        Delete => "Видалення",
        View => "Перегляд таблиці",
        Report => "Звіт",
        Action => "Дія",
        _ => code ?? string.Empty,
    };
}
