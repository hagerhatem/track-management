using FluentValidation;
using FluentValidation.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Common.Exceptions;
using TrackManagement.Application.Tracks;
using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Infrastructure.Services;

public class TrackService : ITrackService
{
    private const string DuplicateIsrcMessage = "A track with this ISRC already exists.";

    private readonly AppDbContext _db;
    private readonly IValidator<CreateTrackRequest> _validator;

    public TrackService(AppDbContext db, IValidator<CreateTrackRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<TrackDetailResponse> CreateAsync(CreateTrackRequest request, CancellationToken cancellationToken)
    {
        // Normalize first, then validate, so "eg-a01-24-00001" is judged and stored as "EGA012400001".
        var normalized = request with
        {
            Title = request.Title?.Trim(),
            Isrc = IsrcNormalizer.Normalize(request.Isrc),
            Genre = request.Genre?.Trim()
        };

        await _validator.ValidateAndThrowAsync(normalized, cancellationToken);

        var artistExists = await _db.Artists.AnyAsync(a => a.Id == normalized.ArtistId, cancellationToken);
        if (!artistExists)
        {
            throw new NotFoundException($"Artist with id {normalized.ArtistId} was not found.");
        }

        var isrc = normalized.Isrc!;
        if (await _db.Tracks.AnyAsync(t => t.Isrc == isrc, cancellationToken))
        {
            throw new ConflictException(DuplicateIsrcMessage);
        }

        var track = new Track
        {
            Title = normalized.Title!,
            ArtistId = normalized.ArtistId,
            Isrc = isrc,
            ReleaseDate = normalized.ReleaseDate!.Value,
            Genre = normalized.Genre!,
            Status = TrackStatus.Draft
        };

        _db.Tracks.Add(track);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        // 2601 = duplicate key in a unique index, 2627 = unique constraint violation.
        // Covers the race where two requests pass the AnyAsync check at the same time.
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException(DuplicateIsrcMessage);
        }

        return await GetByIdAsync(track.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TrackListItemResponse>> GetAllAsync(TrackFilter filter, CancellationToken cancellationToken)
    {
        TrackStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var raw = filter.Status.Trim();
            // IsDefined guards against numeric strings like "7" that Enum.TryParse would accept.
            if (!Enum.TryParse<TrackStatus>(raw, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                var allowed = string.Join(", ", Enum.GetNames<TrackStatus>());
                throw new ValidationException(new[]
                {
                    new ValidationFailure("status", $"Status must be one of: {allowed}.")
                });
            }

            status = parsed;
        }

        var query = _db.Tracks.AsNoTracking().AsQueryable();

        if (filter.ArtistId is not null)
        {
            query = query.Where(t => t.ArtistId == filter.ArtistId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Genre))
        {
            var genre = filter.Genre.Trim().ToLower();
            query = query.Where(t => t.Genre.ToLower() == genre);
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        return await query
            .OrderBy(t => t.Id)
            .Select(t => new TrackListItemResponse(
                t.Id,
                t.Title,
                t.Artist.Name,
                t.Genre,
                t.Status.ToString(),
                t.Isrc,
                t.ReleaseDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<TrackDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var track = await _db.Tracks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TrackDetailResponse(
                t.Id,
                t.Title,
                t.Isrc,
                t.ReleaseDate,
                t.Genre,
                t.Status.ToString(),
                new TrackArtistResponse(t.Artist.Id, t.Artist.Name, t.Artist.Country),
                t.Distributions
                    .OrderBy(d => d.Id)
                    .Select(d => new TrackDistributionResponse(d.Dsp.Name, d.Status.ToString(), d.SubmittedAt))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return track ?? throw new NotFoundException($"Track with id {id} was not found.");
    }
}