using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Marketplace.API.Filters;

/// <summary>Додаткова перевірка ModelState (поруч із вбудованою поведінкою <see cref="ApiControllerAttribute"/>).</summary>
public sealed class ValidateModelAttribute : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
