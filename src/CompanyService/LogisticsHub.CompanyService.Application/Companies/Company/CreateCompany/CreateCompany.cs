using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies;

public sealed class CreateCompany : IRequestHandler<CreateCompanyCommand, Result<CompanyResult>>
{
    private readonly ICompanyDbContext _dbContext;

    public CreateCompany(ICompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CompanyResult>> Handle(
        CreateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExternalCode is not null)
        {
            var existingCompany = await _dbContext.GetCompanyByExternalCodeAsync(command.ExternalCode, cancellationToken);
            if (existingCompany is not null)
            {
                return Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists(command.ExternalCode));
            }
        }

        var now = DateTime.UtcNow;
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            ExternalCode = command.ExternalCode,
            Status = command.Status,
            CreatedAtUtc = now
        };

        await _dbContext.AddCompanyAsync(company, cancellationToken);
        var saveResult = await _dbContext.SaveChangesAsyncHandlingDuplicateExternalCodeAsync(cancellationToken);

        if (saveResult == CompanySaveChangesResult.DuplicateExternalCode && command.ExternalCode is not null)
        {
            return Result<CompanyResult>.Failure(CompanyErrors.ExternalCodeAlreadyExists(command.ExternalCode));
        }

        return Result<CompanyResult>.Success(CompanyResultFactory.ToResult(company));
    }
}
