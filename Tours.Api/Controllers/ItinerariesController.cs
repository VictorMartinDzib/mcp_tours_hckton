using Microsoft.AspNetCore.Mvc;
using Tours.Application.Dtos;
using Tours.Application.Interfaces;

namespace Tours.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ItinerariesController(IItineraryService itineraryService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult> Generate(
        [FromBody] GenerateItineraryRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return BadRequest("La fecha inicial no puede ser mayor a la fecha final.");
        }

        if (request.NumberOfPeople <= 0)
        {
            return BadRequest("El numero de personas debe ser mayor a 0.");
        }

        var itinerary = await itineraryService.GenerateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = itinerary.Id }, itinerary);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.GetByIdAsync(id, cancellationToken);
        return itinerary is null ? NotFound() : Ok(itinerary);
    }

    [HttpPut("{id:guid}/replace-activity")]
    public async Task<ActionResult> ReplaceActivity(
        Guid id,
        [FromBody] ReplaceItineraryActivityRequest request,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.ReplaceActivityAsync(id, request, cancellationToken);
        return itinerary is null ? NotFound() : Ok(itinerary);
    }

    [HttpPost("{id:guid}/add-activity")]
    public async Task<ActionResult> AddActivity(
        Guid id,
        [FromBody] AddItineraryActivityRequest request,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraryService.AddActivityAsync(id, request, cancellationToken);
        return itinerary is null ? NotFound() : Ok(itinerary);
    }
}