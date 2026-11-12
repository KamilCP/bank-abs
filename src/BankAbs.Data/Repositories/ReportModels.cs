namespace BankAbs.Data.Repositories;

/// <summary>Заголовок виписки по рахунку.</summary>
public sealed record StatementHeader(string AccountNumber, string ClientName, string TaxId, string CurrencyCode,
    string AccountType, DateOnly From, DateOnly To, decimal OpeningBalance, decimal ClosingBalance,
    decimal TotalDebit, decimal TotalCredit);

/// <summary>Підсумок операцій за типом і валютою.</summary>
public sealed record OperationTotal(string OpType, string CurrencyCode, int Count, decimal Amount, decimal AmountBase);

/// <summary>Підсумок операцій за виконавцем.</summary>
public sealed record UserTotal(string Login, string FullName, int Count, decimal AmountBase);

/// <summary>Рядок звіту про портфель за продуктом.</summary>
public sealed record PortfolioRow(string Product, string CurrencyCode, int Count, decimal Amount, decimal Current,
    decimal AverageRate, int OverdueCount, decimal AmountBase);

/// <summary>Ключові показники для головної сторінки і статистики.</summary>
public sealed record KeyIndicators(int ClientsTotal, int ClientsNew30, int OpenAccounts, decimal FundsBase,
    int DepositsActive, decimal DepositPortfolioBase, int CreditsActive, decimal CreditPortfolioBase,
    int OverdueCount, decimal OverdueDebtBase, int OpsToday, decimal TurnoverTodayBase);

/// <summary>Кількість і оборот операцій за день.</summary>
public sealed record DailyPoint(DateOnly Day, int Count, decimal TurnoverBase);

/// <summary>Значення за категорією (для діаграм).</summary>
public sealed record CategoryValue(string Category, int Count, decimal Value);

/// <summary>Залишки за валютою.</summary>
public sealed record CurrencyFunds(string CurrencyCode, decimal Current, decimal Deposit, decimal TotalBase);

/// <summary>Клієнт у рейтингу за обсягом коштів.</summary>
public sealed record TopClient(int ClientId, string Name, string ClientType, decimal FundsBase, decimal DebtBase);
