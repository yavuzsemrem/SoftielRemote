using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SoftielRemote.Backend.Api.Configuration;

namespace SoftielRemote.Backend.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ApplicationOptions _applicationOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<ApplicationOptions> applicationOptions)
    {
        _next = next;
        _logger = logger;
        _applicationOptions = applicationOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var isDevelopment = _applicationOptions.Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            
            if (isDevelopment)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "An unhandled exception occurred");
            }
            
            await HandleExceptionAsync(context, ex, isDevelopment);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, bool includeDetails)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        object response;
        
        if (includeDetails)
        {
            response = new
            {
                error = "An error occurred while processing your request",
                message = exception.Message,
                stackTrace = exception.StackTrace,
                statusCode = context.Response.StatusCode
            };
        }
        else
        {
            response = new
            {
                error = "An error occurred while processing your request",
                statusCode = context.Response.StatusCode
            };
        }

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}




