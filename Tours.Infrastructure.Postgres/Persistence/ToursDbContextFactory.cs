using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tours.Infrastructure.Postgres.Persistence;

public sealed class ToursDbContextFactory : IDesignTimeDbContextFactory<ToursDbContext>
{
    public ToursDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ToursDbContext>();
        builder.UseNpgsql("Host=localhost;Port=5432;Database=tours;Username=postgres;Password=postgres");
        return new ToursDbContext(builder.Options);
    }
}