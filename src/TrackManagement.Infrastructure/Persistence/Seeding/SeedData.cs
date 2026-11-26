using TrackManagement.Domain.Enums;

namespace TrackManagement.Infrastructure.Persistence.Seeding;

internal static class SeedData
{
    public sealed record ArtistSeed(string Name, string Email, string Country);

    public sealed record TrackSeed(
        string Title,
        string ArtistEmail,
        string Isrc,
        DateOnly ReleaseDate,
        string Genre,
        TrackStatus Status);

    public sealed record DistributionSeed(string Isrc, string DspName, DistributionStatus Status);

    public static readonly string[] DspNames = { "Spotify", "Apple Music", "YouTube" };

    public static readonly ArtistSeed[] Artists =
    {
        new("Karim Nassar", "karim.nassar@example.com", "EG"),
        new("Omar Al Mansoori", "omar.almansoori@example.com", "AE"),
        new("Rami Haddad", "rami.haddad@example.com", "LB"),
        new("Noura Al Qahtani", "noura.alqahtani@example.com", "SA")
    };

    public static readonly TrackSeed[] Tracks =
    {
        new("Ya Habibi El Dunya", "karim.nassar@example.com", "EGK012300001", new DateOnly(2023, 2, 10), "Mahraganat", TrackStatus.Distributed),
        new("Hara Beat", "karim.nassar@example.com", "EGK012400002", new DateOnly(2024, 5, 17), "Shaabi", TrackStatus.Distributed),
        new("Zahma", "karim.nassar@example.com", "EGK012500003", new DateOnly(2025, 9, 5), "Hip-Hop", TrackStatus.Submitted),
        new("Desert Pulse", "omar.almansoori@example.com", "AEO012300001", new DateOnly(2023, 11, 3), "Electronic", TrackStatus.Distributed),
        new("Skyline Dubai", "omar.almansoori@example.com", "AEO012500002", new DateOnly(2025, 4, 12), "Pop", TrackStatus.Submitted),
        new("Midnight Souk", "omar.almansoori@example.com", "AEO012600003", new DateOnly(2026, 12, 20), "Electronic", TrackStatus.Draft),
        new("Beirut Sunrise", "rami.haddad@example.com", "LBR012400001", new DateOnly(2024, 8, 22), "Arabic Pop", TrackStatus.Distributed),
        new("Ya Lebnan", "rami.haddad@example.com", "LBR012600002", new DateOnly(2026, 10, 30), "Pop", TrackStatus.Submitted),
        new("Najdi Dreams", "noura.alqahtani@example.com", "SAN012500001", new DateOnly(2025, 12, 1), "Arabic Pop", TrackStatus.Submitted),
        new("Riyadh Flow", "noura.alqahtani@example.com", "SAN012700001", new DateOnly(2027, 1, 15), "Hip-Hop", TrackStatus.Draft)
    };

    public static readonly DistributionSeed[] Distributions =
    {
        new("EGK012300001", "Spotify", DistributionStatus.Live),
        new("EGK012300001", "Apple Music", DistributionStatus.Live),
        new("EGK012300001", "YouTube", DistributionStatus.Pending),
        new("EGK012400002", "Spotify", DistributionStatus.Live),
        new("EGK012400002", "YouTube", DistributionStatus.Rejected),
        new("EGK012500003", "Spotify", DistributionStatus.Pending),
        new("EGK012500003", "Apple Music", DistributionStatus.Pending),
        new("AEO012300001", "Spotify", DistributionStatus.Live),
        new("AEO012300001", "Apple Music", DistributionStatus.Live),
        new("AEO012300001", "YouTube", DistributionStatus.Live),
        new("AEO012500002", "Spotify", DistributionStatus.Live),
        new("AEO012500002", "YouTube", DistributionStatus.Rejected),
        new("LBR012400001", "Apple Music", DistributionStatus.Live),
        new("LBR012400001", "Spotify", DistributionStatus.Pending),
        new("LBR012600002", "Spotify", DistributionStatus.Pending),
        new("SAN012500001", "Apple Music", DistributionStatus.Pending),
        new("SAN012500001", "YouTube", DistributionStatus.Rejected)
    };
}
