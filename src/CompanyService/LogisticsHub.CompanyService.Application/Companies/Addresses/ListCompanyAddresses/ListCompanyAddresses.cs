using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Addresses.ListCompanyAddresses;

public sealed class ListCompanyAddresses : IRequestHandler<ListCompanyAddressesQuery, Result<IReadOnlyList<CompanyAddressResult>>>
{
    private readonly ICompanyDbContext _dbContext;

    public ListCompanyAddresses(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CompanyAddressResult>>> Handle(
        ListCompanyAddressesQuery query,
        CancellationToken cancellationToken)
    {
        var companyExists = await _dbContext.CompanyExistsAsync(query.CompanyId, cancellationToken);
        if (!companyExists)
        {
            return Result<IReadOnlyList<CompanyAddressResult>>.Failure(
                CompanyErrors.AddressCompanyNotFound(query.CompanyId));
        }

        var addresses = await _dbContext.ListCompanyAddressesAsync(query.CompanyId, cancellationToken);
        var results = addresses
            .Select(CompanyResultFactory.ToResult)
            .ToArray();

        return Result<IReadOnlyList<CompanyAddressResult>>.Success(results);
    }
}
