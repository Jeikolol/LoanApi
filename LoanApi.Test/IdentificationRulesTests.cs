using Application.Features.Customers;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class IdentificationRulesTests
    {
        [TestCase("CEDULA", "40212345678", "402-1234567-8")]
        [TestCase("CEDULA", "402-1234567-8", "402-1234567-8")]
        [TestCase("CEDULA", " 402 1234567 8 ", "402-1234567-8")]
        [TestCase("RNC", "131123456", "131-12345-6")]
        [TestCase("RNC", "131-12345-6", "131-12345-6")]
        [TestCase("PASSPORT", "us 1234-5678", "US12345678")]
        [TestCase("PASSPORT", "ES123456789012", "ES123456789012")]
        [TestCase("OTHER", "  abc-123 ", "abc-123")]
        public void Normalize_ProducesTheCanonicalForm(string typeCode, string input, string expected) =>
            IdentificationRules.Normalize(typeCode, input).Should().Be(expected);

        [TestCase("CEDULA", "402-1234567-8", null)]
        [TestCase("RNC", "131-12345-6", null)]
        [TestCase("PASSPORT", "US12345678", "US")]
        [TestCase("PASSPORT", "DO123456", "DO")]
        [TestCase("PASSPORT", "US123456789012", "US")]
        [TestCase("OTHER", "anything", null)]
        public void Validate_AcceptsWellFormedDocuments(string typeCode, string identification, string? nationality) =>
            IdentificationRules.Validate(typeCode, identification, nationality).Should().BeNull();

        [TestCase("CEDULA", "402-123-4")]
        [TestCase("CEDULA", "4021234567")]
        [TestCase("CEDULA", "abc-1234567-8")]
        [TestCase("RNC", "131-1234-6")]
        [TestCase("RNC", "1311234567")]
        [TestCase("PASSPORT", "12345678")]
        [TestCase("PASSPORT", "U12345678")]
        [TestCase("PASSPORT", "US12345")]
        [TestCase("PASSPORT", "US1234567890123")]
        [TestCase("PASSPORT", "US1234A678")]
        public void Validate_RejectsMalformedDocuments(string typeCode, string identification) =>
            IdentificationRules.Validate(typeCode, identification, null).Should().NotBeNull();

        [TestCase(CustomerType.PhysicalPerson, "CEDULA", "DO")]
        [TestCase(CustomerType.PhysicalPerson, "CEDULA", "do")]
        [TestCase(CustomerType.PhysicalPerson, "CEDULA", null)]
        [TestCase(CustomerType.PhysicalPerson, "PASSPORT", "DO")]
        [TestCase(CustomerType.PhysicalPerson, "PASSPORT", "US")]
        [TestCase(CustomerType.LegalPerson, "RNC", "DO")]
        public void ValidateCombination_AcceptsTheAllowedPairings(CustomerType customerType, string typeCode, string? nationality) =>
            IdentificationRules.ValidateCombination(customerType, typeCode, nationality).Should().BeEmpty();

        [Test]
        public void ValidateCombination_ACédulaBelongsToDominicans() =>
            IdentificationRules.ValidateCombination(CustomerType.PhysicalPerson, "CEDULA", "US")
                .Should().ContainSingle().Which.Should().Contain("Dominican");

        [Test]
        public void ValidateCombination_AnIndividualCannotUseAnRnc() =>
            IdentificationRules.ValidateCombination(CustomerType.PhysicalPerson, "RNC", "DO")
                .Should().ContainSingle().Which.Should().Contain("legal person");

        [TestCase("DRIVER_LICENSE")]
        [TestCase("OTHER")]
        public void ValidateCombination_AnIndividualCanOnlyUseCédulaOrPassport(string typeCode) =>
            IdentificationRules.ValidateCombination(CustomerType.PhysicalPerson, typeCode, "DO")
                .Should().ContainSingle().Which.Should().Contain("cédula or passport");

        [TestCase("CEDULA")]
        [TestCase("PASSPORT")]
        [TestCase("OTHER")]
        public void ValidateCombination_ALegalPersonMustUseAnRnc(string typeCode) =>
            IdentificationRules.ValidateCombination(CustomerType.LegalPerson, typeCode, "DO")
                .Should().ContainSingle().Which.Should().Contain("RNC");

        [Test]
        public void Validate_ARejectsAPassportWhosePrefixIsNotTheNationality() =>
            IdentificationRules.Validate("PASSPORT", "ES12345678", "US").Should().Contain("US");

        [Test]
        public void Validate_TheCountryCodeComparisonIgnoresCase() =>
            IdentificationRules.Validate("PASSPORT", "US12345678", "us").Should().BeNull();
    }
}
