namespace RepagroSuite.Application.Common.Interfaces;

// Cifra/descifra secretos guardados en BD (p. ej. EMAIL.SMTP_PASSWORD).
// Implementado con Data Protection API en Infrastructure.
public interface ISecretProtector
{
    string Protect(string? plaintext);
    string? Unprotect(string? ciphertext);
}
