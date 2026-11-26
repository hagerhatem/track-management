namespace TrackManagement.Application.Tracks;

public record TrackArtistResponse(int Id, string Name, string Country);

public record TrackDistributionResponse(string DspName, string Status, DateTime SubmittedAt);

public record TrackDetailResponse(
    int Id,
    string Title,
    string Isrc,
    DateOnly ReleaseDate,
    string Genre,
    string Status,
    TrackArtistResponse Artist,
    IReadOnlyList<TrackDistributionResponse> Distributions);
