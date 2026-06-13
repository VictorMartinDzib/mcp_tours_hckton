namespace Tours.Application.Dtos;

public sealed class AddItineraryActivityRequest
{
    public Guid ActivityId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}