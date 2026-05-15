namespace RepagroSuite.Application.Common.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateTemporaryPassword(int length = 12);
    bool IsPasswordPolicyCompliant(string password, out List<string> violations);
}
