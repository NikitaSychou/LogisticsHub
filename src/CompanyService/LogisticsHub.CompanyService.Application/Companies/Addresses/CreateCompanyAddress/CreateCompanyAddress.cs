using LogisticsHub.CompanyService.Application.Companies;
using LogisticsHub.CompanyService.Application.Companies.Results;
using LogisticsHub.CompanyService.Application.Caching;
using LogisticsHub.CompanyService.Application.Persistence;
using LogisticsHub.CompanyService.Domain.Entities;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.CompanyService.Application.Companies.Addresses.CreateCompanyAddress;

public sealed class CreateCompanyAddress : IRequestHandler<CreateCompanyAddressCommand, Result<CompanyAddressResult>>
{
    private readonly ICompanyDbContext _dbContext;
    private readonly ICompanyAddressCache _addressCache;

    public CreateCompanyAddress(
        ICompanyDbContext dbContext,
        ICompanyAddressCache addressCache)
    {
        _dbContext = dbContext;
        _addressCache = addressCache;
    }

    public async Task<Result<CompanyAddressResult>> Handle(
        CreateCompanyAddressCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var companyExists = await _dbContext.CompanyExistsAsync(command.CompanyId, cancellationToken);
        if (!companyExists)
        {
            return Result<CompanyAddressResult>.Failure(CompanyErrors.AddressCompanyNotFound(command.CompanyId));
        }

        var address = new CompanyAddress
        {
            Id = Guid.NewGuid(),
            CompanyId = command.CompanyId,
            AddressType = command.AddressType,
            CountryCode = command.CountryCode,
            City = command.City,
            PostalCode = command.PostalCode,
            Line1 = command.Line1,
            Line2 = command.Line2,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.AddCompanyAddressAsync(address, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _addressCache.InvalidateAsync(command.CompanyId, address.Id, CancellationToken.None);

        return Result<CompanyAddressResult>.Success(CompanyResultFactory.ToResult(address));
    }
}
