using System.Text.RegularExpressions;

namespace Application.Features.Customers
{
    /// <summary>
    /// Phones are stored in one canonical international form, +18095550100 (a plus, the country code and the
    /// national number, digits only). Two spellings of the same number therefore always collide on the unique
    /// index, and screens format the number for its country.
    /// </summary>
    public static partial class PhoneRules
    {
        /// <summary>The home country's dial code: a national number typed without a country code is read as Dominican (+1).</summary>
        public const string HomeDialCode = "1";

        [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
        private static partial Regex E164Pattern();

        [GeneratedRegex(@"^[+0-9()\-\s.]+$")]
        private static partial Regex AllowedCharacters();

        public static string Digits(string value) => new(value.Where(char.IsAsciiDigit).ToArray());

        /// <summary>
        /// Canonical form of what a person or a client typed, or the input itself when it cannot be read as a phone
        /// (so that validation reports it). Returns null for an empty value.
        /// </summary>
        public static string? Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string trimmed = raw.Trim();
            if (!AllowedCharacters().IsMatch(trimmed)) return trimmed;

            string digits = Digits(trimmed);
            if (trimmed.StartsWith('+')) return digits.Length == 0 ? trimmed : "+" + digits;

            // Without a country code the number is national: ten digits (809-555-0100) or with the leading 1.
            if (digits.Length == 10) return "+" + HomeDialCode + digits;
            if (digits.Length == 11 && digits.StartsWith(HomeDialCode)) return "+" + digits;

            return trimmed;
        }

        public static bool IsValid(string? canonical) => canonical is not null && E164Pattern().IsMatch(canonical);
    }
}
