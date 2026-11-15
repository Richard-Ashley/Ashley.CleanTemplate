using System.Text.Json;
using Application.Common.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                title: "Validation failed",
                detail: ex.Message,
                errors: ex.Errors.ToDictionary(kvp => ToCamelCasePath(kvp.Key), kvp => kvp.Value));
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                title: "Domain rule violated",
                detail: ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblem(context, StatusCodes.Status409Conflict,
                title: "Concurrency conflict",
                detail: "The resource was modified by another request. Reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                title: "An error occurred",
                detail: "An unexpected error occurred while processing your request.");
        }
    }

    // "Items[0].Quantity" -> "items[0].quantity": camelCase every property segment, keep indexers.
    private static string ToCamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(segment =>
            segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));

    private static async Task WriteProblem(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ValidationProblemDetails(
            errors is null
                ? new Dictionary<string, string[]>()
                : errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is null)
        {
            var simple = new ProblemDetails
            {
                Type = problem.Type,
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = problem.Instance
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, simple,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return;
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, problem,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
