using FluentValidation;
using Marketplace.Domain.Shared.Kernel;
using MediatR;


namespace Marketplace.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
     where TRequest : IRequest<Result<TResponse>>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, ct))
                );

                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Any())
                {
                    // Створюємо помилку Result з переліком проблем
                    var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

                    // Якщо відповідь Result, повертаємо Failure
                    if (typeof(TResponse).IsGenericType &&
                        typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var failureMethod = typeof(Result<>)
                            .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                            .GetMethod(nameof(Result<object>.Failure));

                        return (TResponse)failureMethod!.Invoke(null, new object[] { errorMessage })!;
                    }
                }
            }

            return await next();
        }
    }
}
