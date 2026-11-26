namespace TrackManagement.Application.Common.Exceptions;

/// <summary>The request conflicts with existing data (e.g. duplicate). Mapped to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
