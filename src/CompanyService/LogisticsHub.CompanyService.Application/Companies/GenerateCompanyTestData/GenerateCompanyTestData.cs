using LogisticsHub.CompanyService.Application.Persistence;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.GenerateCompanyTestData;

public sealed record GenerateCompanyTestDataCommand : IRequest<GenerateCompanyTestDataResult>;

public sealed record GenerateCompanyTestDataResult(
    int CompaniesCreated,
    int AddressesCreated);

public sealed class GenerateCompanyTestData
    : IRequestHandler<GenerateCompanyTestDataCommand, GenerateCompanyTestDataResult>
{
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

        var companies = CompanyTestDataGenerator.GenerateCompanies();
        var addressCount = companies.Sum(company => company.Addresses.Count);

        foreach (var company in companies)
        {
            await _dbContext.AddCompanyAsync(company, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GenerateCompanyTestDataResult(companies.Count, addressCount);
    }
}
