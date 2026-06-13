namespace Tours.Domain.ValueObjects;

public sealed class ActivityReview
{
    public string Author { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}