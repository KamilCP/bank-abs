using BankAbs.Domain.Banking;

namespace BankAbs.Tests.Domain;

public sealed class TaxIdValidatorTests
{
    [Theory]
    [InlineData("3762629459")]
    [InlineData("3421384640")]
    [InlineData("2485204639")]
    public void IsValidPersonCode_AcceptsCodesWithCorrectControlDigit(string code)
    {
        Assert.True(TaxIdValidator.IsValidPersonCode(code));
    }

    [Theory]
    [InlineData("3762629458")]
    [InlineData("376262945")]
    [InlineData("37626294599")]
    [InlineData("37626A9459")]
    [InlineData(null)]
    public void IsValidPersonCode_RejectsWrongCodes(string? code)
    {
        Assert.False(TaxIdValidator.IsValidPersonCode(code));
    }

    [Fact]
    public void GetBirthDate_DecodesDaysSince1899()
    {
        // 37626 днів від 31.12.1899 = 06.01.2003
        Assert.Equal(new DateOnly(2003, 1, 6), TaxIdValidator.GetBirthDate("3762629459"));
    }

    [Theory]
    [InlineData("00032112")]
    [InlineData("14360570")]
    [InlineData("32855961")]
    public void IsValidCompanyCode_AcceptsKnownValidCodes(string code)
    {
        Assert.True(TaxIdValidator.IsValidCompanyCode(code));
    }

    [Theory]
    [InlineData("00032113")]
    [InlineData("1436057")]
    [InlineData("ABCDEFGH")]
    public void IsValidCompanyCode_RejectsWrongCodes(string code)
    {
        Assert.False(TaxIdValidator.IsValidCompanyCode(code));
    }
}
