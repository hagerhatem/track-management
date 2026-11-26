using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TrackManagement.Api.Middleware;

/// <summary>
/// Replaces the default 400 produced by [ApiController] model binding. The default echoes
/// System.Text.Json parser messages (path, line, byte position); here the client only learns
/// that the body is unusable, never the serializer's internal text.
/// </summary>
public static class InvalidModelStateResponse
{
    private const string BodyKey = "body";
    private const string BodyMessage = "The request body is missing, is not valid JSON, or contains a value of the wrong type.";
    private const string FieldMessage = "The value is invalid.";

    public static IActionResult Create(ActionContext context)
    {
        // MVC reports body errors under the [FromBody] parameter name and under "$..." paths.
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sanitized = new ModelStateDictionary();
        var keys = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => e.Key)
            .Distinct();

        foreach (var key in keys)
        {
            var isBody = string.IsNullOrEmpty(key) || key.StartsWith('$') || bodyParameters.Contains(key);

            if (!isBody)
            {
                sanitized.AddModelError(key, FieldMessage);
            }
            else if (!sanitized.ContainsKey(BodyKey))
            {
                sanitized.AddModelError(BodyKey, BodyMessage);
            }
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateValidationProblemDetails(context.HttpContext, sanitized);

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}