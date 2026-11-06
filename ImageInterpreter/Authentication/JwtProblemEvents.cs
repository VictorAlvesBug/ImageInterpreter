using ImageInterpreter.Problems;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ImageInterpreter.Authentication;

internal static class BearerChallenge
{
    public const string Missing = "Bearer";

    public const string InvalidToken = "Bearer error=\"invalid_token\"";

    public const string InsufficientScope = "Bearer error=\"insufficient_scope\", scope=\"interpretations:write\"";

    public static bool IsBearerScheme(string? authorization)
    {
        if (string.IsNullOrEmpty(authorization))
        {
            return false;
        }

        var separator = authorization.IndexOf(' ');
        var scheme = separator < 0 ? authorization : authorization[..separator];
        return scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class JwtProblemEvents
{
    public static Task Challenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var httpContext = context.HttpContext;
        var presentedBearer = BearerChallenge.IsBearerScheme(httpContext.Request.Headers.Authorization.ToString());
        var instance = InstancePath(httpContext);
        var problem = presentedBearer
            ? ProblemCatalog.BearerTokenNotAccepted(instance)
            : ProblemCatalog.BearerTokenRequired(instance);

        httpContext.Response.Headers.WWWAuthenticate = presentedBearer
            ? BearerChallenge.InvalidToken
            : BearerChallenge.Missing;

        return ProblemJson.WriteAsync(httpContext, problem);
    }

    public static Task Forbidden(ForbiddenContext context)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.Headers.WWWAuthenticate = BearerChallenge.InsufficientScope;
        return ProblemJson.WriteAsync(
            httpContext,
            ProblemCatalog.ScopeRequired(InstancePath(httpContext)));
    }

    private static string InstancePath(HttpContext httpContext) =>
        httpContext.Request.Path.Value ?? string.Empty;
}
