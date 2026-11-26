namespace TrackManagement.Application.Tracks;

public interface ITrackService
{
    /// <exception cref="FluentValidation.ValidationException">Input is invalid.</exception>
    /// <exception cref="Common.Exceptions.NotFoundException">The artist does not exist.</exception>
    /// <exception cref="Common.Exceptions.ConflictException">A track with this ISRC already exists.</exception>
    Task<TrackDetailResponse> CreateAsync(CreateTrackRequest request, CancellationToken cancellationToken);

    /// <exception cref="FluentValidation.ValidationException">The status filter is not a valid value.</exception>
    Task<IReadOnlyList<TrackListItemResponse>> GetAllAsync(TrackFilter filter, CancellationToken cancellationToken);

    /// <exception cref="Common.Exceptions.NotFoundException">No track with this id.</exception>
    Task<TrackDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
