using Domain.Enums;
using System.Text.RegularExpressions;

namespace Application.Features.Customers
{
    public static partial class IdentificationRules
    {
        public const string CedulaCode = "CEDULA";
        public const string RncCode = "RNC";
        public const string PassportCode = "PASSPORT";
        public const string DominicanRepublicIso2 = "DO";

        [GeneratedRegex(@"^\d{11}$")]
        private static partial Regex CedulaDigits();

        [GeneratedRegex(@"^\d{9}$")]
        private static partial Regex RncDigits();

        [GeneratedRegex(@"^[A-Z]{2}\d{6,12}$")]
        private static partial Regex PassportPattern();

        /// <summary>
        /// Which documents a kind of customer may use: a legal person is identified by RNC only; an individual by cédula
        /// or passport, and a cédula belongs to Dominican nationals, so a foreigner must present a passport.
        /// </summary>
        public static IReadOnlyList<string> ValidateCombination(CustomerType customerType, string typeCode, string? nationalityIso2)
        {
            List<string> errors = [];

            if (customerType == CustomerType.LegalPerson)
            {
                if (typeCode != RncCode) errors.Add("A legal person must be identified by RNC.");
                return errors;
            }

            if (typeCode == RncCode)
            {
                errors.Add("An RNC can only be used for a legal person.");
            }
            else if (typeCode is not (CedulaCode or PassportCode))
            {
                errors.Add("A physical person can only be identified by cédula or passport.");
            }
            else if (typeCode == CedulaCode
                && nationalityIso2 is not null
                && !string.Equals(nationalityIso2, DominicanRepublicIso2, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("A cédula can only be used by Dominican nationals.");
            }

            return errors;
        }

        /// <summary>
        /// Returns the identification in its canonical form (cédula 999-9999999-9, RNC 999-99999-9, passport AA999999999999)
        /// so that the same document typed in different ways is stored, and de-duplicated, as a single value.
        /// Other document types are only trimmed.
        /// </summary>
        public static string Normalize(string typeCode, string identification)
        {
            string trimmed = identification.Trim();
            string digits = new(trimmed.Where(char.IsDigit).ToArray());

            return typeCode switch
            {
                CedulaCode when CedulaDigits().IsMatch(digits) => $"{digits[..3]}-{digits[3..10]}-{digits[10..]}",
                RncCode when RncDigits().IsMatch(digits) => $"{digits[..3]}-{digits[3..8]}-{digits[8..]}",
                PassportCode => new string(trimmed.Where(c => c is not ' ' and not '-').ToArray()).ToUpperInvariant(),
                _ => trimmed,
            };
        }

        /// <summary>Returns the problem with the (already normalized) identification, or null when it is valid.</summary>
        public static string? Validate(string typeCode, string normalized, string? nationalityIso2)
        {
            switch (typeCode)
            {
                case CedulaCode when !Regex.IsMatch(normalized, @"^\d{3}-\d{7}-\d$"):
                    return "Identification must be a cédula with 11 digits (999-9999999-9).";
                case RncCode when !Regex.IsMatch(normalized, @"^\d{3}-\d{5}-\d$"):
                    return "Identification must be an RNC with 9 digits (999-99999-9).";
                case PassportCode when !PassportPattern().IsMatch(normalized):
                    return "Identification must be a passport: two letters for the country code followed by 6 to 12 digits, without spaces or hyphens (AA999999999999).";
                case PassportCode when nationalityIso2 is not null
                    && !normalized.StartsWith(nationalityIso2, StringComparison.OrdinalIgnoreCase):
                    return $"The passport must start with the ISO country code of the nationality ({nationalityIso2}).";
                default:
                    return null;
            }
        }
    }
}
