using Application.Exceptions;
using Application.Features.Delinquency;
using Application.Features.Delinquency.Commands;
using Application.Features.Delinquency.Handlers;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class WriteOffRulesTests
    {
        [Test]
        public void ALoanPastThePeriod_CanBeWrittenOff()
        {
            WriteOffRules.Problem(LoanStatus.Defaulted, 5000, 91, 90).Should().BeNull();
        }

        [TestCase(90)]
        [TestCase(30)]
        [TestCase(0)]
        public void ALoanNotPastThePeriod_CannotBeWrittenOff(int days)
        {
            WriteOffRules.Problem(LoanStatus.Defaulted, 5000, days, 90).Should().Contain("after 90 days");
        }

        [TestCase(LoanStatus.Pending)]
        [TestCase(LoanStatus.Approved)]
        [TestCase(LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.WrittenOff)]
        [TestCase(LoanStatus.Closed)]
        public void ALoanNotBeingCollected_CannotBeWrittenOff(LoanStatus status)
        {
            WriteOffRules.Problem(status, 5000, 200, 90).Should().Contain("cannot be written off");
        }

        [Test]
        public void ALoanWithNoBalance_CannotBeWrittenOff()
        {
            WriteOffRules.Problem(LoanStatus.Defaulted, 0, 200, 90).Should().Contain("no balance");
        }

        [Test]
        public void ARecalculation_NeverWritesALoanOffOnItsOwn()
        {
            DelinquencyRules.NextStatus(LoanStatus.Defaulted, 400).Should().Be(LoanStatus.Defaulted);
            DelinquencyRules.NextStatus(LoanStatus.Active, 400).Should().Be(LoanStatus.Defaulted);
            DelinquencyRules.NextStatus(LoanStatus.Active, 0).Should().Be(LoanStatus.Active);
        }
    }

    [TestFixture]
    public class WriteOffLoanCommandHandlerTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        private SqliteConnection _connection = null!;
        private WriteOffLoanCommandHandler _handler = null!;
        private Guid _loanId;

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
                CREATE TABLE Loans.Loan (Id TEXT, LoanNumber TEXT, LoanStatus INTEGER, PrincipalRemaining NUMERIC, NextPaymentDueDate TEXT, DaysOverdue INTEGER, UpdatedOn TEXT, IsDeleted INTEGER);
                CREATE TABLE Loans.DelinquencyPolicy (DaysUntilWriteOff INTEGER, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);");

            _loanId = Guid.NewGuid();
            Seed(daysOverdue: 120);
            _handler = new WriteOffLoanCommandHandler(_connection, NullLogger<WriteOffLoanCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private void Seed(int daysOverdue, LoanStatus status = LoanStatus.Defaulted, decimal remaining = 5000)
        {
            _connection.Execute("DELETE FROM Loans.Loan");
            _connection.Execute(
                "INSERT INTO Loans.Loan VALUES (@Id, 'LOAN-1', @Status, @Remaining, @Due, 0, NULL, 0)",
                new { Id = _loanId, Status = (int)status, Remaining = remaining, Due = Today.AddDays(-daysOverdue).ToString("yyyy-MM-dd") });
        }

        private Task<Application.Features.Loans.Commands.LoanTransitionResponse> WriteOff(string reason = "Incobrable tras gestión de cobro", Guid? id = null) =>
            _handler.Handle(new WriteOffLoanCommand { LoanId = id ?? _loanId, Reason = reason }, CancellationToken.None);

        private LoanStatus Status() => (LoanStatus)_connection.ExecuteScalar<int>("SELECT LoanStatus FROM Loans.Loan");

        [Test]
        public async Task ALoanPastThePolicyPeriod_IsWrittenOff()
        {
            var result = await WriteOff();

            result.LoanStatus.Should().Be(LoanStatus.WrittenOff);
            Status().Should().Be(LoanStatus.WrittenOff);
            _connection.ExecuteScalar<int>("SELECT DaysOverdue FROM Loans.Loan").Should().Be(120);
        }

        [Test]
        public void TheActivePolicyPeriodApplies()
        {
            _connection.Execute("INSERT INTO Loans.DelinquencyPolicy VALUES (200, 1, 0, '2026-01-01')");

            Assert.ThrowsAsync<ConflictException>(() => WriteOff());
            Status().Should().Be(LoanStatus.Defaulted);
        }

        [Test]
        public void ALoanNotPastThePeriod_IsRefused()
        {
            Seed(daysOverdue: 60);

            Assert.ThrowsAsync<ConflictException>(() => WriteOff());
            Status().Should().Be(LoanStatus.Defaulted);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("abc")]
        public void ARequiredReason_IsEnforced(string? reason)
        {
            Assert.ThrowsAsync<ValidationException>(() => WriteOff(reason!));
            Status().Should().Be(LoanStatus.Defaulted);
        }

        [Test]
        public async Task WritingOffTwice_IsAConflict()
        {
            await WriteOff();

            Assert.ThrowsAsync<ConflictException>(() => WriteOff());
        }

        [Test]
        public void UnknownLoan_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => WriteOff(id: Guid.NewGuid()));
        }
    }
}
