using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;

namespace TrackManagement.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development-only seeder. Runs migrations, then seeds each table only if it is empty.
/// Tracks and distributions are seeded together as one unit. Everything is written
/// with a single SaveChanges call, so a failure leaves no partial data.
/// </summary>
public sealed class DbSeeder
{
    private static readonly Regex IsrcFormat = new("^[A-Z]{2}[A-Z0-9]{3}[0-9]{2}[0-9]{5}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, ILogger<DbSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        ValidateSeedData();

        await _db.Database.MigrateAsync(ct);

        var dsps = (await _db.Dsps.ToListAsync(ct))
            .ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
        var artists = (await _db.Artists.ToListAsync(ct))
            .ToDictionary(a => a.Email, StringComparer.OrdinalIgnoreCase);

        var dspsAdded = 0;
        var artistsAdded = 0;
        var tracksAdded = 0;
        var distributionsAdded = 0;

        if (dsps.Count == 0)
        {
            foreach (var name in SeedData.DspNames)
            {
                var dsp = new Dsp { Name = name };
                _db.Dsps.Add(dsp);
                dsps[name] = dsp;
                dspsAdded++;
            }
        }

        if (artists.Count == 0)
        {
            foreach (var seed in SeedData.Artists)
            {
                var artist = new Artist { Name = seed.Name, Email = seed.Email, Country = seed.Country };
                _db.Artists.Add(artist);
                artists[seed.Email] = artist;
                artistsAdded++;
            }
        }

        var tracksEmpty = !await _db.Tracks.AnyAsync(ct);
        var distributionsEmpty = !await _db.TrackDistributions.AnyAsync(ct);

        if (tracksEmpty && distributionsEmpty)
        {
            var missingArtists = SeedData.Tracks.Select(t => t.ArtistEmail).Distinct()
                .Count(e => !artists.ContainsKey(e));
            var missingDsps = SeedData.Distributions.Select(d => d.DspName).Distinct()
                .Count(n => !dsps.ContainsKey(n));

            if (missingArtists > 0 || missingDsps > 0)
            {
                _logger.LogWarning(
                    "Tracks and distributions were not seeded: {MissingArtists} seed artist(s) and {MissingDsps} seed DSP(s) are not in the database.",
                    missingArtists, missingDsps);
            }
            else
            {
                var tracksByIsrc = new Dictionary<string, Track>();

                foreach (var seed in SeedData.Tracks)
                {
                    var track = new Track
                    {
                        Title = seed.Title,
                        Artist = artists[seed.ArtistEmail],
                        Isrc = seed.Isrc,
                        ReleaseDate = seed.ReleaseDate,
                        Genre = seed.Genre,
                        Status = seed.Status
                    };
                    _db.Tracks.Add(track);
                    tracksByIsrc[seed.Isrc] = track;
                    tracksAdded++;
                }

                foreach (var seed in SeedData.Distributions)
                {
                    var track = tracksByIsrc[seed.Isrc];
                    var submittedAt = DateTime.SpecifyKind(
                        track.ReleaseDate.ToDateTime(new TimeOnly(9, 0)).AddDays(-14),
                        DateTimeKind.Utc);

                    _db.TrackDistributions.Add(new TrackDistribution
                    {
                        Track = track,
                        Dsp = dsps[seed.DspName],
                        SubmittedAt = submittedAt,
                        Status = seed.Status
                    });
                    distributionsAdded++;
                }
            }
        }
        else
        {
            _logger.LogInformation("Tracks or distributions already exist, skipping them.");
        }

        if (dspsAdded + artistsAdded + tracksAdded + distributionsAdded > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "Seeding finished. Added: {Dsps} DSPs, {Artists} artists, {Tracks} tracks, {Distributions} distributions.",
            dspsAdded, artistsAdded, tracksAdded, distributionsAdded);
    }

    private static void ValidateSeedData()
    {
        foreach (var track in SeedData.Tracks)
        {
            if (!IsrcFormat.IsMatch(track.Isrc))
                throw new InvalidOperationException("Seed data contains an ISRC with an invalid format.");

            var distributions = SeedData.Distributions.Where(d => d.Isrc == track.Isrc).ToList();

            if (track.Status == TrackStatus.Draft && distributions.Count > 0)
                throw new InvalidOperationException("Seed data: a Draft track has distributions.");

            if (track.Status != TrackStatus.Draft && distributions.Count == 0)
                throw new InvalidOperationException("Seed data: a non-Draft track has no distributions.");

            if (track.Status == TrackStatus.Distributed && !distributions.Any(d => d.Status == DistributionStatus.Live))
                throw new InvalidOperationException("Seed data: a Distributed track has no Live distribution.");
        }
    }
}
