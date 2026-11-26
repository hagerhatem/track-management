using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Artists;
using TrackManagement.Application.Common.Exceptions;
using TrackManagement.Domain.Entities;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Infrastructure.Services;

public class ArtistService : IArtistService
{
    private const string DuplicateEmailMessage = "An artist with this email already exists.";

    private readonly AppDbContext _db;
    private readonly IValidator<CreateArtistRequest> _validator;

    public ArtistService(AppDbContext db, IValidator<CreateArtistRequest> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<ArtistResponse> CreateAsync(CreateArtistRequest request, CancellationToken cancellationToken)
    {
        // Normalize first, then validate, so " A@X.com " is judged (and stored) as "a@x.com".
        var normalized = request with
        {
            Name = request.Name?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            Country = request.Country?.Trim()
        };

        await _validator.ValidateAndThrowAsync(normalized, cancellationToken);

        var email = normalized.Email!;
        if (await _db.Artists.AnyAsync(a => a.Email == email, cancellationToken))
        {
            throw new ConflictException(DuplicateEmailMessage);
        }

        var artist = new Artist
        {
            Name = normalized.Name!,
            Email = email,
            Country = normalized.Country!
        };

        _db.Artists.Add(artist);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        // 2601 = duplicate key in a unique index, 2627 = unique constraint violation.
        // Covers the race where two requests pass the AnyAsync check at the same time.
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException(DuplicateEmailMessage);
        }

        return new ArtistResponse(artist.Id, artist.Name, artist.Email, artist.Country);
    }

    public async Task<IReadOnlyList<ArtistResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.Artists
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Select(a => new ArtistResponse(a.Id, a.Name, a.Email, a.Country))
            .ToListAsync(cancellationToken);

    public async Task<ArtistResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var artist = await _db.Artists
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new ArtistResponse(a.Id, a.Name, a.Email, a.Country))
            .FirstOrDefaultAsync(cancellationToken);

        return artist ?? throw new NotFoundException($"Artist with id {id} was not found.");
    }
}
