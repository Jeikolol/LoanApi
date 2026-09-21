using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class CreditScoreServiceTests
    {
        private readonly CreditScoreService _service = new();

        private static Customer CustomerWithOneLoan(
            LoanStatus status = LoanStatus.Active, int daysOverdue = 0, int completedPayments = 12, bool blacklisted = false)
        {
            var loan = new Loan
            {
                Id = Guid.NewGuid(),
                LoanStatus = status,
                DaysOverdue = daysOverdue,
                PrincipalRemaining = status is LoanStatus.FullyPaid or LoanStatus.Closed ? 0m : 10_000m,
                CreatedOn = DateTime.UtcNow.AddYears(-2).AddDays(-30),
                Payments = Enumerable.Range(0, completedPayments)
                    .Select(_ => new LoanPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Completed })
                    .ToList(),
            };

            return new Customer { Id = Guid.NewGuid(), CreditLimit = 100_000m, IsBlacklisted = blacklisted, Loans = [loan] };
        }

        [Test]
        public void ACustomerWhoPaysOnTimeAndOwesNothing_IsRatedExcellent()
        {
            CreditScore score = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.FullyPaid));

            score.PaymentHistoryScore.Should().Be(1000m);
            score.CreditRating.Should().Be(CreditRating.Excellent);
        }

        [Test]
        public void ALoanThatIsOverdueRightNow_LowersThePaymentHistory()
        {
            CreditScore current = _service.CalculateCreditScore(CustomerWithOneLoan(daysOverdue: 0));
            CreditScore overdue = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.Defaulted, daysOverdue: 45));

            overdue.PaymentHistoryScore.Should().Be(500m);
            overdue.Score.Should().BeLessThan(current.Score);
        }

        [Test]
        public void TheOverduePenaltyReachesItsFloorAtTheWriteOffHorizon()
        {
            CreditScore ninety = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.Defaulted, daysOverdue: 90));
            CreditScore muchLater = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.WrittenOff, daysOverdue: 400));

            ninety.PaymentHistoryScore.Should().Be(0m);
            muchLater.PaymentHistoryScore.Should().Be(0m);
        }

        [Test]
        public void ACustomerWithAWrittenOffLoan_IsNotRatedExcellent()
        {
            CreditScore score = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.WrittenOff, daysOverdue: 200));

            score.CreditRating.Should().NotBe(CreditRating.Excellent);
            score.DefaultHistoryScore.Should().Be(600m);
        }

        [Test]
        public void ABlacklistedCustomer_HasNoDefaultHistoryCredit()
        {
            CreditScore score = _service.CalculateCreditScore(CustomerWithOneLoan(blacklisted: true));

            score.DefaultHistoryScore.Should().Be(0m);
        }

        [Test]
        public void ACustomerWithoutLoans_GetsTheNeutralPaymentHistory()
        {
            var customer = new Customer { Id = Guid.NewGuid(), CreditLimit = 50_000m, Loans = [] };

            _service.CalculateCreditScore(customer).PaymentHistoryScore.Should().Be(500m);
        }

        [Test]
        public void TheScoreStaysWithinTheScale()
        {
            foreach (int days in new[] { 0, 1, 30, 89, 90, 500 })
            {
                CreditScore score = _service.CalculateCreditScore(CustomerWithOneLoan(LoanStatus.Defaulted, days));

                score.Score.Should().BeInRange(0, 1000);
            }
        }

        [Test]
        public void AReversedPayment_IsNeitherOnTimeNorMissed()
        {
            Customer clean = CustomerWithOneLoan(LoanStatus.FullyPaid);
            Customer withReversal = CustomerWithOneLoan(LoanStatus.FullyPaid);
            withReversal.Loans.First().Payments.Add(new LoanPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Reversed });
            withReversal.Loans.First().Payments.Add(new LoanPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Reversed });

            CreditScore score = _service.CalculateCreditScore(withReversal);

            score.PaymentHistoryScore.Should().Be(_service.CalculateCreditScore(clean).PaymentHistoryScore);
            score.PaymentHistoryScore.Should().Be(1000m);
        }

        [Test]
        public void ACustomerWhoseOnlyPaymentsWereReversed_HasNoPaymentHistoryYet()
        {
            Customer customer = CustomerWithOneLoan(completedPayments: 0);
            customer.Loans.First().Payments.Add(new LoanPayment { Id = Guid.NewGuid(), Status = PaymentStatus.Reversed });

            _service.CalculateCreditScore(customer).PaymentHistoryScore.Should().Be(300m);
        }
    }
}
