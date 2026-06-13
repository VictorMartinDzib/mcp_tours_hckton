using Tours.Domain.Enums;
using Tours.Domain.ValueObjects;

namespace Tours.Domain.Entities;

public sealed class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public List<ActivityReview> Reviews { get; set; } = [];
    public List<string> Photos { get; set; } = [];
    public ActivityCategory Category { get; set; }
    public ActivityRequirement Requirement { get; set; } = new();
    public bool IncludesTransport { get; set; }
    public bool IncludesGuide { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsIndoorAlternative { get; set; }
}