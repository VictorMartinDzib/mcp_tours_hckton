using Tours.Domain.Interfaces;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => Task.FromResult(0);
}
