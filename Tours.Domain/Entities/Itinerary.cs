using Tours.Domain.Enums;

namespace Tours.Domain.Entities;

public sealed class Itinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NumberOfPeople { get; set; }
    public List<int> Ages { get; set; } = [];
    public List<ActivityCategory> Preferences { get; set; } = [];
    public decimal Budget { get; set; }
    public decimal TotalPrice { get; set; }
    public List<ItineraryItem> Items { get; set; } = [];

    public void RecalculateTotalPrice(IEnumerable<Activity> selectedActivities)
    {
        var activityMap = selectedActivities.ToDictionary(x => x.Id, x => x.Price);
        TotalPrice = Items
            .Where(x => activityMap.ContainsKey(x.ActivityId))
            .Sum(x => activityMap[x.ActivityId] * NumberOfPeople);
    }
}