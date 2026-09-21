using Application.Features.Dashboard;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class DelinquencyHistoryTests
    {
        private static readonly DateOnly Disbursed = new(2026, 1, 15);
        private static readonly Guid LoanId = Guid.NewGuid();

        private static HistoryLoan Loan(DateOnly? disbursed = null, Guid? id = null) =>
            new(id ?? LoanId, disbursed ?? Disbursed, (disbursed ?? Disbursed).AddMonths(12), 12, 1000m);

        private static ILookup<Guid, HistoryPayment> Payments(params HistoryPayment[] payments) => payments.ToLookup(p => p.LoanId);

        [Test]
        public void ALoanIsNotOverdueBeforeItsFirstInstallmentIsDue()
        {
            DelinquencyHistory.AverageDaysOverdue(Disbursed.AddDays(20), [Loan()], Payments()).Should().Be(0);
        }

        [Test]
        public void AnUnpaidLoanIsOverdueSinceItsFirstDueDate()
        {
            // First installment fell due on 15 Feb; on 25 Feb it is 10 days late.
            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 25), [Loan()], Payments()).Should().Be(10);
        }

        [Test]
        public void APaymentCoveringTheInstallmentMovesTheDueDateForward()
        {
            var paid = new HistoryPayment(LoanId, new DateOnly(2026, 2, 20), 1000m, null);

            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 25), [Loan()], Payments(paid)).Should().Be(0);
            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 3, 20), [Loan()], Payments(paid)).Should().Be(5, "the 15 Mar installment is unpaid");
        }

        [Test]
        public void APaymentOnlyCountsFromItsDate()
        {
            var paid = new HistoryPayment(LoanId, new DateOnly(2026, 2, 20), 1000m, null);

            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 18), [Loan()], Payments(paid)).Should().Be(3);
        }

        [Test]
        public void AReversedPaymentCountsOnlyUntilItsReversal()
        {
            var paid = new HistoryPayment(LoanId, new DateOnly(2026, 2, 20), 1000m, new DateOnly(2026, 2, 24));

            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 22), [Loan()], Payments(paid)).Should().Be(0);
            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 25), [Loan()], Payments(paid)).Should().Be(10);
        }

        [Test]
        public void ASettledLoanIsNeverOverdue_EvenPastItsMaturity()
        {
            var paid = new HistoryPayment(LoanId, new DateOnly(2026, 6, 1), 12000m, null);

            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2027, 6, 1), [Loan()], Payments(paid)).Should().Be(0);
        }

        [Test]
        public void TheAverageIsTakenOverTheLoansAlreadyDisbursed()
        {
            HistoryLoan late = Loan();
            HistoryLoan onTime = Loan(id: Guid.NewGuid());
            HistoryLoan future = Loan(new DateOnly(2026, 6, 1), Guid.NewGuid());
            var paid = new HistoryPayment(onTime.Id, new DateOnly(2026, 2, 10), 1000m, null);

            DelinquencyHistory.AverageDaysOverdue(new DateOnly(2026, 2, 25), [late, onTime, future], Payments(paid)).Should().Be(5, "(10 + 0) / 2 loans");
        }

        [Test]
        public void NoLoansMeansZero()
        {
            DelinquencyHistory.AverageDaysOverdue(Disbursed, [], Payments()).Should().Be(0);
        }

        [Test]
        public void TheTrendRunsOldestToNewest_AgainstTheSameDaysOnePeriodEarlier()
        {
            DateOnly today = new(2026, 3, 1);

            var (labels, current, previous) = DelinquencyHistory.Trend(today, 30, [Loan()], []);

            labels.Should().HaveCount(30);
            labels[0].Should().Be("31/01");
            labels[^1].Should().Be("01/03");
            current.Should().BeInAscendingOrder("an unpaid loan only gets later");
            current[^1].Should().Be(14, "due 15 Feb, unpaid on 1 Mar");
            previous[^1].Should().Be(0, "a month earlier (30 Jan) the first installment was not due yet");
            previous.Should().HaveCount(30);
        }
    }
}
