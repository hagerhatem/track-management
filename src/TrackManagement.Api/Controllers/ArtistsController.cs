using Microsoft.AspNetCore.Mvc;
using TrackManagement.Application.Artists;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Route("api/artists")]
public class ArtistsController : ControllerBase
{
    private readonly IArtistService _artists;

    public ArtistsController(IArtistService artists)
    {
        _artists = artists;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ArtistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArtistResponse>> Create(CreateArtistRequest request, CancellationToken cancellationToken)
    {
        var artist = await _artists.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, artist);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ArtistResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArtistResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _artists.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ArtistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtistResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _artists.GetByIdAsync(id, cancellationToken));
}
