# Стандарт кодування Bank ABS

Версія 1.0 · затверджено для проєкту «Автоматизована система діяльності банку» (Bank ABS)

Стандарт ґрунтується на рекомендаціях Microsoft «C# Coding Conventions» і «.NET Runtime coding style»,
а також на загальних принципах PEP 8 (читабельність, послідовність) і C++ Programming Style Guidelines
(значущі імена, відсутність магічних чисел). Правила, які можна перевірити автоматично, записані у файлі
`.editorconfig` і перевіряються під час кожного збирання.

## 1. Загальні правила

| Правило | Значення |
|---|---|
| Мова програмування | C# 14 (.NET 10), SQL — діалект PostgreSQL (PL/pgSQL), розмітка Razor |
| Кодування файлів | UTF-8, порожній рядок у кінці файлу; кінці рядків нормалізує Git (`.gitattributes`: LF у репозиторії, CRLF у робочій копії Windows) |
| Відступи | 4 пробіли; символи табуляції не використовуються (2 пробіли — для JSON, YAML, XML) |
| Довжина рядка | не більше 130 символів |
| Мова ідентифікаторів | англійська (типи, методи, змінні, таблиці БД) |
| Мова коментарів, повідомлень і інтерфейсу | українська |
| Один тип — один файл | назва файлу збігається з назвою типу (`AnnuityCalculator.cs`) |

## 2. Вибір імен і регістр символів

Імена мають пояснювати призначення без коментаря: `monthlyPayment`, а не `mp` чи `x1`. Скорочення
допускаються лише загальновживані (`Id`, `Iban`, `Sql`, `Db`). Логічні імена починаються з `Is`, `Has`,
`Can` (`IsOpen`, `Can(permission)`), колекції — у множині (`accounts`), асинхронні методи мають суфікс
`Async` (`ListAsync`).

| Елемент | Регістр | Приклад |
|---|---|---|
| Простір імен, клас, запис, структура, перелік | PascalCase | `BankAbs.Domain.Banking`, `DepositCalculator` |
| Інтерфейс | PascalCase з префіксом `I` | `IUserContext` |
| Метод, властивість, подія | PascalCase | `MonthlyPayment()`, `BalanceBase` |
| Константа (`const`), статичне поле лише для читання | PascalCase | `MonthsPerYear`, `PersonWeights` |
| Закрите поле екземпляра | `_camelCase` (підкреслення на початку) | `_dataSource`, `_logViews` |
| Параметр, локальна змінна | camelCase | `annualRatePercent`, `issueDate` |
| Компонент Razor (сторінка, елемент інтерфейсу) | PascalCase | `ClientCard.razor`, `StatusBadge.razor` |
| CSS-клас | kebab-case | `kpi-card`, `status-badge` |
| Функція JavaScript | camelCase | `startIdleTimer` |
| Таблиця, колонка, подання БД | snake_case, таблиці в однині | `credit_schedule`, `rate_to_base`, `v_account` |
| Функція / процедура / тригер БД | префікс `fn_` / `sp_` / `trg_` + snake_case | `fn_cash_out`, `sp_close_day`, `trg_audit_row` |
| Індекс / унікальний індекс / обмеження CHECK | `ix_` / `ux_` / `ck_` | `ix_operation_time`, `ck_account_number` |
| Параметр / локальна змінна PL/pgSQL | префікс `p_` / `v_` | `p_account_id`, `v_balance` |

Знак підкреслення для розділення слів використовується лише в SQL (snake_case), у префіксі закритих полів
C# і в назвах тестових методів, що мають формат `Метод_Умова_Очікуваний результат`
(`CashOut_MoreThanBalance_IsRejected`).

## 3. Тип змінної в ідентифікаторі (угорська нотація)

Угорська нотація (`iCount`, `strName`, `x_float`) **не використовується**: C# — мова зі строгою статичною
типізацією, тип видно в оголошенні і в підказках IDE, а префікс типу застаріває при зміні типу.
Тип одиниці виміру, якщо він важливий, відображається в імені за змістом: `annualRatePercent`,
`lockMinutes`, `amountBase` (сума в базовій валюті).

Винятки — **семантичні**, а не типові префікси: об'єкти БД (`fn_`, `sp_`, `trg_`, `v_` для подань) і
змінні PL/pgSQL (`p_` — параметр, `v_` — локальна змінна). Вони потрібні, щоб імена параметрів і змінних
не збігалися з назвами колонок у SQL-запитах усередині функцій.

## 4. Відступи і розстановка дужок

* Фігурні дужки — стиль Allman: відкривна дужка блоку завжди на новому рядку на рівні оператора.
* Тіло `if`, `else`, `for`, `foreach`, `while` **завжди** береться у фігурні дужки, навіть з одного рядка
  (правило IDE0011 з рівнем помилки).
* `else`, `catch`, `finally` — з нового рядка.
* Простори імен — файлові (`namespace BankAbs.Domain.Banking;`), без додаткового рівня відступу.
* Один оператор на рядок; порожній рядок між методами і після блоку `if`/`foreach`.
* У SQL ключові слова — ВЕЛИКИМИ літерами, вкладені частини запиту — з відступом 4 пробіли;
  тіло функції PL/pgSQL — між `BEGIN` і `END` з відступом.

## 5. Пробіли у виразах

