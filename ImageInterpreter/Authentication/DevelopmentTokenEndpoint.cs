using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace ImageInterpreter.Authentication;

internal sealed record DevelopmentAccessToken(
    [property: JsonPropertyName("tokenType")] string TokenType,
    [property: JsonPropertyName("accessToken")] string AccessToken);

internal static class DevelopmentTokenEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dev/token", Issue)
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static IResult Issue(
        DevelopmentSigningKey signingKey,
        IConfiguration configuration,
        string? scope)
    {
        var settings = AuthenticationSettings.Read(configuration);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>
            {
                ["scope"] = scope ?? InterpretationsScope.Write,
            },
        };

        return Results.Json(new DevelopmentAccessToken("Bearer", signingKey.CreateToken(descriptor)));
    }
}
