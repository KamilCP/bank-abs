using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Database;

/// <summary>Інтеграційні тести функцій, процедур і тригерів PostgreSQL.</summary>
public sealed class ServerFunctionTests
{
    private const string NewClientSql = """
        INSERT INTO client (client_type, full_name, tax_id, birth_date)
        VALUES ('P', 'Тест Тестович', lpad((floor(random() * 1e9))::bigint::text, 10, '9'), '1990-01-01') RETURNING id
        """;

    [DbFact]
    public async Task GeneratedAccountNumbers_AreValidIban()
    {
        await using var db = await DbTestSession.StartAsync();
        for (var i = 0; i < 20; i++)
        {
            var number = await db.ScalarAsync<string>("SELECT fn_generate_account_number('P', 'current')");
            Assert.True(Iban.IsValid(number), number);
        }
    }

    [DbFact]
    public async Task AnnuityPayment_OnServer_MatchesDomainCalculator()
    {
        await using var db = await DbTestSession.StartAsync();
        foreach (var (amount, rate, months) in new[] { (100000m, 12m, 12), (30000m, 29.9m, 12), (65000m, 34.9m, 18), (1200m, 0m, 12) })
        {
            var server = await db.ScalarAsync<decimal>("SELECT fn_annuity_payment(@a, @r, @m)", ("a", amount), ("r", rate), ("m", months));
            Assert.Equal(AnnuityCalculator.MonthlyPayment(amount, rate, months), server);
        }
    }

    [DbFact]
    public async Task CashOut_MoreThanBalance_IsRejected_AndBalanceUnchanged()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        await db.ScalarAsync<long>("SELECT fn_cash_in(@a, 100, 'тест')", ("a", account));

        var error = await db.ErrorAsync("SELECT fn_cash_out(@a, 200, 'тест')", ("a", account));

