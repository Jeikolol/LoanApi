using Application.Exceptions;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Handlers;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class CreateLoanCommandHandlerTests
    {
        private SqliteConnection _connection = null!;
        private CreateLoanCommandHandler _handler = null!;
        private Guid _customerId;
        private Guid _branchId;
        private Guid _currencyId;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _customerId = Guid.NewGuid();
            _branchId = Guid.NewGuid();
            _currencyId = Guid.NewGuid();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Loans;
                ATTACH DATABASE ':memory:' AS Customers;
                ATTACH DATABASE ':memory:' AS Admin;

                CREATE TABLE Customers.Customer (Id TEXT, Status INTEGER, IsBlacklisted INTEGER, IsDeleted INTEGER);
                CREATE TABLE Admin.Branch (Id TEXT, IsDeleted INTEGER);

                CREATE TABLE Loans.Loan (
                    Id TEXT, LoanNumber TEXT, CustomerId TEXT, BranchId TEXT, CurrencyId TEXT,
                    PrincipalAmount NUMERIC, InterestRate NUMERIC, TermMonths INTEGER, InstallmentAmount NUMERIC,
                    LoanStatus INTEGER, LoanPurpose INTEGER, DisbursementDate TEXT, MaturityDate TEXT,
                    NextPaymentDueDate TEXT, DaysOverdue INTEGER, DelinquencyPercentage NUMERIC,
                    PrincipalRemaining NUMERIC, TotalInterestAmount NUMERIC, PaidInterest NUMERIC,
                    RemainingInterest NUMERIC, AmortizationBalance NUMERIC, PrincipalPaid NUMERIC,
                    MaxCredit NUMERIC, RequiresGuarantor INTEGER, GuarantorId TEXT, LastPaymentDate TEXT,
                    CreatedOn TEXT, UpdatedOn TEXT, IsActive INTEGER, IsDeleted INTEGER);");


            _connection.Execute("INSERT INTO Customers.Customer VALUES (@Id, 1, 0, 0)", new { Id = _customerId });
            _connection.Execute("INSERT INTO Admin.Branch VALUES (@Id, 0)", new { Id = _branchId });

            _handler = new CreateLoanCommandHandler(_connection, NullLogger<CreateLoanCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private CreateLoanCommand ValidCommand() => new()
        {
            CustomerId = _customerId,
            BranchId = _branchId,
            CurrencyId = _currencyId,
            PrincipalAmount = 12000,
            InterestRate = 3.5m,
            TermMonths = 12,
        };

        private int LoanCount() => _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Loans.Loan");

        [Test]
        public void Handle_WithZeroPrincipal_ThrowsValidationException() =>
            AssertInvalid(c => c.PrincipalAmount = 0, "PrincipalAmount");

        [Test]
        public void Handle_WithNegativePrincipal_ThrowsValidationException() =>
            AssertInvalid(c => c.PrincipalAmount = -1, "PrincipalAmount");

        [Test]
        public void Handle_WithNegativeInterestRate_ThrowsValidationException() =>
            AssertInvalid(c => c.InterestRate = -0.1m, "InterestRate");

        [Test]
        public void Handle_WithZeroTermMonths_ThrowsValidationException() =>
            AssertInvalid(c => c.TermMonths = 0, "TermMonths");

        [Test]
        public void Handle_WithEmptyCustomerId_ThrowsValidationException() =>
            AssertInvalid(c => c.CustomerId = Guid.Empty, "CustomerId");

        [TestCase(101)]
        [TestCase(250.5)]
        public void Handle_WithAnExcessiveInterestRate_IsRejected(double rate) =>
            AssertInvalid(c => c.InterestRate = (decimal)rate, "InterestRate");

        [Test]
        public void Handle_WithTooManyDecimalsInTheRate_IsRejected() =>
            AssertInvalid(c => c.InterestRate = 12.345m, "InterestRate");

        [Test]
        public void Handle_WithATermOverThirtyYears_IsRejected() =>
            AssertInvalid(c => c.TermMonths = 361, "TermMonths");

        [Test]
        public void Handle_WithFractionsOfACent_IsRejected() =>
            AssertInvalid(c => c.PrincipalAmount = 1000.005m, "PrincipalAmount");

        [Test]
        public void Handle_WithAnAbsurdPrincipal_IsRejected() =>
            AssertInvalid(c => c.PrincipalAmount = 100_000_001m, "PrincipalAmount");

        [Test]
        public void Handle_ForAnUnknownCustomer_IsNotFound()
        {
            CreateLoanCommand command = ValidCommand();
            command.CustomerId = Guid.NewGuid();

            Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            LoanCount().Should().Be(0);
        }

        [Test]
        public void Handle_ForASoftDeletedCustomer_IsNotFound()
        {
            _connection.Execute("UPDATE Customers.Customer SET IsDeleted = 1");

            Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(ValidCommand(), CancellationToken.None));
        }

        [Test]
        public void Handle_ForABlacklistedCustomer_IsRefused()
        {
            _connection.Execute("UPDATE Customers.Customer SET IsBlacklisted = 1");

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(ValidCommand(), CancellationToken.None))!;

            exception.Message.Should().Contain("blacklisted");
            LoanCount().Should().Be(0);
        }

        [Test]
        public void Handle_ForASuspendedCustomer_IsRefused()
        {
            _connection.Execute("UPDATE Customers.Customer SET Status = 3");

            Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(ValidCommand(), CancellationToken.None));
        }

        [Test]
        public void Handle_ForAnUnknownBranch_IsRejected()
        {
            CreateLoanCommand command = ValidCommand();
            command.BranchId = Guid.NewGuid();

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().ContainSingle(m => m.Contains("branch"));
        }

        [Test]
        public void Handle_WithEmptyBranchId_ThrowsValidationException() =>
            AssertInvalid(c => c.BranchId = Guid.Empty, "BranchId");

        [Test]
        public void Handle_WithSeveralInvalidFields_ReportsAllOfThem()
        {
            CreateLoanCommand command = ValidCommand();
            command.PrincipalAmount = 0;
            command.TermMonths = 0;

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().HaveCount(2);
        }

        [Test]
        public void Handle_WhenInsertFails_WrapsTheDatabaseError()
        {
            _connection.Execute("DROP TABLE Loans.Loan");

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(ValidCommand(), CancellationToken.None))!;

            exception.InnerException.Should().NotBeNull();
        }

        private void AssertInvalid(Action<CreateLoanCommand> breakIt, string expectedField)
        {
            CreateLoanCommand command = ValidCommand();
            breakIt(command);

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().ContainSingle(m => m.Contains(expectedField));
            LoanCount().Should().Be(0);
        }
    }
}
