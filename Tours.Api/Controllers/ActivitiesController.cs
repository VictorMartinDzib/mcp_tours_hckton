using Microsoft.AspNetCore.Mvc;
using Tours.Application.Interfaces;
using Tours.Domain.Enums;

namespace Tours.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ActivitiesController(IActivityService activityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetActivities(
        [FromQuery] string destination,
        [FromQuery] List<ActivityCategory>? categories,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minAge,
        [FromQuery] bool onlyAvailable = true,
        CancellationToken cancellationToken = default)
    {
        var data = await activityService.SearchAsync(
            destination,
            categories ?? [],
            maxPrice,
            minAge,
            onlyAvailable,
            cancellationToken);

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var activity = await activityService.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return NotFound();
        }

        return Ok(activity);
    }
}