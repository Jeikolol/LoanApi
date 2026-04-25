using System.Text.RegularExpressions;

namespace Application.Services
{
    public static class IdentificationValidator
    {
        // Cédula format: ###-#######-# (e.g., 001-2345678-9)
        private static readonly string CedulaPattern = @"^\d{3}-\d{7}-\d{1}$";

        // Passport format: 2-letter country code + 9-11 digits
        private static readonly string PassportPattern = @"^[A-Z]{2}\d{9,11}$";

        public static bool IsValidDominicanCedula(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return false;

            return Regex.IsMatch(cedula, CedulaPattern);
        }

        public static bool IsValidPassport(string passport)
        {
            if (string.IsNullOrWhiteSpace(passport))
                return false;

            return Regex.IsMatch(passport, PassportPattern);
        }

        public static bool IsValidTaxId(string taxId)
        {
            // RNC format: ###-#####-# (similar to Cédula)
            var rncPattern = @"^\d{3}-\d{5}-\d{1}$";
            if (string.IsNullOrWhiteSpace(taxId))
                return false;

            return Regex.IsMatch(taxId, rncPattern);
        }

        public static ValidationResult ValidateIdentification(string identification, string identificationType)
        {
            if (string.IsNullOrWhiteSpace(identification))
                return new ValidationResult { IsValid = false, Message = "Identification cannot be empty" };

            return identificationType?.ToLower() switch
            {
                "cedula" or "cédula" => IsValidDominicanCedula(identification)
                    ? new ValidationResult { IsValid = true, Message = "Valid Cédula format" }
                    : new ValidationResult { IsValid = false, Message = "Invalid Cédula format. Expected: ###-#######-#" },

                "passport" => IsValidPassport(identification)
                    ? new ValidationResult { IsValid = true, Message = "Valid Passport format" }
                    : new ValidationResult { IsValid = false, Message = "Invalid Passport format. Expected: 2 letters + 9-11 digits" },

                "taxid" or "rnc" => IsValidTaxId(identification)
                    ? new ValidationResult { IsValid = true, Message = "Valid Tax ID format" }
                    : new ValidationResult { IsValid = false, Message = "Invalid Tax ID format. Expected: ###-#####-#" },

                _ => new ValidationResult { IsValid = false, Message = $"Unknown identification type: {identificationType}" }
            };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
