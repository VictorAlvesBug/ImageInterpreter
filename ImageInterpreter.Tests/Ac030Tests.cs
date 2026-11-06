using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImageInterpreter.Tests;

public class Ac030Tests
{
    private const string Id = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
    private const string CollectionPath = "/api/v1/interpretations";

    [Fact(DisplayName = "AC-030 the id cannot be fetched later")]
    public async Task AC030_the_id_cannot_be_fetched_later()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

        var idPath = $"{CollectionPath}/{Id}";

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete, HttpMethod.Head })
        {
            var response = await SendAsync(client, factory, method, CollectionPath);
            AssertProblem(
                response,
                405,
                "urn:imageinterpreter:problem:method-not-allowed",
                "Method Not Allowed",
                "POST is the only method on this resource.",
                CollectionPath,
                "POST");
        }

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete, HttpMethod.Head })
        {
            var response = await SendAsync(client, factory, method, idPath);
            AssertNotFound(response, idPath);
        }

        var withQuery = await SendAsync(client, factory, HttpMethod.Get, $"{idPath}?ignored=1");
        AssertNotFound(withQuery, idPath);

        var malformedId = $"{CollectionPath}/not-a-uuid";
        var malformed = await SendAsync(client, factory, HttpMethod.Get, malformedId);
        AssertNotFound(malformed, malformedId);
    }

    private static void AssertNotFound(ObservedResponse response, string instance) =>
        AssertProblem(
            response,
            404,
            "urn:imageinterpreter:problem:not-found",
            "Not Found",
            "No interpretation resource is stored.",
            instance,
            allow: null);

    private static void AssertProblem(
        ObservedResponse response,
        int status,
        string type,
        string title,
        string detail,
        string instance,
        string? allow)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(allow, response.Allow);

        Assert.NotNull(response.ContentType);
        var mediaType = MediaTypeHeaderValue.Parse(response.ContentType);
        Assert.Equal("application/problem+json", mediaType.MediaType);

        using var document = JsonDocument.Parse(response.Body);
        var root = document.RootElement;
        var names = root.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["detail", "instance", "status", "title", "type"], names);
        Assert.Equal(type, root.GetProperty("type").GetString());
        Assert.Equal(title, root.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("status").ValueKind);
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.Equal(detail, root.GetProperty("detail").GetString());
        Assert.Equal(instance, root.GetProperty("instance").GetString());
    }

    private static async Task<ObservedResponse> SendAsync(
        HttpClient client,
        WebApplicationFactory<Program> factory,
        HttpMethod method,
        string requestUri)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        using var response = await client.SendAsync(request);

        var statusCode = (int)response.StatusCode;
        var allow = ReadAllow(response);
        var contentType = response.Content.Headers.ContentType?.ToString();
        var body = await response.Content.ReadAsStringAsync();

        if (method == HttpMethod.Head && body.Length == 0)
        {
            var serverResponse = await SendThroughTestServerAsync(factory, method, requestUri);
            Assert.Equal(statusCode, serverResponse.StatusCode);
            allow ??= serverResponse.Allow;
            contentType ??= serverResponse.ContentType;
            body = serverResponse.Body;
        }

        return new ObservedResponse(statusCode, allow, contentType, body);
    }

    private static async Task<ObservedResponse> SendThroughTestServerAsync(
        WebApplicationFactory<Program> factory,
        HttpMethod method,
        string requestUri)
    {
        var path = requestUri;
        var query = string.Empty;
        var separator = requestUri.IndexOf('?', StringComparison.Ordinal);
        if (separator >= 0)
        {
            path = requestUri[..separator];
            query = requestUri[separator..];
        }

        var context = await factory.Server.SendAsync(httpContext =>
        {
            httpContext.Request.Method = method.Method;
            httpContext.Request.Path = path;
            if (query.Length > 0)
            {
                httpContext.Request.QueryString = new QueryString(query);
            }
        });

        var allow = context.Response.Headers.TryGetValue("Allow", out var allowHeader)
            ? allowHeader.ToString()
            : null;
        if (string.IsNullOrEmpty(allow))
        {
            allow = null;
        }

        return new ObservedResponse(
            context.Response.StatusCode,
            allow,
            context.Response.ContentType,
            await ReadBodyAsync(context.Response.Body));
    }

    private static string? ReadAllow(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Allow", out var allowValues))
        {
            return string.Join(", ", allowValues);
        }

        if (response.Content is not null && response.Content.Headers.TryGetValues("Allow", out var contentAllow))
        {
            return string.Join(", ", contentAllow);
        }

        return null;
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        if (body.CanSeek)
        {
            body.Position = 0;
        }

        using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed record ObservedResponse(int StatusCode, string? Allow, string? ContentType, string Body);
}
