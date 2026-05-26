using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed class GetCompanyAddress : IRequestHandler<GetCompanyAddressQuery, Result<CompanyAddressResult>>
{
    private readonly ICompanyDbContext _dbContext;

    public GetCompanyAddress(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CompanyAddressResult>> Handle(
        GetCompanyAddressQuery query,
        CancellationToken cancellationToken)
    {
        var address = await _dbContext.GetCompanyAddressAsync(
            query.CompanyId,
            query.AddressId,
            cancellationToken);

        if (address is null)
        {
            return Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(query.CompanyId));
        }

        return Result<CompanyAddressResult>.Success(CompanyResultFactory.ToResult(address));
    }
}
