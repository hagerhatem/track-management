using FluentValidation;

namespace TrackManagement.Application.Artists;

public class CreateArtistRequestValidator : AbstractValidator<CreateArtistRequest>
{
    public CreateArtistRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        // Format check only: two uppercase ASCII letters. It does not verify the code is an assigned ISO 3166 country.
        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Matches("^[A-Z]{2}$").WithMessage("Country must be a 2-letter uppercase ISO code (e.g. EG).");
    }
}
