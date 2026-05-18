using Microsoft.AspNetCore.DataProtection;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

// Wrapper sobre Data Protection API.
// Los secretos cifrados se prefijan con "enc:" para distinguirlos de los planos heredados
// (migración suave: si no tiene prefijo, asumimos plano y lo devolvemos tal cual).
public class SecretProtector : ISecretProtector
{
    private const string Prefix = "enc:v1:";
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("RepagroSuite.Secrets.v1");
    }

    public string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;
        return Prefix + _protector.Protect(plaintext);
    }

    public string? Unprotect(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return null;
        if (!ciphertext.StartsWith(Prefix)) return ciphertext; // valor legado en plano
        try { return _protector.Unprotect(ciphertext[Prefix.Length..]); }
        catch { return null; }
    }
}
