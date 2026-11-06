using System.Globalization;
using System.Security.Cryptography;

namespace ImageInterpreter.Authentication;

internal sealed class AuthenticationSettings
{
    private AuthenticationSettings(
        string issuer,
        string audience,
        TimeSpan clockSkew,
        string? jwksUri,
        RSAParameters? publicKey)
    {
        Issuer = issuer;
        Audience = audience;
        ClockSkew = clockSkew;
        JwksUri = jwksUri;
        PublicKey = publicKey;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public TimeSpan ClockSkew { get; }

    public string? JwksUri { get; }

    public RSAParameters? PublicKey { get; }

    public static AuthenticationSettings Read(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication");
        var issuer = RequiredText(section["Issuer"], "Authentication:Issuer");
        var audience = RequiredText(section["Audience"], "Authentication:Audience");
        var clockSkew = ReadClockSkew(section["ClockSkew"]);
        var jwksUri = BlankToNull(section["JwksUri"]);
        var publicKeyPem = BlankToNull(section["PublicKeyPem"]);

        if ((jwksUri is null) == (publicKeyPem is null))
        {
            throw new InvalidOperationException(
                "Authentication requires exactly one of JwksUri or PublicKeyPem.");
        }

        if (jwksUri is not null)
        {
            if (!Uri.TryCreate(jwksUri, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "Authentication:JwksUri must be an absolute http or https URI.");
            }

            return new AuthenticationSettings(issuer, audience, clockSkew, jwksUri, publicKey: null);
        }

        return new AuthenticationSettings(
            issuer,
            audience,
            clockSkew,
            jwksUri: null,
            ReadPublicKey(publicKeyPem!));
    }

    private static string RequiredText(string? value, string setting)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{setting} is required.");
        }

        return value;
    }

    private static TimeSpan ReadClockSkew(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Authentication:ClockSkew is required.");
        }

        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var clockSkew))
        {
            throw new InvalidOperationException("Authentication:ClockSkew must be a TimeSpan.");
        }

        if (clockSkew < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Authentication:ClockSkew must be zero or greater.");
        }

        return clockSkew;
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static RSAParameters ReadPublicKey(string pem)
    {
        if (HasPrivateKeyLabel(pem))
        {
            throw new InvalidOperationException(
                "Authentication:PublicKeyPem must be an RSA public key in SPKI form. A private key is not accepted.");
        }

        if (!HasSpkiLabel(pem))
        {
            throw new InvalidOperationException(
                "Authentication:PublicKeyPem must be an RSA public key in SPKI form.");
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            try
            {
                _ = rsa.ExportParameters(includePrivateParameters: true);
                throw new InvalidOperationException(
                    "Authentication:PublicKeyPem must be an RSA public key in SPKI form. A private key is not accepted.");
            }
            catch (CryptographicException)
            {
                // A public key cannot export private parameters.
            }

            return rsa.ExportParameters(includePrivateParameters: false);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidOperationException(
                "Authentication:PublicKeyPem must be an RSA public key in SPKI form.");
        }
    }

    private static bool HasPrivateKeyLabel(string pem)
    {
        foreach (var line in pem.Split('\n'))
        {
            var trimmed = line.Trim().TrimEnd('\r');
            if (trimmed.StartsWith("-----BEGIN ", StringComparison.Ordinal)
                && trimmed.Contains("PRIVATE", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSpkiLabel(string pem)
    {
        foreach (var line in pem.Split('\n'))
        {
            var trimmed = line.Trim().TrimEnd('\r');
            if (trimmed.Equals("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
