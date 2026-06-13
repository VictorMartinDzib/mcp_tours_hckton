using Tours.Domain.Entities;

namespace Tours.Domain.Interfaces;

public interface IItineraryRepository
{
    Task<Itinerary?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken);
    Task UpdateAsync(Itinerary itinerary, CancellationToken cancellationToken);
}