        Assert.Contains("Недостатньо коштів", error, StringComparison.Ordinal);
        Assert.Equal(100m, await db.ScalarAsync<decimal>("SELECT balance FROM account WHERE id = @a", ("a", account)));
    }

    [DbFact]
    public async Task Amount_WithFractionOfKopeck_IsRejected()
    {
        // Дефект, знайдений під час первинного тестування бета-версії: сума 123.456 списувалася
        // з рахунку з іншим округленням, ніж записувалася в операцію (розбіжність у копійках)
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));

        var error = await db.ErrorAsync("SELECT fn_cash_in(@a, 123.456, '')", ("a", account));

        Assert.Contains("не більше двох знаків", error, StringComparison.Ordinal);
    }

    [DbFact]
    public async Task Statement_BalancesAreConsistentWithAccounts()
    {
        await using var db = await DbTestSession.StartAsync();
        var mismatches = await db.ScalarAsync<long>("""
            SELECT count(*) FROM account
             WHERE account_type <> 'credit'
               AND (fn_account_balance_on(id, opened_at) <> 0 OR fn_account_balance_on(id, fn_today() + 1) <> balance)
            """);

        Assert.Equal(0L, mismatches);
    }

    [DbFact]
    public async Task Transfer_BetweenCurrencies_UsesBankRateWithMargin()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var uah = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        var usd = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'USD')", ("c", client));
        await db.ScalarAsync<long>("SELECT fn_cash_in(@a, 10000, '')", ("a", uah));
        var usdRate = await db.ScalarAsync<decimal>("SELECT rate_to_base FROM currency WHERE code = 'USD'");
        var margin = await db.ScalarAsync<decimal>("SELECT fn_param_numeric('fx.margin_percent')");

        await db.ScalarAsync<long>("SELECT fn_transfer(@f, @t, 4170, '')", ("f", uah), ("t", usd));

        var expected = CurrencyConverter.Convert(4170m, CurrencyConverter.Rate(1m, usdRate, margin));
        Assert.Equal(expected, await db.ScalarAsync<decimal>("SELECT balance FROM account WHERE id = @a", ("a", usd)));
        Assert.Equal(5830m, await db.ScalarAsync<decimal>("SELECT balance FROM account WHERE id = @a", ("a", uah)));
    }

    [DbFact]
    public async Task IssueCredit_CreatesScheduleEqualToDomainSchedule()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        var product = await db.ScalarAsync<int>("SELECT id FROM credit_product WHERE name = 'Споживчий'");

        var credit = await db.ScalarAsync<int>("SELECT fn_issue_credit(@c, @p, 30000, 12, @a)",
            ("c", client), ("p", product), ("a", account));

        var rate = await db.ScalarAsync<decimal>("SELECT annual_rate FROM credit WHERE id = @id", ("id", credit));
        var issueDate = DateOnly.FromDateTime(await db.ScalarAsync<DateTime>("SELECT fn_today()"));
        var expected = AnnuityCalculator.BuildSchedule(30000m, rate, 12, issueDate);
        var rows = await db.RowsAsync("SELECT payment, principal_part, interest_part, balance_after FROM credit_schedule WHERE credit_id = @id ORDER BY payment_no",
            ("id", credit));
        Assert.Equal(expected.Count, rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            Assert.Equal(expected[i].Payment, Convert.ToDecimal(rows[i][0], System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal(expected[i].BalanceAfter, Convert.ToDecimal(rows[i][3], System.Globalization.CultureInfo.InvariantCulture));
        }

        Assert.Equal(30000m, await db.ScalarAsync<decimal>("SELECT balance FROM account WHERE id = @a", ("a", account)));
    }

    [DbFact]
    public async Task CreditPayment_ReducesDebtByPrincipalPart()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        var product = await db.ScalarAsync<int>("SELECT id FROM credit_product WHERE name = 'Споживчий'");
        var credit = await db.ScalarAsync<int>("SELECT fn_issue_credit(@c, @p, 30000, 12, @a)", ("c", client), ("p", product), ("a", account));
        var principal = await db.ScalarAsync<decimal>("SELECT principal_part FROM credit_schedule WHERE credit_id = @id AND payment_no = 1", ("id", credit));

        await db.ScalarAsync<long>("SELECT fn_credit_payment(@id)", ("id", credit));

        Assert.Equal(30000m - principal, await db.ScalarAsync<decimal>("SELECT debt FROM v_credit WHERE id = @id", ("id", credit)));
        Assert.Equal(1, await db.ScalarAsync<int>("SELECT paid_count FROM v_credit WHERE id = @id", ("id", credit)));
    }

    [DbFact]
    public async Task DepositInterest_AccruedMonthly_MatchesDomainProjection()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        // Вклад відкривається «90 днів тому» (параметр bankabs.demo_time), потім нараховуються відсотки на сьогодні
        await db.ExecuteAsync("SELECT set_config('bankabs.demo_time', (now() - interval '3 months')::text, true)");
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        await db.ScalarAsync<long>("SELECT fn_cash_in(@a, 50000, '')", ("a", account));
        var product = await db.ScalarAsync<int>("SELECT id FROM deposit_product WHERE name = 'Капітал+'");
        var deposit = await db.ScalarAsync<int>("SELECT fn_open_deposit(@c, @p, 20000, @a)", ("c", client), ("p", product), ("a", account));
        await db.ExecuteAsync("SELECT set_config('bankabs.demo_time', '', true)");

        await db.ScalarAsync<int>("SELECT fn_accrue_deposit_interest(fn_today())");

        var projection = DepositCalculator.Project(20000m, 14.5m, 3, capitalization: true);
        Assert.Equal(projection.TotalInterest, await db.ScalarAsync<decimal>("SELECT interest_paid FROM deposit WHERE id = @id", ("id", deposit)));
        Assert.Equal(projection.FinalAmount, await db.ScalarAsync<decimal>("SELECT current_amount FROM v_deposit WHERE id = @id", ("id", deposit)));
    }

    [DbFact]
    public async Task AuditTrigger_RecordsChangedFieldsAndUser()
    {
        await using var db = await DbTestSession.StartAsync(userId: 1);
        var client = await db.ScalarAsync<int>(NewClientSql);

        await db.ExecuteAsync("UPDATE client SET phone = '+380501234567' WHERE id = @id", ("id", client));

        var rows = await db.RowsAsync("""
            SELECT user_id, details -> 'phone' ->> 'new' FROM audit_log
             WHERE table_name = 'client' AND event_type = 'UPDATE' AND record_id = @id::text
            """, ("id", client));
        var row = Assert.Single(rows);
        Assert.Equal(1, Convert.ToInt32(row[0], System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("+380501234567", row[1]);
    }

    [DbFact]
    public async Task Operations_CannotBeChangedOrDeleted()
    {
        await using var db = await DbTestSession.StartAsync();
        var error = await db.ErrorAsync("UPDATE operation SET amount = amount + 1 WHERE id = (SELECT min(id) FROM operation)");

        Assert.Contains("не можна змінювати", error, StringComparison.Ordinal);
    }

    [DbFact]
    public async Task AccountWithBalance_CannotBeClosed()
    {
        await using var db = await DbTestSession.StartAsync();
        var client = await db.ScalarAsync<int>(NewClientSql);
        var account = await db.ScalarAsync<int>("SELECT fn_open_account(@c, 'UAH')", ("c", client));
        await db.ScalarAsync<long>("SELECT fn_cash_in(@a, 1, '')", ("a", account));

        var error = await db.ErrorAsync("SELECT fn_close_account(@a)", ("a", account));

        Assert.Contains("закриття неможливе", error, StringComparison.Ordinal);
    }

    [DbFact]
    public async Task LastActiveAdministrator_CannotBeBlocked()
    {
        await using var db = await DbTestSession.StartAsync();
        await db.ExecuteAsync("UPDATE app_user SET is_active = false WHERE role_code = 'admin' AND id <> (SELECT min(id) FROM app_user WHERE role_code = 'admin')");

        var error = await db.ErrorAsync("UPDATE app_user SET is_active = false WHERE id = (SELECT min(id) FROM app_user WHERE role_code = 'admin')");

        Assert.Contains("останнього активного адміністратора", error, StringComparison.Ordinal);
    }

    [DbFact]
    public async Task SystemParameter_WrongFormat_IsRejectedByTrigger()
    {
        await using var db = await DbTestSession.StartAsync();
        var error = await db.ErrorAsync("UPDATE system_parameter SET value = 'abc' WHERE key = 'ui.page_size'");

        Assert.Contains("цілим числом", error, StringComparison.Ordinal);
    }
}
