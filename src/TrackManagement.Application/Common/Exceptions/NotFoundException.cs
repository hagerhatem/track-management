namespace TrackManagement.Application.Common.Exceptions;

/// <summary>A requested resource does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
