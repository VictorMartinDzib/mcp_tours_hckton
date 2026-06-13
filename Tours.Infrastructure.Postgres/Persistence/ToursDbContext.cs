using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using Tours.Domain.Entities;

namespace Tours.Infrastructure.Postgres.Persistence;

public sealed class ToursDbContext(DbContextOptions<ToursDbContext> options) : DbContext(options)
{
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reviewConverter = new ValueConverter<List<Tours.Domain.ValueObjects.ActivityReview>, string>(
            x => JsonSerializer.Serialize(x, JsonSerializerOptions.Default),
            x => string.IsNullOrWhiteSpace(x)
                ? new List<Tours.Domain.ValueObjects.ActivityReview>()
                : JsonSerializer.Deserialize<List<Tours.Domain.ValueObjects.ActivityReview>>(x, JsonSerializerOptions.Default) ?? new List<Tours.Domain.ValueObjects.ActivityReview>());

        var photosConverter = new ValueConverter<List<string>, string>(
            x => JsonSerializer.Serialize(x, JsonSerializerOptions.Default),
            x => string.IsNullOrWhiteSpace(x)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(x, JsonSerializerOptions.Default) ?? new List<string>());

        var agesConverter = new ValueConverter<List<int>, string>(
            x => JsonSerializer.Serialize(x, JsonSerializerOptions.Default),
            x => string.IsNullOrWhiteSpace(x)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(x, JsonSerializerOptions.Default) ?? new List<int>());

        var preferencesConverter = new ValueConverter<List<Tours.Domain.Enums.ActivityCategory>, string>(
            x => JsonSerializer.Serialize(x, JsonSerializerOptions.Default),
            x => string.IsNullOrWhiteSpace(x)
                ? new List<Tours.Domain.Enums.ActivityCategory>()
                : JsonSerializer.Deserialize<List<Tours.Domain.Enums.ActivityCategory>>(x, JsonSerializerOptions.Default) ?? new List<Tours.Domain.Enums.ActivityCategory>());

        var itineraryItemsConverter = new ValueConverter<List<ItineraryItem>, string>(
            x => JsonSerializer.Serialize(x, JsonSerializerOptions.Default),
            x => string.IsNullOrWhiteSpace(x)
                ? new List<ItineraryItem>()
                : JsonSerializer.Deserialize<List<ItineraryItem>>(x, JsonSerializerOptions.Default) ?? new List<ItineraryItem>());

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Destination).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.Property(x => x.Reviews).HasConversion(reviewConverter).HasColumnType("jsonb");
            entity.Property(x => x.Photos).HasConversion(photosConverter).HasColumnType("jsonb");

            entity.OwnsOne(x => x.Requirement, req =>
            {
                req.Property(p => p.MinimumAge).HasColumnName("minimum_age");
                req.Property(p => p.Difficulty).HasColumnName("difficulty");
            });
        });

        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.ToTable("itineraries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Destination).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Budget).HasPrecision(12, 2);
            entity.Property(x => x.TotalPrice).HasPrecision(12, 2);
            entity.Property(x => x.Ages).HasConversion(agesConverter).HasColumnType("jsonb");
            entity.Property(x => x.Preferences).HasConversion(preferencesConverter).HasColumnType("jsonb");
            entity.Property(x => x.Items).HasConversion(itineraryItemsConverter).HasColumnType("jsonb");
        });
    }
}