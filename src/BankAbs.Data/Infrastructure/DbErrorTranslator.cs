using System.Data.Common;
using System.Text.RegularExpressions;
using BankAbs.Domain.Common;

namespace BankAbs.Data.Infrastructure;

/// <summary>
/// Перетворення помилок сервера БД на повідомлення для користувача.
/// Помилки бізнес-правил (RAISE EXCEPTION у функціях, SQLSTATE P0001) передаються без змін.
/// </summary>
public static partial class DbErrorTranslator
{
    private const string RaiseException = "P0001";
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string CheckViolation = "23514";
    private const string SerializationFailure = "40001";
    private const string DeadlockDetected = "40P01";

    private static readonly Dictionary<string, string> UniqueMessages = new(StringComparer.Ordinal)
    {
        ["ux_client_tax_id"] = "Клієнт з таким ідентифікаційним кодом уже зареєстрований",
        ["ux_app_user_login"] = "Користувач з таким логіном уже існує",
        ["deposit_product_name_key"] = "Депозитний продукт з такою назвою вже існує",
        ["credit_product_name_key"] = "Кредитний продукт з такою назвою вже існує",
    };

    /// <summary>
    /// Повертає <see cref="BusinessRuleException"/> для відомих помилок або <c>null</c>,
    /// якщо помилку слід передати далі без змін.
    /// </summary>
    public static BusinessRuleException? Translate(DbException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var message = StripSqlState(exception.Message);
        return exception.SqlState switch
        {
            RaiseException => new BusinessRuleException(message, exception),
            UniqueViolation => new BusinessRuleException(
                UniqueMessages.FirstOrDefault(p => message.Contains(p.Key, StringComparison.Ordinal)).Value
                ?? "Запис з таким значенням уже існує", exception),
            ForeignKeyViolation => new BusinessRuleException(
                "Операцію не виконано: запис пов'язаний з іншими даними", exception),
            CheckViolation => new BusinessRuleException(
                "Дані не відповідають правилам перевірки (" + ConstraintName(message) + ")", exception),
            SerializationFailure or DeadlockDetected => new BusinessRuleException(
                "Дані одночасно змінюються іншим користувачем. Повторіть операцію", exception),
            _ => null,
        };
    }

    /// <summary>Прибирає префікс «XXXXX: » з коду помилки, який додає постачальник даних.</summary>
    public static string StripSqlState(string message) => SqlStatePrefix().Replace(message, string.Empty);

    private static string ConstraintName(string message)
    {
        var match = ConstraintRegex().Match(message);
        return match.Success ? match.Groups[1].Value : "обмеження цілісності";
    }

    [GeneratedRegex(@"^[0-9A-Z]{5}: ")]
    private static partial Regex SqlStatePrefix();

    [GeneratedRegex("constraint \"([^\"]+)\"")]
    private static partial Regex ConstraintRegex();
}
