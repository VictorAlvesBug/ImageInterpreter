using System.Reflection;
using ImageInterpreter.Problems;
using Microsoft.AspNetCore.Mvc;

namespace ImageInterpreter.Tests;

public class ProblemCatalogTests
{
    [Fact]
    public void Each_catalog_method_returns_the_contract_literals()
    {
        const string instance = "/api/v1/interpretations/7c9e6679-7425-40de-944b-e07fc1f90ae7";
        var expected = new (string Type, string Title, int Status, string Detail)[]
        {
            ("urn:imageinterpreter:problem:invalid-request", "Bad Request", 400, "Content-Type must be multipart/form-data."),
            ("urn:imageinterpreter:problem:invalid-request", "Bad Request", 400, "The request must include one file field named image."),
            ("urn:imageinterpreter:problem:invalid-request", "Bad Request", 400, "The request must include exactly one file field named image."),
            ("urn:imageinterpreter:problem:invalid-request", "Bad Request", 400, "The image file is empty."),
            ("urn:imageinterpreter:problem:unauthorized", "Unauthorized", 401, "A bearer token is required."),
            ("urn:imageinterpreter:problem:unauthorized", "Unauthorized", 401, "The bearer token was not accepted."),
            ("urn:imageinterpreter:problem:forbidden", "Forbidden", 403, "The interpretations:write scope is required."),
            ("urn:imageinterpreter:problem:payload-too-large", "Payload Too Large", 413, "The image is larger than 10 MB."),
            ("urn:imageinterpreter:problem:unsupported-media-type", "Unsupported Media Type", 415, "The image must be JPEG, PNG, or WebP."),
            ("urn:imageinterpreter:problem:uninterpretable-image", "Unprocessable Content", 422, "The image could not be interpreted."),
            ("urn:imageinterpreter:problem:interpreter-unavailable", "Service Unavailable", 503, "The image interpreter is unavailable."),
            ("urn:imageinterpreter:problem:method-not-allowed", "Method Not Allowed", 405, "POST is the only method on this resource."),
            ("urn:imageinterpreter:problem:not-found", "Not Found", 404, "No interpretation resource is stored."),
        };

        var methods = typeof(ProblemCatalog)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.ReturnType == typeof(ProblemDetails) && HasInstanceParameter(method))
            .ToArray();

        Assert.Equal(expected.Length, methods.Length);

        var produced = new List<string>();
        foreach (var method in methods)
        {
            var problem = Assert.IsType<ProblemDetails>(method.Invoke(null, [instance]));
            var match = Assert.Single(expected, row => row.Detail == problem.Detail);
            Assert.Equal(match.Type, problem.Type);
            Assert.Equal(match.Title, problem.Title);
            Assert.Equal(match.Status, problem.Status);
            Assert.Equal(match.Detail, problem.Detail);
            produced.Add(problem.Detail!);
        }

        Assert.Equal(
            expected.Select(row => row.Detail).OrderBy(detail => detail, StringComparer.Ordinal),
            produced.OrderBy(detail => detail, StringComparer.Ordinal));
    }

    private static bool HasInstanceParameter(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
    }
}
