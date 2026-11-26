namespace TrackManagement.Application.Artists;

public interface IArtistService
{
    /// <exception cref="FluentValidation.ValidationException">Input is invalid.</exception>
    /// <exception cref="Common.Exceptions.ConflictException">An artist with this email already exists.</exception>
    Task<ArtistResponse> CreateAsync(CreateArtistRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ArtistResponse>> GetAllAsync(CancellationToken cancellationToken);

    /// <exception cref="Common.Exceptions.NotFoundException">No artist with this id.</exception>
    Task<ArtistResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
