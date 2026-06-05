using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wex.Purchase.API.Models;

namespace Wex.Purchase.API.Filters;

public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.SelectMany(kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>()).ToList();
                 
            var errorResponse = new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Errors = errors
            };

            context.Result = new BadRequestObjectResult(errorResponse);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // no-op
    }
}
