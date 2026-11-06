using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ImageInterpreter.Problems;

public static class ProblemJson
{
    public const string MediaType = "application/problem+json";

    public static async Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        context.Response.StatusCode = problem.Status ?? throw new InvalidOperationException("Problem status is required.");
        context.Response.ContentType = MediaType;
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: context.RequestAborted);
    }
}
