namespace Marketplace.Application.Common.Exceptions;

public sealed class ConcurrencyConflictException : ApplicationException
{
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
