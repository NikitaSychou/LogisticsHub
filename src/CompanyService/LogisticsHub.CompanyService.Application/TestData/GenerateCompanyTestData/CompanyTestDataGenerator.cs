using Bogus;
using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.CompanyService.Domain.Enums;

namespace LogisticsHub.CompanyService.Application.TestData.GenerateCompanyTestData;

public static class CompanyTestDataGenerator
{
    public const int CompanyCount = 1000;

    private const int MinAddressCount = 3;
    private const int MaxAddressCount = 5;

    private static readonly CompanyAddressType[] AddressTypes =
        Enum.GetValues<CompanyAddressType>();

    private static readonly string[] CountryCodes =
    [
        "US",
        "GB",
        "DE",
        "FR",
        "NL",
        "PL",
        "CA",
        "AU"
    ];

    public static IReadOnlyList<Company> GenerateCompanies()
    {
        return GenerateCompanies(CompanyCount);
    }

    public static IReadOnlyList<Company> GenerateCompanies(int count)
    {
        var faker = new Faker();
        var companies = new List<Company>(count);

        for (var index = 0; index < count; index++)
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = LimitRequired(faker.Company.CompanyName(), 200, "Company"),
                ExternalCode = $"TEST-{Guid.NewGuid():N}",
                Status = CompanyStatus.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            var addressCount = faker.Random.Int(MinAddressCount, MaxAddressCount);
            for (var addressIndex = 0; addressIndex < addressCount; addressIndex++)
            {
                company.Addresses.Add(CreateAddress(faker, company));
            }

            companies.Add(company);
        }

        return companies;
    }

    private static CompanyAddress CreateAddress(Faker faker, Company company)
    {
        return new CompanyAddress
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            AddressType = faker.PickRandom(AddressTypes),
            CountryCode = faker.PickRandom(CountryCodes),
            City = LimitRequired(faker.Address.City(), 100, "City"),
            PostalCode = LimitOptional(faker.Address.ZipCode(), 32),
            Line1 = LimitRequired(faker.Address.StreetAddress(), 200, "Street address"),
            Line2 = faker.Random.Bool(0.35f)
                ? LimitOptional(faker.Address.SecondaryAddress(), 200)
                : null,
            CreatedAtUtc = DateTime.UtcNow,
            Company = company
        };
    }

    private static string LimitRequired(string? value, int maxLength, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }

    private static string? LimitOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
