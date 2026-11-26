namespace TrackManagement.Application.Artists;

// Properties are nullable on purpose: a missing field must reach the FluentValidation
// validator so every validation error has the same shape (camelCase field -> messages).
public record CreateArtistRequest(string? Name, string? Email, string? Country);
