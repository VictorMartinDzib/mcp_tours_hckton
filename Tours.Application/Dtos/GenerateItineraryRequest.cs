using Tours.Domain.Enums;

namespace Tours.Application.Dtos;

public sealed class GenerateItineraryRequest
{
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NumberOfPeople { get; set; }
    public List<int> Ages { get; set; } = [];
    public List<ActivityCategory> Preferences { get; set; } = [];
    public decimal Budget { get; set; }
}