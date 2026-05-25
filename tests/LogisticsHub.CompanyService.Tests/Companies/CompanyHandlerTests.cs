using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.CompanyService.Domain.Enums;
using LogisticsHub.CompanyService.Tests.Fakes;
using Xunit;

namespace LogisticsHub.CompanyService.Tests.Companies;

public sealed class CompanyHandlerTests
{
    [Fact]
    public async Task CreateCompany_WhenValid_CreatesCompany()
    {
        var dbContext = new FakeCompanyDbContext();
        var handler = new CreateCompany(dbContext);

        var result = await handler.Handle(
            new CreateCompanyCommand("Acme", "ACME", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var company = Assert.Single(dbContext.Companies);
        Assert.Equal("Acme", company.Name);
        Assert.Equal("ACME", company.ExternalCode);
        Assert.Equal(CompanyStatus.Active, company.Status);
        Assert.Equal(company.Id, result.Value.Id);
    }

    [Fact]
    public async Task CreateCompany_WhenExternalCodeExists_ReturnsDuplicateExternalCodeError()
    {
        var dbContext = new FakeCompanyDbContext();
        dbContext.Companies.Add(CreateCompany("Existing", "ACME"));
        var handler = new CreateCompany(dbContext);

        var result = await handler.Handle(
            new CreateCompanyCommand("Acme", "ACME", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.external_code_already_exists", result.Error.Code);
        Assert.Single(dbContext.Companies);
    }

    [Fact]
    public async Task CreateCompany_WhenSaveDetectsDuplicateExternalCode_ReturnsDuplicateExternalCodeError()
    {
        var dbContext = new FakeCompanyDbContext
        {
            SaveChangesResult = CompanySaveChangesResult.DuplicateExternalCode
        };
        var handler = new CreateCompany(dbContext);

        var result = await handler.Handle(
            new CreateCompanyCommand("Acme", "ACME", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.external_code_already_exists", result.Error.Code);
    }

    [Fact]
    public async Task GetCompany_WhenCompanyExists_ReturnsCompany()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var handler = new GetCompany(dbContext);

        var result = await handler.Handle(new GetCompanyQuery(company.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(company.Id, result.Value.Id);
        Assert.Equal(company.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetCompany_WhenCompanyDoesNotExist_ReturnsNotFoundError()
    {
        var handler = new GetCompany(new FakeCompanyDbContext());

        var result = await handler.Handle(new GetCompanyQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.not_found", result.Error.Code);
    }

    [Fact]
    public async Task ListCompanies_ReturnsCompanies()
    {
        var dbContext = new FakeCompanyDbContext();
        dbContext.Companies.Add(CreateCompany("Acme", "ACME"));
        dbContext.Companies.Add(CreateCompany("Beta", "BETA"));
        var handler = new ListCompanies(dbContext);

        var result = await handler.Handle(new ListCompaniesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, company => company.ExternalCode == "ACME");
        Assert.Contains(result, company => company.ExternalCode == "BETA");
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyExists_UpdatesCompany()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var handler = new UpdateCompany(dbContext);

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "Updated", "UPDATED", CompanyStatus.Inactive),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", company.Name);
        Assert.Equal("UPDATED", company.ExternalCode);
        Assert.Equal(CompanyStatus.Inactive, company.Status);
        Assert.NotNull(company.UpdatedAtUtc);
        Assert.Equal("Updated", result.Value.Name);
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyDoesNotExist_ReturnsNotFoundError()
    {
        var handler = new UpdateCompany(new FakeCompanyDbContext());

        var result = await handler.Handle(
            new UpdateCompanyCommand(Guid.NewGuid(), "Updated", "UPDATED", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.not_found", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCompany_WhenExternalCodeBelongsToAnotherCompany_ReturnsDuplicateExternalCodeError()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        var otherCompany = CreateCompany("Beta", "BETA");
        dbContext.Companies.Add(company);
        dbContext.Companies.Add(otherCompany);
        var handler = new UpdateCompany(dbContext);

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "Acme", "BETA", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.external_code_already_exists", result.Error.Code);
    }

    [Fact]
    public async Task CreateCompanyAddress_WhenCompanyExists_CreatesAddress()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var handler = new CreateCompanyAddress(dbContext);

        var result = await handler.Handle(
            new CreateCompanyAddressCommand(
                company.Id,
                CompanyAddressType.Shipping,
                "US",
                "New York",
                "10001",
                "1 Logistics Way",
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var address = Assert.Single(dbContext.CompanyAddresses);
        Assert.Equal(company.Id, address.CompanyId);
        Assert.Equal(CompanyAddressType.Shipping, address.AddressType);
        Assert.Equal("US", result.Value.CountryCode);
    }

    [Fact]
    public async Task CreateCompanyAddress_WhenCompanyDoesNotExist_ReturnsCompanyNotFoundError()
    {
        var handler = new CreateCompanyAddress(new FakeCompanyDbContext());
        var companyId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateCompanyAddressCommand(
                companyId,
                CompanyAddressType.Shipping,
                "US",
                "New York",
                null,
                "1 Logistics Way",
                null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.address.company_not_found", result.Error.Code);
    }

    [Fact]
    public async Task ListCompanyAddresses_WhenCompanyExists_ReturnsAddresses()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        dbContext.CompanyAddresses.Add(CreateAddress(company.Id, CompanyAddressType.Legal));
        dbContext.CompanyAddresses.Add(CreateAddress(company.Id, CompanyAddressType.Shipping));
        var handler = new ListCompanyAddresses(dbContext);

        var result = await handler.Handle(new ListCompanyAddressesQuery(company.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, address => Assert.Equal(company.Id, address.CompanyId));
    }

    [Fact]
    public async Task ListCompanyAddresses_WhenCompanyDoesNotExist_ReturnsCompanyNotFoundError()
    {
        var handler = new ListCompanyAddresses(new FakeCompanyDbContext());

        var result = await handler.Handle(new ListCompanyAddressesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.address.company_not_found", result.Error.Code);
    }

    private static Company CreateCompany(string name, string? externalCode)
    {
        return new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            ExternalCode = externalCode,
            Status = CompanyStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static CompanyAddress CreateAddress(Guid companyId, CompanyAddressType addressType)
    {
        return new CompanyAddress
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AddressType = addressType,
            CountryCode = "US",
            City = "New York",
            Line1 = "1 Logistics Way",
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
