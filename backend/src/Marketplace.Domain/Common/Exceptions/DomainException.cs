using System;

namespace Marketplace.Domain.Common.Exceptions;

public class DomainException : System.Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
