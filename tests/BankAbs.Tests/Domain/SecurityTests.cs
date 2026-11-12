using BankAbs.Domain.Security;

namespace BankAbs.Tests.Domain;

public sealed class SecurityTests
{
    [Fact]
    public void PasswordHasher_VerifiesCorrectPasswordOnly()
    {
        var hash = PasswordHasher.Hash("Secret123");

        Assert.True(PasswordHasher.Verify("Secret123", hash));
        Assert.False(PasswordHasher.Verify("secret123", hash));
        Assert.False(PasswordHasher.Verify(string.Empty, hash));
    }

    [Fact]
    public void PasswordHasher_UsesRandomSalt()
    {
        Assert.NotEqual(PasswordHasher.Hash("Secret123"), PasswordHasher.Hash("Secret123"));
    }

    [Fact]
    public void PasswordHasher_SeedHashOfDemoAdmin_IsValid()
    {
        const string seedHash = "PBKDF2-SHA256$100000$ZPGRO6aKXCNw8vt5tHLqog==$PXHuonjkeKeDys/JAsmWPEhILtVuYwQ1utyAtwuJkTE=";
        Assert.True(PasswordHasher.Verify("Admin123!", seedHash));
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("PBKDF2-SHA256$x$y$z")]
    [InlineData("PBKDF2-SHA256$1000$not-base64$not-base64")]
    public void PasswordHasher_MalformedHash_ReturnsFalse(string storedHash)
    {
        Assert.False(PasswordHasher.Verify("Admin123!", storedHash));
    }

    [Fact]
    public void PasswordPolicy_ReportsEveryViolation()
    {
        var errors = PasswordPolicy.Validate("abc", "abc", 8);

        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Contains("8 символів", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("цифру", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("логіном", StringComparison.Ordinal));
    }

    [Fact]
    public void PasswordPolicy_AcceptsStrongPassword()
    {
        Assert.Empty(PasswordPolicy.Validate("Oper2026x", "operator"));
    }

    [Fact]
    public void Permissions_PolicyNamesArePrefixed()
    {
        Assert.Equal("perm:clients.view", Permissions.Policy(Permissions.ClientsView));
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
    }
}
