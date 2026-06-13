namespace Tours.Application.Dtos;

public sealed class ReplaceItineraryActivityRequest
{
    public Guid ItemId { get; set; }
    public Guid NewActivityId { get; set; }
}