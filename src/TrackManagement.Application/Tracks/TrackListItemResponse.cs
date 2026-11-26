namespace TrackManagement.Application.Tracks;

public record TrackListItemResponse(
    int Id,
    string Title,
    string ArtistName,
    string Genre,
    string Status,
    string Isrc,
    DateOnly ReleaseDate);
