namespace TrackManagement.Application.Tracks;

/// <summary>Raw query-string filters. Status is a string so invalid values can be rejected with a clear 400.</summary>
public record TrackFilter(int? ArtistId, string? Genre, string? Status);
