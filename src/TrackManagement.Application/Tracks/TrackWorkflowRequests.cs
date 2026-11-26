namespace TrackManagement.Application.Tracks;

public record DistributeTrackRequest(IReadOnlyList<int>? DspIds);

public record UpdateTrackStatusRequest(string? Status);
