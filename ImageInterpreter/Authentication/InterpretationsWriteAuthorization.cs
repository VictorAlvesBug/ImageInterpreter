using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageInterpreter.Authentication;

internal static class InterpretationsPolicies
{
    public const string Write = "InterpretationsWrite";
}

internal static class InterpretationsScope
{
    public const string Write = "interpretations:write";

    public static bool IncludesWrite(string scope)
    {
        foreach (var token in scope.Split(' '))
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (token.Equals(Write, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal readonly record struct ScopeClaim(JsonValueKind Kind, string? Text);

internal static class ScopePayload
{
    public const string ItemKey = "ImageInterpreter.Authentication.Scope";

    public static Task Capture(TokenValidatedContext context)
    {
        if (context.SecurityToken is JsonWebToken jwt)
        {
            var json = Base64UrlEncoder.Decode(jwt.EncodedPayload);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("scope", out var scope))
            {
                context.HttpContext.Items[ItemKey] = scope.ValueKind == JsonValueKind.String
                    ? new ScopeClaim(JsonValueKind.String, scope.GetString())
                    : new ScopeClaim(scope.ValueKind, Text: null);
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class InterpretationsWriteRequirement : IAuthorizationRequirement;

internal sealed class InterpretationsWriteHandler : AuthorizationHandler<InterpretationsWriteRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InterpretationsWriteRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext
            && httpContext.Items[ScopePayload.ItemKey] is ScopeClaim scope
            && scope.Kind == JsonValueKind.String
            && scope.Text is not null
            && InterpretationsScope.IncludesWrite(scope.Text))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
