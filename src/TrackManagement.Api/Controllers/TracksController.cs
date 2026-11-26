using Microsoft.AspNetCore.Mvc;
using TrackManagement.Application.Tracks;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Route("api/tracks")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _tracks;

    public TracksController(ITrackService tracks)
    {
        _tracks = tracks;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TrackDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackDetailResponse>> Create(CreateTrackRequest request, CancellationToken cancellationToken)
    {
        var track = await _tracks.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = track.Id }, track);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TrackListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TrackListItemResponse>>> GetAll(
        [FromQuery] int? artistId,
        [FromQuery] string? genre,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await _tracks.GetAllAsync(new TrackFilter(artistId, genre, status), cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TrackDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackDetailResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _tracks.GetByIdAsync(id, cancellationToken));
}