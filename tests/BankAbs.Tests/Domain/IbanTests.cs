using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Domain;

public sealed class IbanTests
{
    [Theory]
    [InlineData("UA213223130000026007233566001")]
    [InlineData("UA21 3223 1300 0002 6007 2335 6600 1")]
    [InlineData("ua213223130000026007233566001")]
    public void IsValid_AcceptsCorrectUkrainianIban(string iban)
    {
        Assert.True(Iban.IsValid(iban));
    }

    [Theory]
    [InlineData("UA223223130000026007233566001")]
    [InlineData("UA21322313000002600723356600")]
    [InlineData("PL213223130000026007233566001")]
    [InlineData("UA21322313000002600723356600A")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsWrongChecksumLengthOrCountry(string? iban)
    {
        Assert.False(Iban.IsValid(iban));
    }

    [Theory]
    [InlineData("399001", "26200000001001")]
    [InlineData("322313", "0000026007233566001")]
    [InlineData("300001", "1")]
    public void Create_ProducesValidIbanOfLength29(string bankCode, string account)
    {
        var iban = Iban.Create(bankCode, account);

        Assert.Equal(Iban.UkraineLength, iban.Length);
        Assert.StartsWith("UA", iban);
        Assert.True(Iban.IsValid(iban));
    }

    [Fact]
    public void Create_KnownExample_ReturnsExpectedCheckDigits()
    {
        Assert.Equal("UA213223130000026007233566001", Iban.Create("322313", "0000026007233566001"));
    }

    [Fact]
    public void Create_InvalidBankCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => Iban.Create("12345", "1"));
    }

    [Fact]
    public void Format_GroupsByFourCharacters()
    {
        Assert.Equal("UA21 3223 1300 0002 6007 2335 6600 1", Iban.Format("UA213223130000026007233566001"));
    }
}
