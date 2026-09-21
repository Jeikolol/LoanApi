using Domain.Enums;
using System.Text.RegularExpressions;

namespace Application.Localization
{
    /// <summary>
    /// Turns an English message with its variable values ("Customer 3f2a... not found", "cannot exceed 50 characters")
    /// into the template that is looked up in the translation files ("Customer {0} not found") plus the values.
    /// </summary>
    public static partial class MessageTemplate
    {
        private const string NumberOrGuid = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|(?<![\w.])\d+(?:\.\d+)?(?![\w])";

        // Status names and loan action words are values too: "A loan with status Closed ..." shares one template
        // with every other status, and the value itself is translated ("Closed" -> "Cerrado").
        private static readonly string[] Words =
        [
            .. Enum.GetNames<LoanStatus>(), .. Enum.GetNames<PaymentStatus>(), .. Enum.GetNames<CustomerStatus>(),
            "approved", "disbursed", "cancelled", "closed",
        ];

        private static readonly Regex Variable = new(
            NumberOrGuid + @"|\b(?:" + string.Join("|", Words.Distinct()) + @")\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsWord(string value) => Words.Contains(value);

        public static (string Template, string[] Values) Split(string message)
        {
            List<string> values = [];
            string template = Variable.Replace(message, match =>
            {
                values.Add(match.Value);
                return "{" + (values.Count - 1) + "}";
            });

            return (template, [.. values]);
        }

        /// <summary>Puts the values back into a (translated) template, ignoring placeholders it does not use.</summary>
        public static string Fill(string template, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                template = template.Replace("{" + i + "}", values[i]);
            }

            return template;
        }

        /// <summary>Translates through <paramref name="lookup"/> (null when there is no translation); falls back to the original.</summary>
        public static string Translate(string message, Func<string, string?> lookup)
        {
            (string template, string[] values) = Split(message);
            string? translated = lookup(template);

            if (translated is null) return message;

            for (int i = 0; i < values.Length; i++)
            {
                if (IsWord(values[i])) values[i] = lookup(values[i]) ?? values[i];
            }

            return Fill(translated, values);
        }
    }
}
