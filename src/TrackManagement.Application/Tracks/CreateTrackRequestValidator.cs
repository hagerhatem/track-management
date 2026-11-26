using FluentValidation;

namespace TrackManagement.Application.Tracks;

public class CreateTrackRequestValidator : AbstractValidator<CreateTrackRequest>
{
    // Release dates up to one year ahead are allowed (pre-releases).
    private static readonly DateOnly MinReleaseDate = new(1900, 1, 1);

    public CreateTrackRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.ArtistId)
            .GreaterThan(0).WithMessage("ArtistId is required and must be a positive number.");

        // Expects an already normalized value (upper case, no dashes).
        RuleFor(x => x.Isrc)
            .NotEmpty().WithMessage("Isrc is required.")
            .Matches("^[A-Z]{2}[A-Z0-9]{3}[0-9]{2}[0-9]{5}$")
            .WithMessage("Isrc must be 12 characters: 2 letters, 3 letters/digits, 2 digits, 5 digits (e.g. EGA012400001).");

        RuleFor(x => x.ReleaseDate)
            .NotNull().WithMessage("ReleaseDate is required.")
            .Must(d => d is null || (d.Value >= MinReleaseDate && d.Value <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)))
            .WithMessage("ReleaseDate must be between 1900-01-01 and one year from today.");

        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage("Genre is required.")
            .MaximumLength(100).WithMessage("Genre must not exceed 100 characters.");
    }
}
