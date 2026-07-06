using LogisticsHub.CompanyService.Application.Persistence;
using MediatR;

namespace LogisticsHub.CompanyService.Application.TestData.GenerateCompanyTestData;

public sealed record GenerateCompanyTestDataCommand : IRequest<GenerateCompanyTestDataResult>;

public sealed record GenerateCompanyTestDataResult(
    int CompaniesCreated,
    int AddressesCreated);

public sealed class GenerateCompanyTestData
    : IRequestHandler<GenerateCompanyTestDataCommand, GenerateCompanyTestDataResult>
{
    private const int CompanyBatchSize = 100;

    private readonly ICompanyDbContext _dbContext;

    public GenerateCompanyTestData(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GenerateCompanyTestDataResult> Handle(
        GenerateCompanyTestDataCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var companiesCreated = 0;
        var addressesCreated = 0;

        while (companiesCreated < CompanyTestDataGenerator.CompanyCount)
        {
            var batchSize = Math.Min(
                CompanyBatchSize,
                CompanyTestDataGenerator.CompanyCount - companiesCreated);
            var companies = CompanyTestDataGenerator.GenerateCompanies(batchSize);
            var addressCount = companies.Sum(company => company.Addresses.Count);

            foreach (var company in companies)
            {
                await _dbContext.AddCompanyAsync(company, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            companiesCreated += companies.Count;
            addressesCreated += addressCount;

            _dbContext.ClearChangeTracker();
        }

        return new GenerateCompanyTestDataResult(companiesCreated, addressesCreated);
    }
}
