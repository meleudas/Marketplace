using MediatR;

namespace Marketplace.Tests.Common.Fakes;

public sealed class RecordingSender : ISender
{
    public object? LastRequest { get; private set; }
    public object? NextResult { get; set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult((TResponse)NextResult!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        LastRequest = request;
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(NextResult);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => Empty<TResponse>();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => Empty<object?>();

    private static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
