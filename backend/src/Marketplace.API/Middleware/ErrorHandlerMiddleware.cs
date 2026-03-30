using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marketplace.Application.Common.Exceptions;
using Marketplace.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Marketplace.API.Middleware;

public sealed class ErrorHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        var problem = ex switch
        {
            ValidationException vex => new ProblemDetails
            {
                Title = "Помилка валідації",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = vex.Errors }
            },
            DomainException dex => new ProblemDetails
            {
                Title = "Помилка домену",
                Detail = dex.Message,
                Status = StatusCodes.Status400BadRequest
            },
            Marketplace.Application.Common.Exceptions.ApplicationException aex => new ProblemDetails
            {
                Title = "Помилка застосунку",
                Detail = aex.Message,
                Status = StatusCodes.Status400BadRequest
            },
            _ => new ProblemDetails
            {
                Title = "Внутрішня помилка сервера",
                Detail = _environment.IsDevelopment() ? ex.ToString() : "Спробуйте пізніше.",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
