using System.Security.Cryptography;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateTemporaryPassword(int length = 12)
    {
        // Usar PRNG criptográfico: dos invocaciones simultáneas NO pueden colisionar.
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "@#$!%&*-_";

        var chars = new char[length];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        var all = upper + lower + digits + special;
        for (int i = 4; i < length; i++)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // Shuffle Fisher-Yates con CSPRNG.
        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    public bool IsPasswordPolicyCompliant(string password, out List<string> violations)
    {
        violations = [];
        if (password.Length < 8) violations.Add("La contraseña debe tener al menos 8 caracteres.");
        if (!password.Any(char.IsUpper)) violations.Add("Debe contener al menos una letra mayúscula.");
        if (!password.Any(char.IsLower)) violations.Add("Debe contener al menos una letra minúscula.");
        if (!password.Any(char.IsDigit)) violations.Add("Debe contener al menos un número.");
        if (!password.Any(c => !char.IsLetterOrDigit(c))) violations.Add("Debe contener al menos un carácter especial.");
        return violations.Count == 0;
    }
}
