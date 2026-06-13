using Tours.Domain.Entities;
using Tours.Domain.Enums;
using Tours.Domain.ValueObjects;

namespace Tours.Infrastructure.Postgres.Seed;

public static class ActivitySeedFactory
{
    public static IReadOnlyCollection<Activity> Build()
    {
        var destinations = new[] { "Cancun", "Riviera Maya", "CDMX", "Los Cabos", "Oaxaca" };
        var categories = Enum.GetValues<ActivityCategory>();
        var difficulties = Enum.GetValues<DifficultyLevel>();
        var random = new Random(777);

        var activities = new List<Activity>(120);
        for (var i = 0; i < 120; i++)
        {
            var category = categories[i % categories.Length];
            var destination = destinations[i % destinations.Length];
            var difficulty = difficulties[i % difficulties.Length];
            var minAge = difficulty switch
            {
                DifficultyLevel.Easy => 5,
                DifficultyLevel.Moderate => 10,
                DifficultyLevel.Challenging => 14,
                _ => 18
            };

            var indoor = category is ActivityCategory.Cultural or ActivityCategory.Gastronomic or ActivityCategory.Wellness;
            var basePrice = category switch
            {
                ActivityCategory.Adventure => 80,
                ActivityCategory.Cultural => 45,
                ActivityCategory.Gastronomic => 55,
                ActivityCategory.Nature => 60,
                ActivityCategory.Relax => 70,
                ActivityCategory.Family => 40,
                ActivityCategory.Nightlife => 65,
                _ => 75
            };

            activities.Add(new Activity
            {
                Name = $"{category} Experience #{i + 1}",
                Description = $"Actividad {category} en {destination} con enfoque premium para viajeros.",
                Price = basePrice + random.Next(5, 120),
                DurationMinutes = random.Next(60, 420),
                Location = $"Zona {random.Next(1, 20)}, {destination}",
                Destination = destination,
                IsAvailable = random.NextDouble() > 0.07,
                Reviews =
                [
                    new ActivityReview { Author = "Ana", Rating = random.Next(3, 6), Comment = "Muy bien organizada." },
                    new ActivityReview { Author = "Luis", Rating = random.Next(3, 6), Comment = "Excelente relación calidad-precio." }
                ],
                Photos =
                [
                    $"https://picsum.photos/seed/tour-{i + 1}/1024/768",
                    $"https://picsum.photos/seed/tour-{i + 1000}/1024/768"
                ],
                Category = category,
                Requirement = new ActivityRequirement
                {
                    MinimumAge = minAge,
                    Difficulty = difficulty
                },
                IncludesTransport = random.NextDouble() > 0.35,
                IncludesGuide = random.NextDouble() > 0.25,
                ProviderName = $"Operador {destination} {((i % 12) + 1):D2}",
                IsIndoorAlternative = indoor
            });
        }

        return activities;
    }
}