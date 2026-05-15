using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private static readonly string[] SpecialChars = ["@", "#", "$", "!", "%", "&", "*", "-", "_", "+", "=", "?"];

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateTemporaryPassword(int length = 12)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "@#$!%&*-_";

        var rng = new Random();
        var chars = new List<char>
        {
            upper[rng.Next(upper.Length)],
            lower[rng.Next(lower.Length)],
            digits[rng.Next(digits.Length)],
            special[rng.Next(special.Length)]
        };

        var all = upper + lower + digits + special;
        for (int i = chars.Count; i < length; i++)
            chars.Add(all[rng.Next(all.Length)]);

        return new string(chars.OrderBy(_ => rng.Next()).ToArray());
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
