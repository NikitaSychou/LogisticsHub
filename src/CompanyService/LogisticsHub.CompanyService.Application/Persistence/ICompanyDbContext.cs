namespace LogisticsHub.CompanyService.Application.Persistence;

public interface ICompanyDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
