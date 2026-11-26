namespace TrackManagement.Application.Tracks;

public static class IsrcNormalizer
{
    /// <summary>Trims, removes dashes and spaces, and upper-cases. Returns null for null input.</summary>
    public static string? Normalize(string? isrc) =>
        isrc is null
            ? null
            : isrc.Trim().Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
}
