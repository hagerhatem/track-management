namespace TrackManagement.Application.Tracks;

/// <summary>Create payload. There is deliberately no Status: new tracks always start as Draft.</summary>
public record CreateTrackRequest(
    string? Title,
    int ArtistId,
    string? Isrc,
    DateOnly? ReleaseDate,
    string? Genre);
