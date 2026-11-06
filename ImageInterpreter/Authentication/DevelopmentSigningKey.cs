using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ImageInterpreter.Authentication;

internal sealed class DevelopmentSigningKey : IDisposable
{
    private readonly RSA _rsa;
    private readonly object _gate = new();

    public DevelopmentSigningKey(RSA rsa)
    {
        _rsa = rsa;
    }

    public string ExportPublicKeyPem() => _rsa.ExportSubjectPublicKeyInfoPem();

    public string CreateToken(SecurityTokenDescriptor descriptor)
    {
        lock (_gate)
        {
            descriptor.SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa),
                SecurityAlgorithms.RsaSha256);
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }
    }

    public void Dispose() => _rsa.Dispose();
}
