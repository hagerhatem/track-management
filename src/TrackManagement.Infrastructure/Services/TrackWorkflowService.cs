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

// Workflow rules for tracks (distribute and status).
// Distribute: DspIds must be non-empty and distinct (400); the track must exist (404);
//   every DSP must exist (400, missing ids listed); a DSP that already has a distribution
//   for this track gives 409 listing the ids and nothing is inserted.
//   New rows are Pending with SubmittedAt = UTC now. A Draft track becomes Submitted.
//   Everything is saved with a single SaveChanges (all or nothing).
//   A Distributed track can still be sent to additional DSPs.
// Status: only Draft->Submitted, Submitted->Distributed and Submitted->Draft are allowed.
//   Distributed is final. Submitted->Distributed needs at least one Live distribution.
//   Any other change (including the same status) gives 409.
//   The status must be a known name; numeric strings are rejected (400).
public class TrackWorkflowService : ITrackWorkflowService
{
    private static readonly HashSet<(TrackStatus From, TrackStatus To)> AllowedTransitions = new()
    {
        (TrackStatus.Draft, TrackStatus.Submitted),
        (TrackStatus.Submitted, TrackStatus.Distributed),
        (TrackStatus.Submitted, TrackStatus.Draft)
    };

    private readonly AppDbContext _db;
    private readonly ITrackService _tracks;

    public TrackWorkflowService(AppDbContext db, ITrackService tracks)
    {
        _db = db;
        _tracks = tracks;
    }

    public async Task<IReadOnlyList<TrackDistributionResponse>> DistributeAsync(int trackId, DistributeTrackRequest request, CancellationToken cancellationToken)
    {
        var requested = request.DspIds?.ToList();
        if (requested is null || requested.Count == 0)
        {
            throw Invalid("dspIds", "DspIds must contain at least one id.");
        }

        if (requested.Distinct().Count() != requested.Count)
        {
            throw Invalid("dspIds", "DspIds must not contain duplicates.");
        }

        var track = await _db.Tracks.FirstOrDefaultAsync(t => t.Id == trackId, cancellationToken)
            ?? throw new NotFoundException($"Track with id {trackId} was not found.");

        var existingDspIds = await _db.Dsps.AsNoTracking()
            .Where(d => requested.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        var missing = requested.Except(existingDspIds).OrderBy(id => id).ToList();
        if (missing.Count > 0)
        {
            throw Invalid("dspIds", $"DSPs not found: {string.Join(", ", missing)}.");
        }

        var alreadyDistributed = await _db.TrackDistributions.AsNoTracking()
            .Where(d => d.TrackId == trackId && requested.Contains(d.DspId))
            .Select(d => d.DspId)
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        if (alreadyDistributed.Count > 0)
        {
            throw new ConflictException($"This track already has a distribution for DSP ids: {string.Join(", ", alreadyDistributed)}.");
        }

        var now = DateTime.UtcNow;
        foreach (var dspId in requested)
        {
            _db.TrackDistributions.Add(new TrackDistribution
            {
                TrackId = trackId,
                DspId = dspId,
                SubmittedAt = now,
                Status = DistributionStatus.Pending
            });
        }

        if (track.Status == TrackStatus.Draft)
        {
            track.Status = TrackStatus.Submitted;
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        // 2601 / 2627 = unique index violation on (TrackId, DspId): a concurrent request won the race.
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("One or more DSPs already have a distribution for this track.");
        }

        return await _db.TrackDistributions.AsNoTracking()
            .Where(d => d.TrackId == trackId)
            .OrderBy(d => d.Id)
            .Select(d => new TrackDistributionResponse(d.Dsp.Name, d.Status.ToString(), d.SubmittedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TrackDetailResponse> UpdateStatusAsync(int trackId, UpdateTrackStatusRequest request, CancellationToken cancellationToken)
    {
        var target = ParseStatus(request.Status);

        var track = await _db.Tracks.FirstOrDefaultAsync(t => t.Id == trackId, cancellationToken)
            ?? throw new NotFoundException($"Track with id {trackId} was not found.");

        var current = track.Status;
        if (!AllowedTransitions.Contains((current, target)))
        {
            throw new ConflictException($"Cannot change status from {current} to {target}. Allowed: Draft to Submitted, Submitted to Distributed, Submitted to Draft. Distributed is final.");
        }

        if (target == TrackStatus.Distributed)
        {
            var hasLive = await _db.TrackDistributions.AsNoTracking()
                .AnyAsync(d => d.TrackId == trackId && d.Status == DistributionStatus.Live, cancellationToken);
            if (!hasLive)
            {
                throw new ConflictException("A track can be marked Distributed only when at least one of its distributions is Live.");
            }
        }

        track.Status = target;
        await _db.SaveChangesAsync(cancellationToken);

        return await _tracks.GetByIdAsync(trackId, cancellationToken);
    }

    // Matches by name only, so numeric strings such as "1" are rejected.
    private static TrackStatus ParseStatus(string? raw)
    {
        var name = Enum.GetNames<TrackStatus>()
            .FirstOrDefault(n => string.Equals(n, raw?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (name is null)
        {
            throw Invalid("status", $"Status must be one of: {string.Join(", ", Enum.GetNames<TrackStatus>())}.");
        }

        return Enum.Parse<TrackStatus>(name);
    }

    private static ValidationException Invalid(string field, string message) =>
        new(new[] { new ValidationFailure(field, message) });
}
