using Tours.Application.Dtos;
using Tours.Application.Interfaces;
using Tours.Domain.Entities;
using Tours.Domain.Enums;
using Tours.Domain.Interfaces;

namespace Tours.Application.Services;

public sealed class ItineraryService(
    IActivityRepository activityRepository,
    IItineraryRepository itineraryRepository,
    IWeatherService weatherService,
    IUnitOfWork unitOfWork) : IItineraryService
{
    public async Task<ItineraryResponse> GenerateAsync(GenerateItineraryRequest request, CancellationToken cancellationToken)
    {
        var minAge = request.Ages.Count == 0 ? 0 : request.Ages.Min();
        var candidates = await activityRepository.SearchAsync(
            request.Destination,
            request.Preferences,
            maxPrice: null,
            minAge,
            onlyAvailable: true,
            cancellationToken);

        var planDays = Enumerable.Range(0, request.EndDate.DayNumber - request.StartDate.DayNumber + 1)
            .Select(offset => request.StartDate.AddDays(offset))
            .ToArray();

        var itinerary = new Itinerary
        {
            Destination = request.Destination,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NumberOfPeople = request.NumberOfPeople,
            Ages = request.Ages,
            Preferences = request.Preferences,
            Budget = request.Budget,
            Items = []
        };

        var selected = new List<Activity>();
        decimal runningCost = 0m;

        foreach (var day in planDays)
        {
            var dayChoice = PickBestActivity(candidates, selected, request.Preferences, request.Budget, request.NumberOfPeople, runningCost);
            if (dayChoice is null)
            {
                break;
            }

            selected.Add(dayChoice);
            runningCost += dayChoice.Price * request.NumberOfPeople;
            itinerary.Items.Add(new ItineraryItem
            {
                ActivityId = dayChoice.Id,
                ScheduledDate = day,
                Notes = "Plan principal"
            });
        }

        // For 14-16 day ranges we enrich the decision with the requested hybrid signal.
        var dayCount = planDays.Length;
        var hybridScore = dayCount is >= 14 and <= 16
            ? await weatherService.GetHybridRiskScoreAsync(request.Destination, request.StartDate, request.EndDate, cancellationToken)
            : 0d;

        foreach (var item in itinerary.Items)
        {
            var activity = selected.First(x => x.Id == item.ActivityId);
            var assessment = await weatherService.AssessActivityWeatherAsync(activity.Location, item.ScheduledDate, cancellationToken);

            var riskDetected = !assessment.IsSuitable || hybridScore > 60;
            item.ApplyWeather(assessment);
            item.WeatherRiskDetected = riskDetected;

            if (riskDetected)
            {
                var alternative = candidates
                    .Where(x => x.IsIndoorAlternative)
                    .Where(x => x.Id != activity.Id)
                    .OrderBy(x => x.Price)
                    .FirstOrDefault();

                item.SuggestedAlternativeActivityId = alternative?.Id;
            }
        }

        itinerary.RecalculateTotalPrice(selected);
        await itineraryRepository.AddAsync(itinerary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(itinerary);
    }

    public async Task<ItineraryResponse?> GetByIdAsync(Guid itineraryId, CancellationToken cancellationToken)
    {
        var itinerary = await itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
        return itinerary is null ? null : ToResponse(itinerary);
    }

    public async Task<ItineraryResponse?> ReplaceActivityAsync(
        Guid itineraryId,
        ReplaceItineraryActivityRequest request,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary is null)
        {
            return null;
        }

        var newActivity = await activityRepository.GetByIdAsync(request.NewActivityId, cancellationToken);
        if (newActivity is null)
        {
            return null;
        }

        var item = itinerary.Items.FirstOrDefault(x => x.Id == request.ItemId);
        if (item is null)
        {
            return null;
        }

        item.ActivityId = newActivity.Id;
        var weather = await weatherService.AssessActivityWeatherAsync(newActivity.Location, item.ScheduledDate, cancellationToken);
        item.ApplyWeather(weather);

        var referencedActivities = await ResolveActivitiesAsync(itinerary.Items, cancellationToken);
        itinerary.RecalculateTotalPrice(referencedActivities);

        await itineraryRepository.UpdateAsync(itinerary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(itinerary);
    }

    public async Task<ItineraryResponse?> AddActivityAsync(
        Guid itineraryId,
        AddItineraryActivityRequest request,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary is null)
        {
            return null;
        }

        var activity = await activityRepository.GetByIdAsync(request.ActivityId, cancellationToken);
        if (activity is null)
        {
            return null;
        }

        itinerary.Items.Add(new ItineraryItem
        {
            ActivityId = activity.Id,
            ScheduledDate = request.ScheduledDate,
            Notes = request.Notes
        });

        var weather = await weatherService.AssessActivityWeatherAsync(activity.Location, request.ScheduledDate, cancellationToken);
        itinerary.Items[^1].ApplyWeather(weather);

        var referencedActivities = await ResolveActivitiesAsync(itinerary.Items, cancellationToken);
        itinerary.RecalculateTotalPrice(referencedActivities);

        await itineraryRepository.UpdateAsync(itinerary, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(itinerary);
    }

    private static Activity? PickBestActivity(
        IReadOnlyCollection<Activity> candidates,
        IReadOnlyCollection<Activity> selected,
        IReadOnlyCollection<ActivityCategory> preferences,
        decimal budget,
        int numberOfPeople,
        decimal runningCost)
    {
        var budgetLeft = budget - runningCost;
        if (budgetLeft <= 0)
        {
            return null;
        }

        return candidates
            .Where(x => preferences.Count == 0 || preferences.Contains(x.Category))
            .Where(x => !selected.Any(s => s.Id == x.Id))
            .Where(x => x.Price * numberOfPeople <= budgetLeft)
            .OrderByDescending(x => preferences.Contains(x.Category))
            .ThenBy(x => x.Price)
            .FirstOrDefault();
    }

    private async Task<List<Activity>> ResolveActivitiesAsync(
        IReadOnlyCollection<ItineraryItem> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(x => x.ActivityId).Distinct().ToArray();
        var list = new List<Activity>(ids.Length);
        foreach (var id in ids)
        {
            var activity = await activityRepository.GetByIdAsync(id, cancellationToken);
            if (activity is not null)
            {
                list.Add(activity);
            }
        }

        return list;
    }

    private static ItineraryResponse ToResponse(Itinerary itinerary) => new(
        itinerary.Id,
        itinerary.Destination,
        itinerary.StartDate,
        itinerary.EndDate,
        itinerary.NumberOfPeople,
        itinerary.Ages,
        itinerary.Preferences,
        itinerary.Budget,
        itinerary.TotalPrice,
        itinerary.Items
            .OrderBy(x => x.ScheduledDate)
            .Select(x => new ItineraryItemResponse(
                x.Id,
                x.ActivityId,
                x.ScheduledDate,
                x.WeatherRiskDetected,
                x.WeatherSummary,
                x.SuggestedAlternativeActivityId,
                x.Notes))
            .ToArray());
}