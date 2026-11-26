namespace TrackManagement.Application.Tracks;

public interface ITrackWorkflowService
{
    Task<IReadOnlyList<TrackDistributionResponse>> DistributeAsync(int trackId, DistributeTrackRequest request, CancellationToken cancellationToken);

    Task<TrackDetailResponse> UpdateStatusAsync(int trackId, UpdateTrackStatusRequest request, CancellationToken cancellationToken);
}
