using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed class UpdateCompany : IRequestHandler<UpdateCompanyCommand, Result<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;
    private readonly ICompanyCache _companyCache;

    public UpdateCompany(
        ICompanyDbContext dbContext,
        ICompanyCache companyCache)
    {
        _dbContext = dbContext;
        _companyCache = companyCache;
    }

    public async Task<Result<CompanyResult>> Handle(
        UpdateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var company = await _dbContext.GetCompanyForUpdateAsync(command.Id, cancellationToken);
        if (company is null)
        {
            return Result<CompanyResult>.Failure(CompanyErrors.NotFound(command.Id));
        }

        if (command.ExternalCode is not null)
        {
            var existingCompany = await _dbContext.GetCompanyByExternalCodeAsync(command.ExternalCode, cancellationToken);
            if (existingCompany is not null && existingCompany.Id != command.Id)
            {
                return Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists(command.ExternalCode));
            }
        }

        company.Name = command.Name;
        company.ExternalCode = command.ExternalCode;
        company.Status = command.Status;
        company.UpdatedAtUtc = DateTime.UtcNow;

        var saveResult = await _dbContext.SaveChangesAsyncHandlingDuplicateExternalCodeAsync(cancellationToken);

        if (saveResult == CompanySaveChangesResult.DuplicateExternalCode && command.ExternalCode is not null)
        {
            return Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists(command.ExternalCode));
        }

        await _companyCache.InvalidateAsync(command.Id, CancellationToken.None);

        return Result<CompanyResult>.Success(CompanyResultFactory.ToResult(company));
    }
}
