using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ImageInterpreter.Problems;

public sealed class NonOperationRoutesMiddleware
{
    private const string ProblemMediaType = "application/problem+json";

    private static readonly PathString CollectionPath = new("/api/v1/interpretations");

    private readonly RequestDelegate _next;

    public NonOperationRoutesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (IsCollection(path))
        {
            if (IsDisallowedCollectionMethod(context.Request.Method))
            {
                return WriteAsync(
                    context,
                    ProblemCatalog.MethodNotAllowed(InstanceOf(context)),
                    allowPost: true);
            }

            return _next(context);
        }

        if (IsIdPath(path))
        {
            return WriteAsync(
                context,
                ProblemCatalog.NotFound(InstanceOf(context)),
                allowPost: false);
        }

        return _next(context);
    }

    private static bool IsCollection(PathString path) =>
        path.Equals(CollectionPath, StringComparison.OrdinalIgnoreCase);

    private static bool IsIdPath(PathString path)
    {
        if (!path.StartsWithSegments(CollectionPath, StringComparison.OrdinalIgnoreCase, out var remainder))
        {
            return false;
        }

        var value = remainder.Value;
        return value is not null
            && value.Length > 1
            && value[0] == '/'
            && value.IndexOf('/', 1) < 0;
    }

    private static bool IsDisallowedCollectionMethod(string method) =>
        HttpMethods.IsGet(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method)
        || HttpMethods.IsHead(method);

    private static string InstanceOf(HttpContext context) =>
        context.Request.Path.Value ?? string.Empty;

    private static async Task WriteAsync(HttpContext context, ProblemDetails problem, bool allowPost)
    {
        context.Response.StatusCode = problem.Status ?? throw new InvalidOperationException("Problem status is required.");
        context.Response.ContentType = ProblemMediaType;

        if (allowPost)
        {
            context.Response.Headers["Allow"] = "POST";
        }

        await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
    }
}
