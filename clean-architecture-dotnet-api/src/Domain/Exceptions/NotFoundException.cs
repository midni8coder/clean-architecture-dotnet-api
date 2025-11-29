namespace Domain.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) 
        : base(message, "NOT_FOUND")
    {
    }
}
