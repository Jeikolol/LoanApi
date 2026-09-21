using Application.Features.Payments;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class PaymentRulesTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(1000, 100, 1)]
        [TestCase(1000, 99, 0)]
        [TestCase(2200, 0, 2)]
        [TestCase(999999, 0, 12)]
        public void InstallmentsCovered_CountsWholeInstallmentsAndNeverExceedsTheTerm(double principal, double interest, int expected)
        {
            PaymentRules.InstallmentsCovered((decimal)principal, (decimal)interest, 1100m, 12).Should().Be(expected);
        }

        [Test]
        public void InstallmentsCovered_WithoutAValidInstallment_IsZero()
        {
            PaymentRules.InstallmentsCovered(500, 500, 0m, 12).Should().Be(0);
        }

        [Test]
        public void NextDueDate_FollowsTheScheduleAndEndsAtMaturity()
        {
            DateOnly disbursement = new(2026, 1, 31);
            DateOnly maturity = disbursement.AddMonths(12);

            PaymentRules.NextDueDate(disbursement, maturity, 0, 12).Should().Be(disbursement.AddMonths(1));
            PaymentRules.NextDueDate(disbursement, maturity, 5, 12).Should().Be(disbursement.AddMonths(6));
            PaymentRules.NextDueDate(disbursement, maturity, 12, 12).Should().Be(maturity);
        }

        [Test]
        public void DaysOverdue_IsNeverNegative()
        {
            PaymentRules.DaysOverdue(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 11)).Should().Be(10);
            PaymentRules.DaysOverdue(new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 11)).Should().Be(0);
        }

        [TestCase(LoanStatus.Defaulted, 100, 0, LoanStatus.Active)]
        [TestCase(LoanStatus.Defaulted, 100, 5, LoanStatus.Defaulted)]
        [TestCase(LoanStatus.Disbursed, 100, 0, LoanStatus.Active)]
        [TestCase(LoanStatus.Active, 100, 0, LoanStatus.Active)]
        [TestCase(LoanStatus.PartiallyPaid, 100, 10, LoanStatus.PartiallyPaid)]
        [TestCase(LoanStatus.Defaulted, 0, 0, LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.Active, 0, 0, LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.WrittenOff, 100, 0, LoanStatus.WrittenOff)]
        public void StatusAfterPayment_FollowsTheRules(LoanStatus current, double remaining, int daysOverdue, LoanStatus expected)
        {
            PaymentRules.StatusAfterPayment(current, (decimal)remaining, daysOverdue).Should().Be(expected);
        }

        [TestCase(LoanStatus.Disbursed, true)]
        [TestCase(LoanStatus.Active, true)]
        [TestCase(LoanStatus.PartiallyPaid, true)]
        [TestCase(LoanStatus.Defaulted, true)]
        [TestCase(LoanStatus.Restructured, true)]
        [TestCase(LoanStatus.FullyPaid, true)]
        [TestCase(LoanStatus.Pending, false)]
        [TestCase(LoanStatus.Approved, false)]
        [TestCase(LoanStatus.Closed, false)]
        [TestCase(LoanStatus.WrittenOff, false)]
        public void CanReversePaymentsOf_OnlyLoansThatAreStillInPlay(LoanStatus status, bool expected)
        {
            PaymentRules.CanReversePaymentsOf(status).Should().Be(expected);
        }

        [TestCase(LoanStatus.FullyPaid, 500, 0, LoanStatus.Active)]
        [TestCase(LoanStatus.FullyPaid, 500, 12, LoanStatus.Defaulted)]
        [TestCase(LoanStatus.Active, 500, 12, LoanStatus.Defaulted)]
        [TestCase(LoanStatus.Active, 500, 0, LoanStatus.Active)]
        [TestCase(LoanStatus.PartiallyPaid, 500, 0, LoanStatus.PartiallyPaid)]
        [TestCase(LoanStatus.Defaulted, 500, 12, LoanStatus.Defaulted)]
        [TestCase(LoanStatus.Closed, 500, 12, LoanStatus.Closed)]
        [TestCase(LoanStatus.WrittenOff, 500, 12, LoanStatus.WrittenOff)]
        [TestCase(LoanStatus.FullyPaid, 0, 0, LoanStatus.FullyPaid, TestName = "Nothing owed, still settled")]
        public void StatusAfterReversal_FollowsTheRules(LoanStatus current, double remaining, int daysOverdue, LoanStatus expected)
        {
            PaymentRules.StatusAfterReversal(current, (decimal)remaining, daysOverdue).Should().Be(expected);
        }
    }
}
