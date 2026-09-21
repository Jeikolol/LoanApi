using System.Text.RegularExpressions;

namespace Application.Features.Customers
{
    /// <summary>Field checks shared by creating and editing a customer.</summary>
    public static partial class CustomerFieldRules
    {
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailPattern();

        public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static void Required(List<string> errors, string name, string value, int maxLength)
        {
            if (value.Length == 0) errors.Add($"{name} is required.");
            else if (value.Length > maxLength) errors.Add($"{name} cannot exceed {maxLength} characters.");
        }

        public static void CheckPhone(List<string> errors, string name, string? value, bool required)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (required) errors.Add($"{name} is required.");
                return;
            }

            if (!PhoneRules.IsValid(PhoneRules.Normalize(value))) errors.Add($"{name} is not valid: use the international format with the country code, for example +18095550100.");
        }

        public static void CheckEmail(List<string> errors, string? email)
        {
            if (email is not null && (email.Length > 100 || !EmailPattern().IsMatch(email)))
            {
                errors.Add("Email is not valid.");
            }
        }

        public static void CheckMoney(List<string> errors, string name, decimal amount)
        {
            if (amount < 0) errors.Add($"{name} cannot be negative.");
            else if (decimal.Round(amount, 2) != amount) errors.Add($"{name} cannot have more than two decimals.");
        }
    }
}