| Правило | Правильно | Неправильно |
|---|---|---|
| Бінарні оператори і присвоєння оточуються пробілами | `balance - principal` | `balance-principal` |
| Після коми — пробіл, перед комою — ні | `Rate(from, to, margin)` | `Rate(from ,to,margin)` |
| Після ключового слова — пробіл | `if (amount <= 0)` | `if(amount<=0)` |
| Між назвою методу і дужкою — без пробілу | `MonthlyPayment(amount)` | `MonthlyPayment (amount)` |
| Усередині дужок — без пробілів | `(1 + monthlyRate)` | `( 1 + monthlyRate )` |
| Після приведення типу — без пробілу | `(decimal)months` | `(decimal) months` |
| Складні вирази розбиваються дужками | `principal * rate / Percent / MonthsPerYear` у функції з назвою | один довгий рядок без пояснення |

## 6. Коментарі

* Усі відкриті (`public`) типи і члени бібліотек `BankAbs.Domain` і `BankAbs.Data` мають
  документуючі XML-коментарі `/// <summary>` (з `<param>`, `<returns>` за потреби). Для бібліотеки
  домену формується XML-файл документації (`GenerateDocumentationFile`).
* Звичайні коментарі пояснюють **чому** зроблено так, а не **що** робить рядок
  (`// Блокуємо рахунки в порядку зростання id, щоб уникнути взаємного блокування`).
* Коментар — окремим рядком перед кодом, з пробілом після `//`.
* У SQL — коментар `--` перед об'єктом і `COMMENT ON TABLE/COLUMN` для опису структури БД.
* Закоментований код і `TODO` у гілках `main` і `develop` не залишаються.

## 7. Відсутність магічних чисел

Числа в коді, крім 0, 1 і очевидних індексів, оголошуються іменованими константами з поясненням.
Бізнес-налаштування, які може змінювати банк, зберігаються не в коді, а в системних параметрах БД
(таблиця `system_parameter`: маржа конвертації, кількість спроб входу, розмір сторінки тощо).

```csharp
// Неправильно
var interest = Math.Round(balance * rate / 100 / 12, 2, MidpointRounding.AwayFromZero);

// Правильно
public static decimal MonthlyInterest(decimal baseAmount, decimal annualRatePercent) =>
    MoneyMath.RoundMoney(baseAmount * annualRatePercent / MoneyMath.Percent / MoneyMath.MonthsPerYear);
```

## 8. Інші правила

* **Модифікатори доступу** вказуються явно для всіх членів (IDE0040); поля, що не змінюються після
  конструктора, — `readonly` (IDE0044).
* **Помилки бізнес-правил** передаються винятком `BusinessRuleException` (C#) або `RAISE EXCEPTION`
  (PL/pgSQL, SQLSTATE P0001); текст — зрозумілий користувачу, без технічних деталей.
* **SQL-запити** — лише параметризовані (`@name`); назва колонки сортування від користувача не
  підставляється в SQL, а обирається з білого списку (`SqlSort.OrderBy`).
* **Асинхронність** — `async`/`await` на всьому шляху; `.Result` і `.Wait()` заборонені.
* **Нульові посилання** — увімкнено `Nullable`; попередження компілятора є помилками
  (`TreatWarningsAsErrors`).

## 9. Автоматична перевірка

| Інструмент | Що перевіряє |
|---|---|
| `.editorconfig` + `EnforceCodeStyleInBuild` | Іменування (IDE1006), дужки (IDE0011), модифікатори доступу (IDE0040), `readonly` (IDE0044), файлові простори імен (IDE0161), відступи і пробіли |
| `TreatWarningsAsErrors` | Будь-яке попередження компілятора чи аналізатора зупиняє збирання |
| Аналізатори .NET (CA) | Типові помилки якості коду |
| Рев'ю коду в Pull Request | Правила, які не перевіряються автоматично (імена, коментарі, магічні числа) |

## 10. Приклад оформлення коду

```csharp
namespace BankAbs.Domain.Banking;

/// <summary>
/// Розрахунок ануїтетного (рівними платежами) графіка погашення кредиту.
/// </summary>
public static class AnnuityCalculator
{
    /// <summary>Розмір щомісячного ануїтетного платежу.</summary>
    /// <param name="amount">Сума кредиту.</param>
    /// <param name="annualRatePercent">Річна ставка, %.</param>
    /// <param name="months">Строк у місяцях.</param>
    public static decimal MonthlyPayment(decimal amount, decimal annualRatePercent, int months)
    {
        ValidateArguments(amount, annualRatePercent, months);
        var monthlyRate = MonthlyRate(annualRatePercent);
        if (monthlyRate == 0)
        {
            return MoneyMath.RoundMoney(amount / months);
        }

        var growth = Power(1 + monthlyRate, months);
        return MoneyMath.RoundMoney(amount * monthlyRate * growth / (growth - 1));
    }

    private static decimal MonthlyRate(decimal annualRatePercent) =>
        annualRatePercent / MoneyMath.Percent / MoneyMath.MonthsPerYear;
}
```

```sql
-- Видача готівки з поточного рахунку
CREATE FUNCTION fn_cash_out(p_account_id integer, p_amount numeric, p_description varchar DEFAULT '')
RETURNS bigint
LANGUAGE plpgsql AS $$
DECLARE
    v_acc account;
BEGIN
    PERFORM fn_check_amount(p_amount);
    v_acc := fn_lock_account(p_account_id, 'current');
    IF v_acc.balance < p_amount THEN
        RAISE EXCEPTION 'Недостатньо коштів на рахунку % (залишок % %)',
            v_acc.number, fn_fmt_money(v_acc.balance), v_acc.currency_code;
    END IF;
    UPDATE account SET balance = balance - p_amount WHERE id = p_account_id;
    RETURN fn_post_operation('CASH_OUT', p_account_id, NULL, p_amount, v_acc.currency_code,
                             coalesce(nullif(p_description, ''), 'Видача готівки'));
END
$$;
```
