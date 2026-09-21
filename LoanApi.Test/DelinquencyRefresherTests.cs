using Application.Features.Delinquency;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class DelinquencyRefresherTests
    {
        private static readonly DateOnly Today = new(2026, 9, 21);

        private SqliteConnection _connection = null!;
        private DelinquencyRefresher _refresher = null!;

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
                CREATE TABLE Loans.Loan (Id TEXT, LoanStatus INTEGER, NextPaymentDueDate TEXT, DaysOverdue INTEGER, DelinquencyPercentage NUMERIC, UpdatedOn TEXT, IsDeleted INTEGER);
                CREATE TABLE Loans.DelinquencyPolicy (DailyPenaltyRate NUMERIC, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);");
            _connection.Execute("INSERT INTO Loans.DelinquencyPolicy VALUES (0.001, 1, 0, '2026-01-01')");

            _refresher = new DelinquencyRefresher(_connection, NullLogger<DelinquencyRefresher>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private Guid AddLoan(LoanStatus status, int daysPastDue, int storedDays = 0, decimal storedPercentage = 0m, bool deleted = false)
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                "INSERT INTO Loans.Loan VALUES (@Id, @Status, @Due, @Days, @Pct, NULL, @Deleted)",
                new { Id = id, Status = (int)status, Due = Today.AddDays(-daysPastDue).ToString("yyyy-MM-dd"), Days = storedDays, Pct = storedPercentage, Deleted = deleted ? 1 : 0 });
            return id;
        }

        private (LoanStatus Status, int Days, decimal Percentage) Read(Guid id)
        {
            dynamic row = _connection.QuerySingle("SELECT LoanStatus, DaysOverdue, DelinquencyPercentage FROM Loans.Loan WHERE Id = @Id", new { Id = id });
            return ((LoanStatus)(int)(long)row.LoanStatus, (int)(long)row.DaysOverdue, Convert.ToDecimal((object)row.DelinquencyPercentage));
        }

        [Test]
        public async Task AnActiveLoanThatStoppedPaying_BecomesDelinquent()
        {
            Guid id = AddLoan(LoanStatus.Active, daysPastDue: 12);

            DelinquencyRefreshResponse result = await _refresher.RefreshAsync(Today);

            Read(id).Should().Be((LoanStatus.Defaulted, 12, 1.20m));
            result.Updated.Should().Be(1);
            result.NowDelinquent.Should().Be(1);
        }

        [Test]
        public async Task ADelinquentLoanKeepsAccumulatingDays()
        {
            Guid id = AddLoan(LoanStatus.Defaulted, daysPastDue: 40, storedDays: 12, storedPercentage: 1.20m);

            await _refresher.RefreshAsync(Today);

            Read(id).Should().Be((LoanStatus.Defaulted, 40, 4.00m));
        }

        [Test]
        public async Task ALoanThatCaughtUp_GoesBackToActive()
        {
            Guid id = AddLoan(LoanStatus.Defaulted, daysPastDue: 0, storedDays: 12, storedPercentage: 1.20m);

            DelinquencyRefreshResponse result = await _refresher.RefreshAsync(Today);

            Read(id).Should().Be((LoanStatus.Active, 0, 0m));
            result.BackToActive.Should().Be(1);
        }

        [Test]
        public async Task ALoanNotYetDue_IsLeftAlone()
        {
            Guid id = AddLoan(LoanStatus.Active, daysPastDue: -10);

            DelinquencyRefreshResponse result = await _refresher.RefreshAsync(Today);

            Read(id).Should().Be((LoanStatus.Active, 0, 0m));
            result.Updated.Should().Be(0);
        }

        [Test]
        public async Task ANeverWrittenOffOnItsOwn_NoMatterHowLate()
        {
            Guid id = AddLoan(LoanStatus.Defaulted, daysPastDue: 500);

            await _refresher.RefreshAsync(Today);

            Read(id).Status.Should().Be(LoanStatus.Defaulted);
            Read(id).Percentage.Should().Be(50.00m);
        }

        [TestCase(LoanStatus.Pending)]
        [TestCase(LoanStatus.Approved)]
        [TestCase(LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.WrittenOff)]
        [TestCase(LoanStatus.Closed)]
        public async Task LoansNotBeingRepaid_AreNeverTouched(LoanStatus status)
        {
            Guid id = AddLoan(status, daysPastDue: 30);

            DelinquencyRefreshResponse result = await _refresher.RefreshAsync(Today);

            Read(id).Should().Be((status, 0, 0m));
            result.LoansChecked.Should().Be(0);
        }

        [Test]
        public async Task RunningItAgain_ChangesNothing()
        {
            AddLoan(LoanStatus.Active, daysPastDue: 12);
            await _refresher.RefreshAsync(Today);

            DelinquencyRefreshResponse again = await _refresher.RefreshAsync(Today);

            again.Updated.Should().Be(0);
        }

        [Test]
        public async Task TheRateComesFromTheActivePolicy()
        {
            _connection.Execute("UPDATE Loans.DelinquencyPolicy SET DailyPenaltyRate = 0.005");
            Guid id = AddLoan(LoanStatus.Active, daysPastDue: 10);

            await _refresher.RefreshAsync(Today);

            Read(id).Percentage.Should().Be(5.00m);
        }

        [Test]
        public async Task DeletedLoans_AreIgnored()
        {
            AddLoan(LoanStatus.Active, daysPastDue: 12, deleted: true);

            (await _refresher.RefreshAsync(Today)).LoansChecked.Should().Be(0);
        }
    }
}
