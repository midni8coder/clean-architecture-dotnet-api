namespace Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific exceptions.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; set; }

    public DomainException(string message, string code = "DOMAIN_ERROR") 
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, string code, Exception innerException) 
        : base(message, innerException)
    {
        Code = code;
    }
}
