using System.Data.Common;
using BankAbs.Data.Infrastructure;
using BankAbs.Domain.Common;

namespace BankAbs.Tests.Data;

public sealed class DataInfrastructureTests
{
    private static readonly Dictionary<string, string> Columns = new() { ["name"] = "c.full_name", ["date"] = "c.created_at" };

    [Fact]
    public void SqlSort_UnknownColumn_FallsBackToDefault_NoInjection()
    {
        var sql = SqlSort.OrderBy(Columns, "name; DROP TABLE client", false, "date", "c.id");

        Assert.Equal(" ORDER BY c.created_at ASC NULLS LAST, c.id ", sql);
    }

    [Fact]
    public void SqlSort_KnownColumnDescending()
    {
        Assert.Equal(" ORDER BY c.full_name DESC NULLS LAST, c.id ", SqlSort.OrderBy(Columns, "name", true, "date", "c.id"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData("Шевчук", "%Шевчук%")]
    [InlineData("50%_off", "%50\\%\\_off%")]
    public void LikePattern_EscapesWildcards(string? search, string? expected)
    {
        Assert.Equal(expected, SqlSort.LikePattern(search));
    }

    [Fact]
    public void ErrorTranslator_PassesBusinessRuleMessageWithoutSqlState()
    {
        var translated = DbErrorTranslator.Translate(new FakeDbException("P0001", "P0001: Недостатньо коштів на рахунку"));

        Assert.NotNull(translated);
        Assert.Equal("Недостатньо коштів на рахунку", translated.Message);
    }

    [Fact]
    public void ErrorTranslator_MapsUniqueConstraintToFriendlyText()
    {
        var translated = DbErrorTranslator.Translate(new FakeDbException("23505",
            "23505: duplicate key value violates unique constraint \"ux_client_tax_id\""));

        Assert.Equal("Клієнт з таким ідентифікаційним кодом уже зареєстрований", translated?.Message);
    }

    [Fact]
    public void ErrorTranslator_UnknownErrors_AreNotTranslated()
    {
        Assert.Null(DbErrorTranslator.Translate(new FakeDbException("08006", "connection failure")));
    }

    [Fact]
    public void ListQuery_OffsetDependsOnPageAndSize()
    {
        Assert.Equal(30, new ListQuery { Page = 3, PageSize = 15 }.Offset);
        Assert.Equal(0, new ListQuery { Page = 0, PageSize = 15 }.Offset);
    }

    [Fact]
    public void DatabaseInitializer_EmbedsAllScripts()
    {
        foreach (var name in new[] { "01_schema.sql", "02_functions.sql", "03_triggers.sql", "04_seed.sql" })
        {
            Assert.False(string.IsNullOrWhiteSpace(DatabaseInitializer.ReadScript(name)));
        }
    }

    private sealed class FakeDbException(string sqlState, string message) : DbException(message)
    {
        public override string SqlState => sqlState;
    }
}
