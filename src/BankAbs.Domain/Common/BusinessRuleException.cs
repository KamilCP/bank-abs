namespace BankAbs.Domain.Common;

/// <summary>
/// Порушення бізнес-правила або помилка введення даних. Текст повідомлення
/// показується користувачу без змін, тому має бути сформульований зрозуміло.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    /// <summary>Створює виняток з повідомленням для користувача.</summary>
    public BusinessRuleException(string message)
        : base(message)
    {
    }

    /// <summary>Створює виняток з повідомленням для користувача і вихідною причиною.</summary>
    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
