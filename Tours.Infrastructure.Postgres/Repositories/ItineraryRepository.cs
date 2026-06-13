using Microsoft.EntityFrameworkCore;
using Tours.Domain.Entities;
using Tours.Domain.Interfaces;
using Tours.Infrastructure.Postgres.Persistence;

namespace Tours.Infrastructure.Postgres.Repositories;

public sealed class ItineraryRepository(ToursDbContext dbContext) : IItineraryRepository
{
    public Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Itineraries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken)
    {
        await dbContext.Itineraries.AddAsync(itinerary, cancellationToken);
    }

    public Task UpdateAsync(Itinerary itinerary, CancellationToken cancellationToken)
    {
        dbContext.Itineraries.Update(itinerary);
        return Task.CompletedTask;
    }
}