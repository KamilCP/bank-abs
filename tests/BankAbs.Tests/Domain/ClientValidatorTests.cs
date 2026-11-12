using BankAbs.Domain.Entities;

namespace BankAbs.Tests.Domain;

public sealed class ClientValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 12);

    private static Client ValidPerson() => new()
    {
        ClientType = ClientTypes.Person,
        FullName = "Коваленко Андрій Петрович",
        TaxId = "3762629459",
        BirthDate = new DateOnly(2003, 1, 6),
        Phone = "+380671096465",
        Email = "andrii.kovalenko@example.com",
    };

    [Fact]
    public void ValidPerson_HasNoErrors()
    {
        Assert.Empty(ClientValidator.Validate(ValidPerson(), Today));
    }

    [Fact]
    public void BirthDateNotMatchingTaxId_IsRejected()
    {
        var client = ValidPerson();
        client.BirthDate = new DateOnly(2003, 1, 7);

        Assert.Contains("Дата народження не відповідає РНОКПП", ClientValidator.Validate(client, Today));
    }

    [Fact]
    public void WrongPhoneAndEmail_AreRejected()
    {
        var client = ValidPerson();
        client.Phone = "0671096465";
        client.Email = "not-an-email";

        var errors = ClientValidator.Validate(client, Today);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void LegalEntity_RequiresValidCompanyCode_NotBirthDate()
    {
        var company = new Client { ClientType = ClientTypes.Legal, FullName = "ТОВ «Тест»", TaxId = "00032112" };
        Assert.Empty(ClientValidator.Validate(company, Today));

        company.TaxId = "00032113";
        Assert.Single(ClientValidator.Validate(company, Today));
    }

    [Fact]
    public void Underage_IsRejected()
    {
        var client = ValidPerson();
        Assert.Contains(ClientValidator.Validate(client, new DateOnly(2010, 1, 1)), e => e.Contains("не молодшим", StringComparison.Ordinal));
    }
}
