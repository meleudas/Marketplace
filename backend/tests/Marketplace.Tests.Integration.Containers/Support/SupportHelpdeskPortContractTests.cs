using Marketplace.Application.Support.Ports;
using Marketplace.Infrastructure.External.Support;

namespace Marketplace.Tests;

[Trait("Suite", "Support")]
[Trait("Layer", "IntegrationContainers")]
public sealed class SupportHelpdeskPortContractTests
{
    [Fact]
    public async Task LoggingHelpdeskPort_Returns_External_Id_On_Create()
    {
        var port = new LoggingHelpdeskPort(Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingHelpdeskPort>.Instance);
        var result = await port.CreateTicketAsync(
            new HelpdeskCreateRequest(42, "SUP-001", "Subject", "Body", 1, Guid.NewGuid().ToString()));
        Assert.StartsWith("logging-42", result.ExternalTicketId, StringComparison.Ordinal);

        var snapshot = await port.FetchTicketSnapshotAsync(result.ExternalTicketId);
        Assert.NotNull(snapshot);
        Assert.Equal(result.ExternalTicketId, snapshot!.ExternalTicketId);
    }
}
