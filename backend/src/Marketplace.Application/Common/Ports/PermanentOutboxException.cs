namespace Marketplace.Application.Common.Ports;

public sealed class PermanentOutboxException : Exception
{
    public PermanentOutboxException(string message)
        : base(message)
    {
    }
}
