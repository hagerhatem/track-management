using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackManagement.Application.Tracks;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Route("api/tracks")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _tracks;
    private readonly ITrackWorkflowService _workflow;

    public TracksController(ITrackService tracks, ITrackWorkflowService workflow)
    {
        _tracks = tracks;
        _workflow = workflow;
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

    [HttpPost("{id:int}/distribute")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<TrackDistributionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<TrackDistributionResponse>>> Distribute(int id, DistributeTrackRequest request, CancellationToken cancellationToken) =>
        Ok(await _workflow.DistributeAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TrackDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackDetailResponse>> UpdateStatus(int id, UpdateTrackStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await _workflow.UpdateStatusAsync(id, request, cancellationToken));
}
