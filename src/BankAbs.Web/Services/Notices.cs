namespace BankAbs.Web.Services;

/// <summary>
/// Повідомлення про успішне виконання дії. Після збереження форми сторінка перенаправляє
/// користувача (шаблон Post/Redirect/Get) з кодом повідомлення в параметрі <c>ok</c>.
/// </summary>
public static class Notices
{
    private static readonly Dictionary<string, string> Texts = new(StringComparer.Ordinal)
    {
        ["client-created"] = "Клієнта зареєстровано",
        ["client-saved"] = "Зміни в картці клієнта збережено",
        ["client-blocked"] = "Клієнта заблоковано: операції за його рахунками заборонені",
        ["client-unblocked"] = "Клієнта розблоковано",
        ["account-opened"] = "Рахунок відкрито",
        ["account-closed"] = "Рахунок закрито",
        ["cash-done"] = "Касову операцію проведено",
        ["transfer-done"] = "Переказ виконано",
        ["deposit-opened"] = "Вклад розміщено",
        ["deposit-closed"] = "Вклад розірвано, кошти зараховано на рахунок виплати",
        ["credit-issued"] = "Кредит видано, кошти зараховано на поточний рахунок",
        ["credit-paid"] = "Платіж за кредитом зараховано",
        ["rate-saved"] = "Курс валюти змінено",
        ["product-saved"] = "Продукт збережено",
        ["user-created"] = "Користувача створено",
        ["user-saved"] = "Дані користувача збережено",
        ["password-reset"] = "Пароль змінено, блокування знято",
        ["user-unlocked"] = "Тимчасове блокування знято",
        ["roles-saved"] = "Права доступу збережено. Вони застосовуються до користувачів під час наступного запиту",
        ["settings-saved"] = "Параметри збережено",
        ["day-closed"] = "Закриття дня виконано: відсотки за вкладами нараховано, стани кредитів оновлено",
    };

    /// <summary>Текст повідомлення за кодом або <c>null</c>.</summary>
    public static string? Text(string? code) =>
        code is not null && Texts.TryGetValue(code, out var text) ? text : null;
}
