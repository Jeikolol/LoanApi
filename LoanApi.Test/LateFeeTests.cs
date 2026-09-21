using Application.Exceptions;
using Application.Features.Delinquency;
using Application.Features.Delinquency.Commands;
using Application.Features.Delinquency.Handlers;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class LateFeeRulesTests
    {
        private static readonly DateOnly Today = new(2026, 9, 21);
        private const decimal Rate = 0.001m;

        [TestCase(1100, 0.001, 1.10)]
        [TestCase(1134.72, 0.001, 1.13)]
        [TestCase(1134.72, 0.002, 2.27)]
        [TestCase(500, 0.0005, 0.25)]
        [TestCase(1000.05, 0.001, 1.00)]
        [TestCase(0, 0.001, 0)]
        public void DailyFee_IsRoundedToCents(double installment, double rate, double expected)
        {
            LateFeeRules.DailyFee((decimal)installment, (decimal)rate).Should().Be((decimal)expected);
        }

        [TestCase(0, 0.001, 0)]
        [TestCase(-3, 0.001, 0)]
        [TestCase(10, 0.001, 1.00)]
        [TestCase(45, 0.002, 9.00)]
        [TestCase(7, 0.0015, 1.05)]
        [TestCase(5000, 0.001, 100)]
        public void DelinquencyPercentage_IsDaysTimesTheDailyRate_CappedAt100(int days, double rate, double expected)
        {
            LateFeeRules.DelinquencyPercentage(days, (decimal)rate).Should().Be((decimal)expected);
        }

        /// <summary>A 12-month loan of 1,000 installments disbursed on <paramref name="disbursed"/>; the fee per late installment per day is 1.00.</summary>
        private static LateFeeAssessment Assess(DateOnly disbursed, decimal paid = 0m, DateOnly? chargedThrough = null, DateOnly? today = null) =>
            LateFeeRules.Assess(disbursed, 12, 1000m, paid, 0m, chargedThrough, today ?? Today, Rate);

        private static readonly DateOnly OneLate = new(2026, 8, 1);      // due 1 Sep
        private static readonly DateOnly ThreeLate = new(2026, 6, 15);   // due 15 Jul, 15 Aug, 15 Sep

        [Test]
        public void OneLateInstallment_ChargesEachDayAfterItsDueDate()
        {
            LateFeeAssessment result = Assess(OneLate);

            result.DaysCharged.Should().Be(20, "2 to 21 September");
            result.Amount.Should().Be(20.00m);
            result.InstallmentsOverdue.Should().Be(1);
        }

        [Test]
        public void SeveralLateInstallments_EachPayItsOwnFee()
        {
            LateFeeAssessment result = Assess(ThreeLate);

            // 31 days with 1 late installment, 31 days with 2, then 6 days with 3.
            result.DaysCharged.Should().Be(68);
            result.Amount.Should().Be(31 * 1 + 31 * 2 + 6 * 3);
            result.InstallmentsOverdue.Should().Be(3);
        }

        [Test]
        public void ThePaymentsSoFarCoverTheOldestInstallmentsFirst()
        {
            LateFeeAssessment result = Assess(ThreeLate, paid: 1000m);

            result.Amount.Should().Be(31 * 1 + 6 * 2, "installment 1 is paid, so the fee starts with installment 2");
            result.InstallmentsOverdue.Should().Be(2);
        }

        [Test]
        public void OnlyTheDaysAfterTheLastCharge_AreCharged()
        {
            LateFeeAssessment result = Assess(ThreeLate, chargedThrough: new DateOnly(2026, 9, 10));

            result.DaysCharged.Should().Be(11);
            result.Amount.Should().Be(5 * 2 + 6 * 3, "11-15 September have 2 late installments, 16-21 have 3");
        }

        [Test]
        public void AlreadyChargedToday_HasNothingLeft()
        {
            Assess(ThreeLate, chargedThrough: Today).Should().Be(new LateFeeAssessment(0, 0m, 3));
        }

        [Test]
        public void ALoanThatIsNotOverdue_HasNothingToCharge()
        {
            Assess(OneLate, today: new DateOnly(2026, 9, 1)).Amount.Should().Be(0, "the due day itself is not late");
            Assess(OneLate, today: new DateOnly(2026, 8, 20)).Amount.Should().Be(0);
        }

        [Test]
        public void ASettledLoan_HasNothingToCharge()
        {
            Assess(ThreeLate, paid: 12000m).Should().Be(new LateFeeAssessment(0, 0m, 0));
        }

        [Test]
        public void ChargingEveryDay_ChargesEachInstallmentDayExactlyOnce()
        {
            DateOnly? through = null;
            decimal total = 0m;

            for (DateOnly day = ThreeLate.AddMonths(1); day <= Today; day = day.AddDays(1))
            {
                total += Assess(ThreeLate, chargedThrough: through, today: day).Amount;
                through = day;
            }

            total.Should().Be(Assess(ThreeLate).Amount, "however often the charge runs, the total is the same");
        }

        [Test]
        public void ChargingTwiceEveryDay_StillChargesEachDayOnce()
        {
            DateOnly? through = null;
            decimal total = 0m;

            for (DateOnly day = ThreeLate.AddMonths(1); day <= Today; day = day.AddDays(1))
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    total += Assess(ThreeLate, chargedThrough: through, today: day).Amount;
                    through = day;
                }
            }

            total.Should().Be(Assess(ThreeLate).Amount);
        }

        [Test]
        public void APaymentDuringTheOverduePeriod_StopsTheFeesOfTheInstallmentItCovers()
        {
            // Charged through 10 Sep with installments 1 and 2 late; then installment 1 is paid.
            LateFeeAssessment after = Assess(ThreeLate, paid: 1000m, chargedThrough: new DateOnly(2026, 9, 10));

            after.Amount.Should().Be(5 * 1 + 6 * 2, "only installments 2 and 3 keep accruing");
        }
    }

    [TestFixture]
    public class ChargeLateFeeCommandHandlerTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        private SqliteConnection _connection = null!;
        private ChargeLateFeeCommandHandler _handler = null!;
        private Guid _loanId;

        /// <summary>How many days the first installment of the seeded loan is overdue (month lengths can move it a day or two from the request).</summary>
        private int _days;

        [OneTimeSetUp]
        public void RegisterHandlers()
        {
            SqlMapper.AddTypeHandler(new SqliteGuidHandler());
            SqlMapper.AddTypeHandler(new SqliteDateOnlyHandler());
        }

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Loans;
                CREATE TABLE Loans.Loan (
                    Id TEXT, LoanNumber TEXT, LoanStatus INTEGER, NextPaymentDueDate TEXT, InstallmentAmount NUMERIC,
                    LateFeesCharged NUMERIC, LateFeesChargedThrough TEXT, AmortizationBalance NUMERIC, RemainingInterest NUMERIC,
                    DisbursementDate TEXT, TermMonths INTEGER, PrincipalPaid NUMERIC, PaidInterest NUMERIC,
                    UpdatedOn TEXT, IsDeleted INTEGER);
                CREATE TABLE Loans.DelinquencyPolicy (DailyPenaltyRate NUMERIC, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);");

            _loanId = Guid.NewGuid();
            SeedLoan(daysOverdue: 10);

            _handler = new ChargeLateFeeCommandHandler(new LateFeeCharger(_connection, NullLogger<LateFeeCharger>.Instance));
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        /// <summary>A 12-month loan of 1,100 installments whose first installment fell due about <paramref name="daysOverdue"/> days ago.</summary>
        private void SeedLoan(int daysOverdue, LoanStatus status = LoanStatus.Defaulted, DateOnly? chargedThrough = null, decimal paid = 0m)
        {
            DateOnly disbursed = Today.AddDays(-daysOverdue).AddMonths(-1);
            DateOnly firstDue = disbursed.AddMonths(1);
            _days = Today.DayNumber - firstDue.DayNumber;

            _connection.Execute("DELETE FROM Loans.Loan");
            _connection.Execute(
                @"INSERT INTO Loans.Loan
                    (Id, LoanNumber, LoanStatus, NextPaymentDueDate, InstallmentAmount, LateFeesCharged, LateFeesChargedThrough,
                     AmortizationBalance, RemainingInterest, DisbursementDate, TermMonths, PrincipalPaid, PaidInterest, UpdatedOn, IsDeleted)
                  VALUES (@Id, 'LOAN-1', @Status, @Due, 1100, 0, @Through, 9000, 2500, @Disbursed, 12, @Paid, 0, NULL, 0)",
                new
                {
                    Id = _loanId,
                    Status = (int)status,
                    Due = firstDue.ToString("yyyy-MM-dd"),
                    Through = chargedThrough?.ToString("yyyy-MM-dd"),
                    Disbursed = disbursed.ToString("yyyy-MM-dd"),
                    Paid = paid,
                });
        }

        private Task<LateFeeChargeResponse> Charge(Guid? loanId = null) =>
            _handler.Handle(new ChargeLateFeeCommand(loanId ?? _loanId), CancellationToken.None);

        private decimal Charged() => Convert.ToDecimal(_connection.ExecuteScalar<object>("SELECT LateFeesCharged FROM Loans.Loan"));

        private const decimal OneDay = 1.10m; // 1,100 x 0.1%

        [Test]
        public async Task TheFirstCharge_CoversEveryOverdueDay()
        {
            LateFeeChargeResponse result = await Charge();

            result.DaysCharged.Should().Be(_days);
            result.AmountCharged.Should().Be(_days * OneDay);
            result.TotalLateFees.Should().Be(_days * OneDay);
            result.ChargedThrough.Should().Be(Today);
            Charged().Should().Be(_days * OneDay);
        }

        [Test]
        public async Task ChargingAgainOnTheSameDay_ChargesNothing()
        {
            await Charge();

            LateFeeChargeResponse again = await Charge();

            again.DaysCharged.Should().Be(0);
            again.AmountCharged.Should().Be(0);
            again.TotalLateFees.Should().Be(_days * OneDay);
            Charged().Should().Be(_days * OneDay, "running it twice must not charge twice");
        }

        [Test]
        public async Task ManyRepeatedCalls_NeverAddUp()
        {
            for (int i = 0; i < 5; i++) await Charge();

            Charged().Should().Be(_days * OneDay);
        }

        [Test]
        public async Task TheNextDay_ChargesOnlyThatDay()
        {
            SeedLoan(daysOverdue: 11, chargedThrough: Today.AddDays(-1));

            LateFeeChargeResponse result = await Charge();

            result.DaysCharged.Should().Be(1);
            result.AmountCharged.Should().Be(OneDay);
        }

        [Test]
        public async Task SeveralLateInstallments_ChargeTheFeeOfEachOne()
        {
            SeedLoan(daysOverdue: 45); // the first installment is 45 days late, the second about 15

            LateFeeChargeResponse result = await Charge();

            result.AmountCharged.Should().BeGreaterThan(_days * OneDay, "the second late installment adds its own fee");
            Charged().Should().Be(result.AmountCharged);
        }

        [Test]
        public async Task APaidInstallment_IsNotCharged()
        {
            SeedLoan(daysOverdue: 45, paid: 1100m); // installment 1 is covered; installment 2 is the one that is late

            LateFeeChargeResponse result = await Charge();

            result.AmountCharged.Should().BeLessThan(_days * OneDay, "only the second installment is late, and for fewer days");
        }

        [Test]
        public async Task TheFeesDoNotTouchThePrincipalOrTheInterest()
        {
            await Charge();

            Convert.ToDecimal(_connection.ExecuteScalar<object>("SELECT AmortizationBalance FROM Loans.Loan")).Should().Be(9000m);
            Convert.ToDecimal(_connection.ExecuteScalar<object>("SELECT RemainingInterest FROM Loans.Loan")).Should().Be(2500m);
        }

        [Test]
        public async Task TheRateComesFromTheActivePolicy()
        {
            _connection.Execute("INSERT INTO Loans.DelinquencyPolicy VALUES (0.005, 0, 0, '2026-01-01')");
            _connection.Execute("INSERT INTO Loans.DelinquencyPolicy VALUES (0.002, 1, 0, '2026-02-01')");

            LateFeeChargeResponse result = await Charge();

            result.AmountCharged.Should().Be(_days * 2.20m, "days x 1,100 x 0.2%, ignoring the inactive policy");
        }

        [Test]
        public async Task WithoutAnActivePolicy_TheDefaultRateApplies()
        {
            LateFeeChargeResponse result = await Charge();

            result.AmountCharged.Should().Be(_days * Math.Round(1100m * LateFeeCharger.DefaultDailyRate, 2));
        }

        [Test]
        public void ALoanThatIsNotOverdue_CannotBeCharged()
        {
            SeedLoan(daysOverdue: 0, status: LoanStatus.Active);

            Assert.ThrowsAsync<ConflictException>(() => Charge());
            Charged().Should().Be(0);
        }

        [TestCase(LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.Closed)]
        [TestCase(LoanStatus.Pending)]
        [TestCase(LoanStatus.WrittenOff)]
        public void ALoanThatIsNotBeingCollected_CannotBeCharged(LoanStatus status)
        {
            SeedLoan(daysOverdue: 10, status: status);

            Assert.ThrowsAsync<ConflictException>(() => Charge());
        }

        [Test]
        public void UnknownLoan_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => Charge(Guid.NewGuid()));
        }
    }
}
