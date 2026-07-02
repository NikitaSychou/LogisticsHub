using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.GenerateCompanyTestData;
using LogisticsHub.CompanyService.Application.Companies.GetCompanyAddress;
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
        var cache = new FakeCompanyCache();
        var handler = new GetCompany(dbContext, cache);

        var result = await handler.Handle(new GetCompanyQuery(company.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(company.Id, result.Value.Id);
        Assert.Equal(company.Name, result.Value.Name);
        Assert.Equal(1, cache.GetOrCreateCallCount);
        Assert.Equal(1, dbContext.GetCompanyByIdCallCount);
    }

    [Fact]
    public async Task GetCompany_WhenCompanyIsCached_ReturnsCachedCompanyWithoutReadingDatabase()
    {
        var company = new CompanyResult(
            Guid.NewGuid(),
            "Cached",
            "CACHED",
            CompanyStatus.Active,
            DateTime.UtcNow,
            null);
        var dbContext = new FakeCompanyDbContext();
        var cache = new FakeCompanyCache();
        cache.Add(company);
        var handler = new GetCompany(dbContext, cache);

        var result = await handler.Handle(new GetCompanyQuery(company.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(company.Id, result.Value.Id);
        Assert.Equal("Cached", result.Value.Name);
        Assert.Equal(1, cache.GetOrCreateCallCount);
        Assert.Equal(0, dbContext.GetCompanyByIdCallCount);
    }

    [Fact]
    public async Task GetCompany_WhenCompanyDoesNotExist_ReturnsNotFoundError()
    {
        var dbContext = new FakeCompanyDbContext();
        var handler = new GetCompany(dbContext, new FakeCompanyCache());

        var result = await handler.Handle(new GetCompanyQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.not_found", result.Error.Code);
        Assert.Equal(1, dbContext.GetCompanyByIdCallCount);
    }

    [Fact]
    public async Task GetCompany_ForwardsCancellationTokenToDatabaseSource()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        using var cancellationTokenSource = new CancellationTokenSource();
        var handler = new GetCompany(dbContext, new FakeCompanyCache());

        var result = await handler.Handle(
            new GetCompanyQuery(company.Id),
            cancellationTokenSource.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(cancellationTokenSource.Token, dbContext.LastGetCompanyByIdCancellationToken);
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
        var cache = new FakeCompanyCache();
        var handler = new UpdateCompany(dbContext, cache);

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "Updated", "UPDATED", CompanyStatus.Inactive),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", company.Name);
        Assert.Equal("UPDATED", company.ExternalCode);
        Assert.Equal(CompanyStatus.Inactive, company.Status);
        Assert.NotNull(company.UpdatedAtUtc);
        Assert.Equal("Updated", result.Value.Name);
        Assert.Equal(1, cache.InvalidateCallCount);
        Assert.Equal(company.Id, cache.LastInvalidatedCompanyId);
        Assert.Equal(CancellationToken.None, cache.LastInvalidateCancellationToken);
    }

    [Fact]
    public async Task UpdateCompany_WhenCompanyDoesNotExist_ReturnsNotFoundError()
    {
        var cache = new FakeCompanyCache();
        var handler = new UpdateCompany(new FakeCompanyDbContext(), cache);

        var result = await handler.Handle(
            new UpdateCompanyCommand(Guid.NewGuid(), "Updated", "UPDATED", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.not_found", result.Error.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UpdateCompany_WhenExternalCodeBelongsToAnotherCompany_ReturnsDuplicateExternalCodeError()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        var otherCompany = CreateCompany("Beta", "BETA");
        dbContext.Companies.Add(company);
        dbContext.Companies.Add(otherCompany);
        var cache = new FakeCompanyCache();
        var handler = new UpdateCompany(dbContext, cache);

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "Acme", "BETA", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.external_code_already_exists", result.Error.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UpdateCompany_WhenSaveDetectsDuplicateExternalCode_DoesNotInvalidateCompanyCache()
    {
        var dbContext = new FakeCompanyDbContext
        {
            SaveChangesResult = CompanySaveChangesResult.DuplicateExternalCode
        };
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var cache = new FakeCompanyCache();
        var handler = new UpdateCompany(dbContext, cache);

        var result = await handler.Handle(
            new UpdateCompanyCommand(company.Id, "Acme", "BETA", CompanyStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.external_code_already_exists", result.Error.Code);
        Assert.Equal(0, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task UpdateCompany_WhenSaveFails_DoesNotInvalidateCompanyCache()
    {
        var dbContext = new FakeCompanyDbContext
        {
            ThrowOnSaveChanges = true
        };
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var cache = new FakeCompanyCache();
        var handler = new UpdateCompany(dbContext, cache);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new UpdateCompanyCommand(company.Id, "Updated", "UPDATED", CompanyStatus.Active),
                CancellationToken.None));

        Assert.Equal(0, cache.InvalidateCallCount);
    }

    [Fact]
    public async Task GenerateCompanyTestData_CreatesOneThousandCompaniesInBatches()
    {
        var dbContext = new FakeCompanyDbContext();
        var handler = new GenerateCompanyTestData(dbContext);

        var result = await handler.Handle(new GenerateCompanyTestDataCommand(), CancellationToken.None);

        Assert.Equal(1000, CompanyTestDataGenerator.CompanyCount);
        Assert.Equal(1000, result.CompaniesCreated);
        Assert.Equal(1000, dbContext.Companies.Count);
        Assert.Equal(dbContext.SavedAddressBatchCounts.Sum(), result.AddressesCreated);
        Assert.Equal(10, dbContext.SaveChangesCallCount);
        Assert.Equal(10, dbContext.ClearChangeTrackerCallCount);
        Assert.All(dbContext.CompanyBatchCountsAtSave, count => Assert.InRange(count, 1, 100));
        Assert.All(dbContext.CompanyBatchCountsAtSave, count => Assert.Equal(100, count));
        Assert.All(dbContext.TrackedEntityCountsAtSave, count => Assert.InRange(count, 400, 600));
        Assert.Equal(0, dbContext.CurrentTrackedCompanyCount);
        Assert.Equal(0, dbContext.CurrentTrackedAddressCount);
    }

    [Fact]
    public async Task GenerateCompanyTestData_ForwardsCancellationTokenToEverySave()
    {
        var dbContext = new FakeCompanyDbContext();
        using var cancellationTokenSource = new CancellationTokenSource();
        var handler = new GenerateCompanyTestData(dbContext);

        var result = await handler.Handle(
            new GenerateCompanyTestDataCommand(),
            cancellationTokenSource.Token);

        Assert.Equal(1000, result.CompaniesCreated);
        Assert.Equal(cancellationTokenSource.Token, dbContext.LastSaveChangesCancellationToken);
    }

    [Fact]
    public async Task GenerateCompanyTestData_WhenBatchSaveFails_StopsWithoutClearingFailedBatch()
    {
        var dbContext = new FakeCompanyDbContext
        {
            FailOnSaveChangesCall = 3
        };
        var handler = new GenerateCompanyTestData(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GenerateCompanyTestDataCommand(), CancellationToken.None));

        Assert.Equal(3, dbContext.SaveChangesCallCount);
        Assert.Equal(2, dbContext.ClearChangeTrackerCallCount);
        Assert.Equal(300, dbContext.Companies.Count);
        Assert.Equal(100, dbContext.CurrentTrackedCompanyCount);
        Assert.All(dbContext.CompanyBatchCountsAtSave, count => Assert.Equal(100, count));
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

    [Fact]
    public async Task GetCompanyAddress_WhenAddressBelongsToCompany_ReturnsAddress()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        var address = CreateAddress(company.Id, CompanyAddressType.Shipping);
        dbContext.Companies.Add(company);
        dbContext.CompanyAddresses.Add(address);
        var cache = new FakeCompanyAddressCache();
        var handler = new GetCompanyAddress(dbContext, cache);

        var result = await handler.Handle(
            new GetCompanyAddressQuery(company.Id, address.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(address.Id, result.Value.Id);
        Assert.Equal(company.Id, result.Value.CompanyId);
        Assert.Equal(CompanyAddressType.Shipping, result.Value.AddressType);
        Assert.Equal(1, cache.GetOrCreateCallCount);
    }

    [Fact]
    public async Task GetCompanyAddress_WhenAddressIsCached_ReturnsCachedAddress()
    {
        var companyId = Guid.NewGuid();
        var address = new CompanyAddressResult(
            Guid.NewGuid(),
            companyId,
            CompanyAddressType.Shipping,
            "US",
            "New York",
            null,
            "1 Logistics Way",
            null,
            DateTime.UtcNow,
            null);
        var cache = new FakeCompanyAddressCache();
        cache.Add(address);
        var handler = new GetCompanyAddress(new FakeCompanyDbContext(), cache);

        var result = await handler.Handle(
            new GetCompanyAddressQuery(companyId, address.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(address.Id, result.Value.Id);
        Assert.Equal(companyId, result.Value.CompanyId);
        Assert.Equal(1, cache.GetOrCreateCallCount);
    }

    [Fact]
    public async Task GetCompanyAddress_WhenCacheFails_ReturnsAddressFromDatabase()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        var address = CreateAddress(company.Id, CompanyAddressType.Shipping);
        dbContext.Companies.Add(company);
        dbContext.CompanyAddresses.Add(address);
        var cache = new FakeCompanyAddressCache
        {
            FailOnGetOrCreate = true
        };
        var handler = new GetCompanyAddress(dbContext, cache);

        var result = await handler.Handle(
            new GetCompanyAddressQuery(company.Id, address.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(address.Id, result.Value.Id);
        Assert.Equal(1, cache.GetOrCreateCallCount);
    }

    [Fact]
    public async Task GetCompanyAddress_WhenAddressDoesNotExist_ReturnsCompanyNotFoundError()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        dbContext.Companies.Add(company);
        var handler = new GetCompanyAddress(dbContext, new FakeCompanyAddressCache());

        var result = await handler.Handle(
            new GetCompanyAddressQuery(company.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("company.address.company_not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetCompanyAddress_WhenAddressBelongsToAnotherCompany_ReturnsCompanyNotFoundError()
    {
        var dbContext = new FakeCompanyDbContext();
        var company = CreateCompany("Acme", "ACME");
        var otherCompany = CreateCompany("Beta", "BETA");
        var address = CreateAddress(otherCompany.Id, CompanyAddressType.Billing);
        dbContext.Companies.Add(company);
        dbContext.Companies.Add(otherCompany);
        dbContext.CompanyAddresses.Add(address);
        var handler = new GetCompanyAddress(dbContext, new FakeCompanyAddressCache());

        var result = await handler.Handle(
            new GetCompanyAddressQuery(company.Id, address.Id),
            CancellationToken.None);

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
