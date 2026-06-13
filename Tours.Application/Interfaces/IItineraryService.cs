using Tours.Application.Dtos;

namespace Tours.Application.Interfaces;

public interface IItineraryService
{
    Task<ItineraryResponse> GenerateAsync(GenerateItineraryRequest request, CancellationToken cancellationToken);
    Task<ItineraryResponse?> GetByIdAsync(Guid itineraryId, CancellationToken cancellationToken);
    Task<ItineraryResponse?> ReplaceActivityAsync(Guid itineraryId, ReplaceItineraryActivityRequest request, CancellationToken cancellationToken);
    Task<ItineraryResponse?> AddActivityAsync(Guid itineraryId, AddItineraryActivityRequest request, CancellationToken cancellationToken);
}