using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class EfUnitOfWork(ToursDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}