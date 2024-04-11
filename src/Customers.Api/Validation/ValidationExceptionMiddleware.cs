using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Customers.Api.Validation;

public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _request;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ValidationExceptionMiddleware(RequestDelegate request)
    {
        _request = request;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _request(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = 400;
            
            var error = new ValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Status = 400,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };
            foreach (var validationFailure in exception.Errors)
            {
                error.Errors.Add(new KeyValuePair<string, string[]>(
                    validationFailure.PropertyName, 
                    new[] { validationFailure.ErrorMessage }));
            }
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(error, _jsonSerializerOptions));
        }
    }
}
