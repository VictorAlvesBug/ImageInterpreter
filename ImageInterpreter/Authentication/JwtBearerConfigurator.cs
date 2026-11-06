using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ImageInterpreter.Authentication;

internal static class AuthenticationRegistration
{
    public static void AddImageInterpreterAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IStartupFilter, AuthenticationStartupFilter>();
        services.AddSingleton<IAuthorizationHandler, InterpretationsWriteHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfiguration>((options, configuration) =>
            {
                Apply(options, AuthenticationSettings.Read(configuration));
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(InterpretationsPolicies.Write, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new InterpretationsWriteRequirement());
            });
        });
    }

    public static void Apply(JwtBearerOptions options, AuthenticationSettings settings)
    {
        options.MapInboundClaims = false;
        options.UseSecurityTokenValidators = false;
        options.SaveToken = false;
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = settings.ClockSkew,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        };

        if (settings.PublicKey is { } publicKey)
        {
            options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(publicKey);
            options.ConfigurationManager = null;
        }
        else
        {
            options.ConfigurationManager = CreateJwksManager(settings.JwksUri!);
        }

        options.Events.OnTokenValidated = ScopePayload.Capture;
        options.Events.OnChallenge = JwtProblemEvents.Challenge;
        options.Events.OnForbidden = JwtProblemEvents.Forbidden;
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> CreateJwksManager(string jwksUri)
    {
        var retriever = new HttpDocumentRetriever();
        if (Uri.TryCreate(jwksUri, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp)
        {
            retriever.RequireHttps = false;
        }

        return new ConfigurationManager<OpenIdConnectConfiguration>(
            jwksUri,
            new JwksDocumentRetriever(),
            retriever);
    }
}

internal sealed class AuthenticationStartupFilter : IStartupFilter
{
    private readonly IConfiguration _configuration;

    public AuthenticationStartupFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            AuthenticationSettings.Read(_configuration);
            next(app);
        };
    }
}
