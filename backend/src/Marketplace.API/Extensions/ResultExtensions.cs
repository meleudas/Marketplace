using Marketplace.Domain.Shared.Kernel;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result is { IsSuccess: true, Value: not null })
            return new OkObjectResult(result.Value);

        var status = MapStatus(result.Error);
        return new ObjectResult(new ProblemDetails
        {
            Title = status == StatusCodes.Status401Unauthorized ? "Неавторизовано" : "Помилка запиту",
            Detail = result.Error,
            Status = status
        })
        {
            StatusCode = status
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        var status = MapStatus(result.Error);
        return new ObjectResult(new ProblemDetails
        {
            Title = "Помилка запиту",
            Detail = result.Error,
            Status = status
        })
        {
            StatusCode = status
        };
    }

    private static int MapStatus(string? error)
    {
        if (string.IsNullOrEmpty(error))
            return StatusCodes.Status400BadRequest;
        var e = error.ToLowerInvariant();
        if (e.Contains("invalid email or password") ||
            e.Contains("invalid or expired refresh") ||
            e.Contains("invalid confirmation") ||
            e.Contains("invalid reset") ||
            e.Contains("2fa code required"))
            return StatusCodes.Status401Unauthorized;
        return StatusCodes.Status400BadRequest;
    }
}
