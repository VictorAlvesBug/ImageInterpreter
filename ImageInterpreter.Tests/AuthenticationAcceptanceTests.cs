using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ImageInterpreter.Tests;

[Collection(JwtHostCollection.Name)]
public class AuthenticationAcceptanceTests
{
    private readonly JwtHostFixture _host;

    public AuthenticationAcceptanceTests(JwtHostFixture host)
    {
        _host = host;
    }

    [Fact(DisplayName = "AC-009 a missing token is 401")]
    public async Task AC009_a_missing_token_is_401()
    {
        using var client = _host.CreateClient();
        using var request = InterpretationRequests.PostImage(authorization: null);
        using var response = await client.SendAsync(request);

        Assert.Equal("Bearer", AuthenticateHeader(response));
        await AssertProblemAsync(
            response,
            401,
            "urn:imageinterpreter:problem:unauthorized",
            "Unauthorized",
            "A bearer token is required.",
            "/api/v1/interpretations");
    }

    [Fact(DisplayName = "AC-010 a rejected bearer token is 401")]
    public async Task AC010_a_rejected_bearer_token_is_401()
    {
        using var other = System.Security.Cryptography.RSA.Create(2048);
        var token = _host.CreateToken(signingKey: other);
        using var client = _host.CreateClient();
        using var request = InterpretationRequests.PostImage($"Bearer {token}");
        using var response = await client.SendAsync(request);

        var authenticate = AuthenticateHeader(response);
        Assert.Contains("error=\"invalid_token\"", authenticate, StringComparison.Ordinal);
        await AssertProblemAsync(
            response,
            401,
            "urn:imageinterpreter:problem:unauthorized",
            "Unauthorized",
            "The bearer token was not accepted.",
            "/api/v1/interpretations");
    }

    [Fact(DisplayName = "AC-011 failed authentication stays 401 when the scope is also absent")]
    public async Task AC011_failed_authentication_stays_401_when_the_scope_is_also_absent()
    {
        var token = _host.CreateToken(
            scope: "images:read",
            expires: DateTime.UtcNow.AddMinutes(-30));
        using var client = _host.CreateClient();
        using var request = InterpretationRequests.PostImage($"Bearer {token}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "urn:imageinterpreter:problem:unauthorized",
            document.RootElement.GetProperty("type").GetString());
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact(DisplayName = "AC-012 an authentic token without the scope is 403")]
    public async Task AC012_an_authentic_token_without_the_scope_is_403()
    {
        var token = _host.CreateToken(scope: "images:read");
        using var client = _host.CreateClient();
        using var request = InterpretationRequests.PostImage($"Bearer {token}");
        using var response = await client.SendAsync(request);

        var authenticate = AuthenticateHeader(response);
        Assert.Contains("error=\"insufficient_scope\"", authenticate, StringComparison.Ordinal);
        Assert.Contains("scope=\"interpretations:write\"", authenticate, StringComparison.Ordinal);
        await AssertProblemAsync(
            response,
            403,
            "urn:imageinterpreter:problem:forbidden",
            "Forbidden",
            "The interpretations:write scope is required.",
            "/api/v1/interpretations");
    }

    [Fact(DisplayName = "AC-034 401 challenges use the Bearer scheme")]
    public async Task AC034_401_challenges_use_the_bearer_scheme()
    {
        using var client = _host.CreateClient();

        using (var missing = InterpretationRequests.PostImage(authorization: null))
        using (var missingResponse = await client.SendAsync(missing))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, missingResponse.StatusCode);
            Assert.Equal("Bearer", AuthenticateHeader(missingResponse));
        }

        using var other = System.Security.Cryptography.RSA.Create(2048);
        var token = _host.CreateToken(signingKey: other);
        using var rejected = InterpretationRequests.PostImage($"Bearer {token}");
        using var rejectedResponse = await client.SendAsync(rejected);
        var authenticate = AuthenticateHeader(rejectedResponse);
        Assert.Equal(HttpStatusCode.Unauthorized, rejectedResponse.StatusCode);
        Assert.Contains("Bearer", authenticate, StringComparison.Ordinal);
        Assert.Contains("error=\"invalid_token\"", authenticate, StringComparison.Ordinal);
    }

    private static string AuthenticateHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("WWW-Authenticate", out var values))
        {
            return string.Empty;
        }

        return string.Join(", ", values);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int status,
        string type,
        string title,
        string detail,
        string instance)
    {
        Assert.Equal(status, (int)response.StatusCode);

        Assert.NotNull(response.Content.Headers.ContentType);
        var mediaType = MediaTypeHeaderValue.Parse(response.Content.Headers.ContentType.ToString());
        Assert.Equal("application/problem+json", mediaType.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
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
}
