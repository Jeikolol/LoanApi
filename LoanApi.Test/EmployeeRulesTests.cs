using Application.Features.Employees;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class EmployeeRulesTests
    {
        [TestCase("maria.perez", true)]
        [TestCase("jperez", true)]
        [TestCase("a-b_c.9", true)]
        [TestCase("ab", false)]
        [TestCase("", false)]
        [TestCase("María", false)]
        [TestCase("with space", false)]
        [TestCase("Upper", false)]
        [TestCase(".startswithdot", false)]
        [TestCase("admin'--", false)]
        public void IsValidUserName_AcceptsOnlySimpleLowerCaseNames(string userName, bool valid)
        {
            EmployeeRules.IsValidUserName(userName).Should().Be(valid);
        }

        [Test]
        public void NormalizeUserName_TrimsAndLowersCase()
        {
            EmployeeRules.NormalizeUserName("  Maria.Perez ").Should().Be("maria.perez");
            EmployeeRules.NormalizeUserName(null).Should().BeEmpty();
        }

        [Test]
        public void TemporaryPasswords_MeetThePasswordPolicy()
        {
            for (int i = 0; i < 500; i++)
            {
                string password = EmployeeRules.GenerateTemporaryPassword();

                password.Should().HaveLength(EmployeeRules.TemporaryPasswordLength);
                password.Any(char.IsUpper).Should().BeTrue(password);
                password.Any(char.IsLower).Should().BeTrue(password);
                password.Any(char.IsDigit).Should().BeTrue(password);
                password.Any(c => !char.IsLetterOrDigit(c)).Should().BeTrue(password);
                password.Should().NotContainAny("0", "O", "1", "l", "I");
            }
        }

        [Test]
        public void TemporaryPasswords_AreNotRepeated()
        {
            Enumerable.Range(0, 1000).Select(_ => EmployeeRules.GenerateTemporaryPassword()).Distinct().Should().HaveCount(1000);
        }

        [Test]
        public void TokensValidFrom_IsTheNextWholeSecond()
        {
            DateTime moment = new(2026, 9, 21, 10, 30, 15, 987, DateTimeKind.Utc);

            EmployeeRules.TokensValidFrom(moment).Should().Be(new DateTime(2026, 9, 21, 10, 30, 16, DateTimeKind.Utc));
            EmployeeRules.TokensValidFrom(new DateTime(2026, 9, 21, 10, 30, 15, DateTimeKind.Utc))
                .Should().Be(new DateTime(2026, 9, 21, 10, 30, 16, DateTimeKind.Utc));
        }

        [Test]
        public void ATokenIssuedInTheSameSecondAsTheRevocation_IsRefused()
        {
            DateTime revokedAt = new(2026, 9, 21, 10, 30, 15, 400, DateTimeKind.Utc);
            long sameSecond = new DateTimeOffset(new DateTime(2026, 9, 21, 10, 30, 15, DateTimeKind.Utc)).ToUnixTimeSeconds();

            EmployeeRules.IsTokenValid(true, false, EmployeeRules.TokensValidFrom(revokedAt), sameSecond).Should().BeFalse();
            EmployeeRules.IsTokenValid(true, false, EmployeeRules.TokensValidFrom(revokedAt), sameSecond + 1).Should().BeTrue();
        }

        [Test]
        public void IsTokenValid_RefusesInactiveOrDeletedEmployees()
        {
            EmployeeRules.IsTokenValid(false, false, null, 100).Should().BeFalse();
            EmployeeRules.IsTokenValid(true, true, null, 100).Should().BeFalse();
        }

        [Test]
        public void IsTokenValid_AcceptsEverythingWhenNothingWasRevoked()
        {
            EmployeeRules.IsTokenValid(true, false, null, 100).Should().BeTrue();
            EmployeeRules.IsTokenValid(true, false, null, null).Should().BeTrue("tokens from before the iat claim existed stay valid until something is revoked");
        }

        [Test]
        public void IsTokenValid_RefusesTokensIssuedBeforeTheRevocation()
        {
            DateTime revokedAt = new(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
            long at = new DateTimeOffset(revokedAt).ToUnixTimeSeconds();

            EmployeeRules.IsTokenValid(true, false, revokedAt, at - 1).Should().BeFalse();
            EmployeeRules.IsTokenValid(true, false, revokedAt, at).Should().BeTrue("the cut-off itself is the first second that is accepted");
            EmployeeRules.IsTokenValid(true, false, revokedAt, at + 3600).Should().BeTrue();
            EmployeeRules.IsTokenValid(true, false, revokedAt, null).Should().BeFalse("a token without an issue time cannot prove it is newer");
        }
    }
}
