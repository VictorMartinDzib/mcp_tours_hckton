using Tours.Domain.Enums;

namespace Tours.Domain.ValueObjects;

public sealed class ActivityRequirement
{
    public int MinimumAge { get; set; }
    public DifficultyLevel Difficulty { get; set; }
}