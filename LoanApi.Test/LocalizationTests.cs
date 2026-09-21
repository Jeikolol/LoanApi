using System.Text.RegularExpressions;
using Application.Localization;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class LocalizationTests
    {
        private static Dictionary<string, string> LoadSpanish()
        {
            string dir = AppContext.BaseDirectory;
            while (dir is not null && !File.Exists(Path.Combine(dir, "LoanApi", "Localization", "es", "LoanApi.po")))
            {
                dir = Path.GetDirectoryName(dir)!;
            }

            string text = File.ReadAllText(Path.Combine(dir!, "LoanApi", "Localization", "es", "LoanApi.po"));
            Dictionary<string, string> entries = [];
            foreach (Match m in Regex.Matches(text, "^msgid \"(.+)\"\r?\nmsgstr \"(.*)\"", RegexOptions.Multiline))
            {
                entries.Add(m.Groups[1].Value, m.Groups[2].Value);
            }

            return entries;
        }

        private static string? Lookup(string template) => LoadSpanish().GetValueOrDefault(template);

        [Test]
        public void Split_ReplacesNumbersGuidsAndStatusWords()
        {
            var (template, values) = MessageTemplate.Split("Loan 3f2504e0-4f89-41d3-9a0c-0305e82c3301 has status Closed after 12 months (9000.50)");

            template.Should().Be("Loan {0} has status {1} after {2} months ({3})");
            values.Should().Equal("3f2504e0-4f89-41d3-9a0c-0305e82c3301", "Closed", "12", "9000.50");
        }

        [Test]
        public void Split_LeavesWordsThatMerelyContainDigitsOrStatuses()
        {
            MessageTemplate.Split("E.164 numbers, 3x the payment, ActiveLoans").Template.Should().Be("E.164 numbers, 3x the payment, ActiveLoans");
        }

        [Test]
        public void Translate_PutsTheValuesBackInTheTranslation()
        {
            string result = MessageTemplate.Translate("Customer 3f2504e0-4f89-41d3-9a0c-0305e82c3301 not found", Lookup);

            result.Should().Be("Cliente 3f2504e0-4f89-41d3-9a0c-0305e82c3301 no encontrado");
        }

        [Test]
        public void Translate_AlsoTranslatesTheStatusValues()
        {
            MessageTemplate.Translate("A loan with status FullyPaid cannot be approved.", Lookup)
                .Should().Be("Un préstamo con estado Pagado en su totalidad no puede ser aprobado.");
        }

        [Test]
        public void Translate_WithoutATranslation_KeepsTheOriginalMessage()
        {
            MessageTemplate.Translate("Something nobody translated 42", _ => null).Should().Be("Something nobody translated 42");
        }

        [TestCase("The file cannot exceed 10 MB.", "El archivo no puede exceder 10 MB.")]
        [TestCase("PrincipalAmount cannot exceed 100000000.", "El monto del préstamo no puede exceder 100000000.")]
        [TestCase("The customer is Suspended, not Active.", "El cliente está Suspendido, no Activo.")]
        [TestCase("Phone is not valid: use the international format with the country code, for example +18095550100.",
            "El teléfono no es válido: usa el formato internacional con el código del país, por ejemplo +18095550100.")]
        [TestCase("Invalid username or password.", "Usuario o contraseña incorrectos.")]
        public void RealMessages_AreTranslated(string english, string spanish)
        {
            MessageTemplate.Translate(english, Lookup).Should().Be(spanish);
        }

        [Test]
        public void EverySpanishEntry_KeepsTheSamePlaceholdersAsItsEnglishMessage()
        {
            foreach ((string english, string spanish) in LoadSpanish())
            {
                if (english.Length == 0) continue;

                string[] expected = Regex.Matches(english, @"\{\d\}").Select(m => m.Value).OrderBy(x => x).ToArray();
                string[] actual = Regex.Matches(spanish, @"\{\d\}").Select(m => m.Value).OrderBy(x => x).ToArray();

                actual.Should().Equal(expected, $"the translation of \"{english}\" must use the same placeholders");
            }
        }
    }
}
