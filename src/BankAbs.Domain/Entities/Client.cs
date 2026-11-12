namespace BankAbs.Domain.Entities;

/// <summary>Клієнт банку (фізична або юридична особа).</summary>
public sealed class Client
{
    /// <summary>Ідентифікатор.</summary>
    public int Id { get; set; }

    /// <summary>Тип клієнта (<see cref="ClientTypes"/>).</summary>
    public string ClientType { get; set; } = ClientTypes.Person;

    /// <summary>ПІБ або повне найменування.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>РНОКПП (10 цифр) або код ЄДРПОУ (8 цифр).</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Документ (паспорт / ID-картка / номер запису в ЄДР).</summary>
    public string Document { get; set; } = string.Empty;

    /// <summary>Дата народження (для фізичної особи).</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>Телефон.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Електронна пошта.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Адреса.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Стан (<see cref="ClientStatuses"/>).</summary>
    public string Status { get; set; } = ClientStatuses.Active;

    /// <summary>Дата реєстрації.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Кількість відкритих рахунків (заповнюється у списку).</summary>
    public int OpenAccounts { get; set; }

    /// <summary>Сума коштів на поточних і депозитних рахунках у базовій валюті (заповнюється у списку).</summary>
    public decimal TotalBalanceBase { get; set; }

    /// <summary>Копія об'єкта для редагування у формі.</summary>
    public Client Clone() => (Client)MemberwiseClone();
}
