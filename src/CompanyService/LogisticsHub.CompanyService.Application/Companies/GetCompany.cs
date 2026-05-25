using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed class GetCompany : IRequestHandler<GetCompanyQuery, Result<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;

    public GetCompany(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CompanyResult>> Handle(
        GetCompanyQuery query,
        CancellationToken cancellationToken)
    {
        var company = await _dbContext.GetCompanyByIdAsync(query.Id, cancellationToken);

        if (company is null)
        {
            return Result<CompanyResult>.Failure(CompanyErrors.NotFound(query.Id));
        }

        return Result<CompanyResult>.Success(CompanyResultFactory.ToResult(company));
    }
}
