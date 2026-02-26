using System.Text.RegularExpressions;

namespace LinkVault.Shared.Validators;

public static partial class PasswordValidator
{
    public static (bool IsValid, List<string> Errors) Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return (false, errors);
        }

        if (password.Length < 12)
            errors.Add("Password must be at least 12 characters long.");

        if (!HasUppercase(password))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!HasLowercase(password))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!HasDigit(password))
            errors.Add("Password must contain at least one number.");

        if (!HasSpecialChar(password))
            errors.Add("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>).");

        return (errors.Count == 0, errors);
    }

    private static bool HasUppercase(string password) => password.Any(char.IsUpper);
    private static bool HasLowercase(string password) => password.Any(char.IsLower);
    private static bool HasDigit(string password) => password.Any(char.IsDigit);
    private static bool HasSpecialChar(string password) => password.Any(c => "!@#$%^&*(),.?\":{}|<>".Contains(c));
}
