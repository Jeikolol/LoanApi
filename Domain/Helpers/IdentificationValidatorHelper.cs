using System.Text.RegularExpressions;

namespace Domain.Helpers
{
    public static class IdentificationValidatorHelper
    {
        // Cédula format: ###-#######-# (e.g., 001-2345678-9)
        public static bool IsValidDominicanCedula(string cedula)
        {
            var pattern = @"^\d{3}-\d{7}-\d{1}$";
            return Regex.IsMatch(cedula, pattern);
        }

        // Passport format: 2-letter country code + 9-11 digits
        public static bool IsValidPassport(string passport)
        {
            var pattern = @"^[A-Z]{2}\d{9,11}$";
            return Regex.IsMatch(passport, pattern);
        }
    }
}
