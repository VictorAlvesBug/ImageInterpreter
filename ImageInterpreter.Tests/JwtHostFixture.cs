using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageInterpreter.Tests;

[CollectionDefinition(JwtHostCollection.Name)]
public sealed class JwtHostCollection : ICollectionFixture<JwtHostFixture>
{
    public const string Name = "Jwt host";
}

public sealed class JwtHostFixture : IDisposable
{
    public const string Issuer = "https://issuer.example";
    public const string Audience = "imageinterpreter";

    private readonly RSA _rsa = RSA.Create(2048);

    public JwtHostFixture()
    {
        var publicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Issuer"] = Issuer,
                    ["Authentication:Audience"] = Audience,
                    ["Authentication:ClockSkew"] = "00:00:00",
                    ["Authentication:PublicKeyPem"] = publicKeyPem,
                });
            });
        });
    }

    public WebApplicationFactory<Program> Factory { get; }

    public HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

    public string CreateToken(
        object? scope = null,
        bool includeScope = true,
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null,
        RSA? signingKey = null)
    {
        var claims = new Dictionary<string, object>();
        if (includeScope)
        {
            claims["scope"] = scope ?? InterpretationsWriteScope;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingKey ?? _rsa),
                SecurityAlgorithms.RsaSha256),
            Claims = claims,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public string CreateTokenWithRawScope(string scopeJson, DateTime? expires = null)
    {
        var now = DateTimeOffset.UtcNow;
        var exp = new DateTimeOffset(expires ?? DateTime.UtcNow.AddMinutes(5), TimeSpan.Zero);
        var payload = new Dictionary<string, object>
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["nbf"] = now.AddMinutes(-5).ToUnixTimeSeconds(),
            ["exp"] = exp.ToUnixTimeSeconds(),
        };

        var header = Base64UrlEncoder.Encode("""{"alg":"RS256","typ":"JWT"}""");
        var payloadJson = JsonSerializer.Serialize(payload);
        payloadJson = payloadJson[..^1] + ",\"scope\":" + scopeJson + "}";
        var encodedPayload = Base64UrlEncoder.Encode(payloadJson);
        var signingInput = System.Text.Encoding.ASCII.GetBytes($"{header}.{encodedPayload}");
        var signature = _rsa.SignData(signingInput, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{header}.{encodedPayload}.{Base64UrlEncoder.Encode(signature)}";
    }

    public const string InterpretationsWriteScope = "interpretations:write";

    public void Dispose()
    {
        Factory.Dispose();
        _rsa.Dispose();
    }
}

internal static class InterpretationRequests
{
    public static HttpRequestMessage PostImage(string? authorization)
    {
        var file = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var body = new MultipartFormDataContent();
        body.Add(file, "image", "photo.jpg");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/interpretations")
        {
            Content = body,
        };

        if (authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        return request;
    }
}
