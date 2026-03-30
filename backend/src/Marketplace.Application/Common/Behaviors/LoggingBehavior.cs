using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;


namespace Marketplace.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : IRequest<Result<TResponse>>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid().ToString("N")[..8];


            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("RequestName", requestName))
            {
                _logger.LogInformation(
                    "[{RequestId}] Handling {RequestName} with {@Request}",
                    requestId,
                    requestName,
                    request
                );

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    var response = await next();

                    stopwatch.Stop();

                    if (response is Result result)
                    {
                        if (result.IsFailure)
                        {
                            _logger.LogWarning(
                                "[{RequestId}] {RequestName} failed with error: {Error}",
                                requestId,
                                requestName,
                                result.Error
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "[{RequestId}] {RequestName} completed successfully in {ElapsedMilliseconds}ms",
                                requestId,
                                requestName,
                                stopwatch.ElapsedMilliseconds
                            );
                        }
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    _logger.LogError(
                        ex,
                        "[{RequestId}] {RequestName} threw exception after {ElapsedMilliseconds}ms",
                        requestId,
                        requestName,
                        stopwatch.ElapsedMilliseconds
                    );

                    throw;
                }
            }
        }
    }
}
