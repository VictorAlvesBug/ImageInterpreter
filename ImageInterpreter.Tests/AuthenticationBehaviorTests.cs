using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ImageInterpreter.Tests;

[Collection(JwtHostCollection.Name)]
public class AuthenticationBehaviorTests
{
    private readonly JwtHostFixture _host;

    public AuthenticationBehaviorTests(JwtHostFixture host)
    {
        _host = host;
    }

    [Fact]
    public async Task Wrong_issuer_audience_expired_empty_bearer_and_basic_scheme_stay_401()
    {
        var cases = new[]
        {
            "Bearer " + _host.CreateToken(issuer: "https://other.example"),
            "Bearer " + _host.CreateToken(audience: "other-audience"),
            "Bearer " + _host.CreateToken(expires: DateTime.UtcNow.AddMinutes(-30)),
            "Bearer",
            "Basic abc",
        };

        using var client = _host.CreateClient();
        foreach (var authorization in cases)
        {
            using var request = InterpretationRequests.PostImage(authorization);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Write_scope_case_empty_missing_and_json_array_stay_403()
    {
        var tokens = new[]
        {
            _host.CreateToken(scope: "interpretations:WRITE"),
            _host.CreateToken(scope: ""),
            _host.CreateToken(includeScope: false),
            _host.CreateTokenWithRawScope("""["interpretations:write"]"""),
        };

        using var client = _host.CreateClient();
        foreach (var token in tokens)
        {
            using var request = InterpretationRequests.PostImage($"Bearer {token}");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Spaced_write_scope_and_bearer_scheme_pass_authorization()
    {
        var spaced = _host.CreateToken(scope: "images:read  interpretations:write");
        var canonical = _host.CreateToken();

        using var client = _host.CreateClient();
        foreach (var authorization in new[] { $"Bearer {spaced}", $"bearer {canonical}" })
        {
            using var request = InterpretationRequests.PostImage(authorization);
            using var response = await client.SendAsync(request);
            var status = (int)response.StatusCode;
            Assert.NotEqual(401, status);
            Assert.NotEqual(403, status);
        }
    }

    [Fact]
    public void Host_without_a_public_key_does_not_start()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = JwtHostFixture.Issuer,
            ["Authentication:Audience"] = JwtHostFixture.Audience,
            ["Authentication:ClockSkew"] = "00:00:00",
        });

        var description = StartFailure(factory);
        Assert.Contains(nameof(InvalidOperationException), description, StringComparison.Ordinal);
        Assert.Contains("JwksUri", description, StringComparison.Ordinal);
        Assert.Contains("PublicKeyPem", description, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_with_JwksUri_and_PublicKeyPem_does_not_start()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportSubjectPublicKeyInfoPem();
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = JwtHostFixture.Issuer,
            ["Authentication:Audience"] = JwtHostFixture.Audience,
            ["Authentication:ClockSkew"] = "00:00:00",
            ["Authentication:JwksUri"] = "https://issuer.example/.well-known/jwks.json",
            ["Authentication:PublicKeyPem"] = pem,
        });

        var description = StartFailure(factory);
        Assert.Contains(nameof(InvalidOperationException), description, StringComparison.Ordinal);
        Assert.Contains("exactly one of JwksUri or PublicKeyPem", description, StringComparison.Ordinal);
        Assert.DoesNotContain(pem, description, StringComparison.Ordinal);
        Assert.DoesNotContain("BEGIN PUBLIC KEY", description, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(settings);
            });
        });

    private static string StartFailure(WebApplicationFactory<Program> factory)
    {
        try
        {
            using var client = factory.CreateClient();
        }
        catch (Exception exception)
        {
            return Describe(exception);
        }

        throw new Xunit.Sdk.XunitException("The host started.");
    }

    private static string Describe(Exception exception)
    {
        var builder = new StringBuilder();
        Append(builder, exception, 0);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, Exception exception, int depth)
    {
        if (depth > 8)
        {
            return;
        }

        builder.AppendLine(exception.GetType().FullName);
        builder.AppendLine(exception.Message);
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                Append(builder, inner, depth + 1);
            }

            return;
        }

        if (exception.InnerException is not null)
        {
            Append(builder, exception.InnerException, depth + 1);
        }
    }
}
