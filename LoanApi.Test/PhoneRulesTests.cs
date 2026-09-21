using Application.Features.Customers;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class PhoneRulesTests
    {
        [TestCase("809-555-0100", "+18095550100")]
        [TestCase("(809) 555-0100", "+18095550100")]
        [TestCase("809.555.0100", "+18095550100")]
        [TestCase("8095550100", "+18095550100")]
        [TestCase("1-809-555-0100", "+18095550100")]
        [TestCase("18095550100", "+18095550100")]
        [TestCase("+1 809 555 0100", "+18095550100")]
        [TestCase("  +1 (809) 555-0100  ", "+18095550100")]
        [TestCase("+57 300 123 4567", "+573001234567")]
        [TestCase("+34 612 34 56 78", "+34612345678")]
        [TestCase("+52 55 1234 5678", "+525512345678")]
        public void Normalize_GivesTheCanonicalInternationalForm(string typed, string expected)
        {
            PhoneRules.Normalize(typed).Should().Be(expected);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Normalize_OfNothing_IsNull(string? typed)
        {
            PhoneRules.Normalize(typed).Should().BeNull();
        }

        [TestCase("abc")]
        [TestCase("809-555-01ab")]
        [TestCase("8095550100 ext 5")]
        [TestCase("12345")]
        [TestCase("+")]
        [TestCase("809-555")]
        public void Normalize_KeepsWhatItCannotReadSoValidationReportsIt(string typed)
        {
            PhoneRules.IsValid(PhoneRules.Normalize(typed)).Should().BeFalse();
        }

        [TestCase("+18095550100", true)]
        [TestCase("+573001234567", true)]
        [TestCase("+1234567", true, TestName = "Shortest allowed")]
        [TestCase("+123456789012345", true, TestName = "Longest allowed (15 digits)")]
        [TestCase("+1234567890123456", false, TestName = "Too long (16 digits)")]
        [TestCase("+123456", false, TestName = "Too short")]
        [TestCase("+0123456789", false, TestName = "A country code never starts with 0")]
        [TestCase("18095550100", false, TestName = "Missing the plus")]
        [TestCase("+1 809 555 0100", false, TestName = "Not canonical")]
        [TestCase(null, false)]
        public void IsValid_AcceptsOnlyTheCanonicalForm(string? canonical, bool expected)
        {
            PhoneRules.IsValid(canonical).Should().Be(expected);
        }

        [Test]
        public void Digits_KeepsOnlyAsciiDigits()
        {
            PhoneRules.Digits("+1 (809) 555-0100 ٣").Should().Be("18095550100");
        }
    }
}
