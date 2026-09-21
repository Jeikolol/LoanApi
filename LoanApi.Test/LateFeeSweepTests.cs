using Application.Exceptions;
using Application.Features.Delinquency;
using Application.Models.Responses;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LoanApi.Test
{
    [TestFixture]
    public class LateFeeSweepTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        private SqliteConnection _connection = null!;
        private LateFeeSweep _sweep = null!;

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
                    LateFeesCharged NUMERIC, LateFeesChargedThrough TEXT, DisbursementDate TEXT, TermMonths INTEGER, PrincipalPaid NUMERIC,
                    PaidInterest NUMERIC, UpdatedOn TEXT, IsDeleted INTEGER);
                CREATE TABLE Loans.DelinquencyPolicy (DailyPenaltyRate NUMERIC, AutomaticallyChargeLateFees INTEGER, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);");

            SetPolicy(automatic: true);
            _sweep = new LateFeeSweep(_connection, new LateFeeCharger(_connection, NullLogger<LateFeeCharger>.Instance), NullLogger<LateFeeSweep>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private void SetPolicy(bool automatic, bool active = true)
        {
            _connection.Execute("DELETE FROM Loans.DelinquencyPolicy");
            _connection.Execute("INSERT INTO Loans.DelinquencyPolicy VALUES (0.001, @Auto, @Active, 0, '2026-01-01')", new { Auto = automatic ? 1 : 0, Active = active ? 1 : 0 });
        }

        private Guid AddLoan(int daysOverdue, LoanStatus status = LoanStatus.Defaulted, DateOnly? chargedThrough = null)
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                @"INSERT INTO Loans.Loan
                    (Id, LoanNumber, LoanStatus, NextPaymentDueDate, InstallmentAmount, LateFeesCharged, LateFeesChargedThrough,
                     DisbursementDate, TermMonths, PrincipalPaid, PaidInterest, UpdatedOn, IsDeleted)
                  VALUES (@Id, @Number, @Status, @Due, 1000, 0, @Through, @Disbursed, 12, 0, 0, NULL, 0)",
                new
                {
                    Id = id,
                    Number = $"LOAN-{id.ToString()[..6]}",
                    Status = (int)status,
                    Due = Today.AddDays(-daysOverdue).AddMonths(-1).AddMonths(1).ToString("yyyy-MM-dd"),
                    Through = chargedThrough?.ToString("yyyy-MM-dd"),
                    Disbursed = Today.AddDays(-daysOverdue).AddMonths(-1).ToString("yyyy-MM-dd"),
                });
            return id;
        }

        private decimal FeesOf(Guid id) => Convert.ToDecimal(_connection.ExecuteScalar<object>("SELECT LateFeesCharged FROM Loans.Loan WHERE Id = @Id", new { Id = id }));

        // ---------------------------------------------------------------- what it charges

        [Test]
        public async Task ChargesEveryOverdueLoanThatIsBeingCollected_AndOnlyThose()
        {
            Guid a = AddLoan(5);
            Guid b = AddLoan(10, LoanStatus.Active);
            Guid c = AddLoan(3, LoanStatus.Restructured);
            Guid notOverdue = AddLoan(0, LoanStatus.Active);
            Guid paidOff = AddLoan(20, LoanStatus.FullyPaid);
            Guid closed = AddLoan(20, LoanStatus.Closed);
            Guid writtenOff = AddLoan(200, LoanStatus.WrittenOff);
            Guid alreadyToday = AddLoan(7, chargedThrough: Today);

            LateFeeSweepResponse result = await _sweep.RunAsync(automatic: false);

            result.LoansCharged.Should().Be(3);
            result.DaysCharged.Should().Be(5 + 10 + 3);
            result.AmountCharged.Should().Be(18m, "18 overdue days x (1,000 x 0.1%)");
            FeesOf(a).Should().Be(5m);
            FeesOf(b).Should().Be(10m);
            FeesOf(c).Should().Be(3m);
            new[] { notOverdue, paidOff, closed, writtenOff, alreadyToday }.Select(FeesOf).Should().OnlyContain(fee => fee == 0m);
        }

        [Test]
        public async Task RunningItAgain_ChargesNothingMore()
        {
            Guid loan = AddLoan(10);
            await _sweep.RunAsync(automatic: false);

            LateFeeSweepResponse again = await _sweep.RunAsync(automatic: false);

            again.LoansChecked.Should().Be(0, "everything is charged through today already");
            again.AmountCharged.Should().Be(0);
            FeesOf(loan).Should().Be(10m);
        }

        [Test]
        public async Task RunningItEveryDay_ChargesEachDayOnce()
        {
            Guid loan = AddLoan(10);
            await _sweep.RunAsync(automatic: false);

            // Tomorrow: the loan is a day older and was charged through today.
            _connection.Execute("UPDATE Loans.Loan SET NextPaymentDueDate = @Due, LateFeesChargedThrough = @Through",
                new { Due = Today.AddDays(-11).ToString("yyyy-MM-dd"), Through = Today.AddDays(-1).ToString("yyyy-MM-dd") });
            LateFeeSweepResponse tomorrow = await _sweep.RunAsync(automatic: false);

            tomorrow.DaysCharged.Should().Be(1);
            FeesOf(loan).Should().Be(11m);
        }

        // ---------------------------------------------------------------- the policy

        [Test]
        public async Task TheAutomaticRun_DoesNothingWhenThePolicyDoesNotAllowIt()
        {
            SetPolicy(automatic: false);
            Guid loan = AddLoan(10);

            LateFeeSweepResponse result = await _sweep.RunAsync(automatic: true);

            result.LoansChecked.Should().Be(0);
            result.Automatic.Should().BeTrue();
            FeesOf(loan).Should().Be(0m);
        }

        [Test]
        public async Task TheAutomaticRun_DoesNothingWithoutAnActivePolicy()
        {
            SetPolicy(automatic: true, active: false);
            Guid loan = AddLoan(10);

            await _sweep.RunAsync(automatic: true);

            FeesOf(loan).Should().Be(0m);
        }

        [Test]
        public async Task TheAutomaticRun_ChargesWhenThePolicyAllowsIt()
        {
            Guid loan = AddLoan(10);

            LateFeeSweepResponse result = await _sweep.RunAsync(automatic: true);

            result.LoansCharged.Should().Be(1);
            FeesOf(loan).Should().Be(10m);
        }

        [Test]
        public async Task AnAdministratorsManualRun_IgnoresThePolicyFlag()
        {
            SetPolicy(automatic: false);
            Guid loan = AddLoan(10);

            LateFeeSweepResponse result = await _sweep.RunAsync(automatic: false);

            result.LoansCharged.Should().Be(1);
            result.Automatic.Should().BeFalse();
            FeesOf(loan).Should().Be(10m);
        }

        // ---------------------------------------------------------------- isolation

        [Test]
        public async Task OneLoanThatFails_DoesNotStopTheOthers()
        {
            Guid good1 = AddLoan(4);
            Guid bad = AddLoan(9);
            Guid good2 = AddLoan(2);

            Mock<ILateFeeCharger> charger = new();
            charger.Setup(c => c.ChargeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Guid id, DateOnly _, CancellationToken _) => new LateFeeChargeResponse(id, "L", 1, 1m, 1m, Today));
            charger.Setup(c => c.ChargeAsync(bad, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("boom"));
            LateFeeSweep sweep = new(_connection, charger.Object, NullLogger<LateFeeSweep>.Instance);

            LateFeeSweepResponse result = await sweep.RunAsync(automatic: false);

            result.Failed.Should().Be(1);
            result.LoansCharged.Should().Be(2);
            charger.Verify(c => c.ChargeAsync(good1, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
            charger.Verify(c => c.ChargeAsync(good2, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ALoanThatIsNoLongerChargeable_CountsAsSkippedNotFailed()
        {
            Guid loan = AddLoan(6);
            Mock<ILateFeeCharger> charger = new();
            charger.Setup(c => c.ChargeAsync(loan, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new ConflictException("charged in the meantime"));
            LateFeeSweep sweep = new(_connection, charger.Object, NullLogger<LateFeeSweep>.Instance);

            LateFeeSweepResponse result = await sweep.RunAsync(automatic: false);

            result.Skipped.Should().Be(1);
            result.Failed.Should().Be(0);
        }

        [Test]
        public async Task ACancelledRun_StopsInsteadOfChargingTheRest()
        {
            AddLoan(3);
            AddLoan(4);
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            Func<Task> act = () => _sweep.RunAsync(automatic: false, cancelled.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }

    [TestFixture]
    public class DailyScheduleTests
    {
        [TestCase("06:00", true, 6, 0)]
        [TestCase("6:00", true, 6, 0)]
        [TestCase("23:59", true, 23, 59)]
        [TestCase("00:00", true, 0, 0)]
        [TestCase("24:00", false, 0, 0)]
        [TestCase("06:60", false, 0, 0)]
        [TestCase("six", false, 0, 0)]
        [TestCase("", false, 0, 0)]
        [TestCase(null, false, 0, 0)]
        public void TryParseTime_AcceptsOnlyATimeOfDay(string? text, bool valid, int hour, int minute)
        {
            DailySchedule.TryParseTime(text, out TimeSpan time).Should().Be(valid);
            if (valid) time.Should().Be(new TimeSpan(hour, minute, 0));
        }

        [Test]
        public void BeforeTheRunTime_TheNextRunIsToday()
        {
            DateTime now = new(2026, 9, 21, 3, 15, 0, DateTimeKind.Utc);

            DailySchedule.NextRunUtc(now, new TimeSpan(6, 0, 0)).Should().Be(new DateTime(2026, 9, 21, 6, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void AfterTheRunTime_TheNextRunIsTomorrow()
        {
            DateTime now = new(2026, 9, 21, 6, 0, 1, DateTimeKind.Utc);

            DailySchedule.NextRunUtc(now, new TimeSpan(6, 0, 0)).Should().Be(new DateTime(2026, 9, 22, 6, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void ExactlyAtTheRunTime_TheNextRunIsTomorrow()
        {
            DateTime now = new(2026, 9, 21, 6, 0, 0, DateTimeKind.Utc);

            DailySchedule.NextRunUtc(now, new TimeSpan(6, 0, 0)).Should().Be(new DateTime(2026, 9, 22, 6, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void TheNextRunIsAlwaysWithinADay()
        {
            DateTime start = new(2026, 12, 30, 0, 0, 0, DateTimeKind.Utc);

            for (int minute = 0; minute < 3 * 24 * 60; minute += 37)
            {
                DateTime now = start.AddMinutes(minute);
                DateTime next = DailySchedule.NextRunUtc(now, new TimeSpan(6, 30, 0));

                next.Should().BeAfter(now);
                (next - now).Should().BeLessThanOrEqualTo(TimeSpan.FromDays(1));
                next.TimeOfDay.Should().Be(new TimeSpan(6, 30, 0));
            }
        }
    }
}
