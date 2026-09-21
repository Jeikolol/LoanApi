using Application.Exceptions;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Handlers;
using Application.Interfaces;
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
    public class TransitionLoanCommandHandlerTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

        private SqliteConnection _connection = null!;
        private TransitionLoanCommandHandler _handler = null!;
        private Guid _customerId;
        private Guid _loanId;
        private Guid _approverId;

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
                ATTACH DATABASE ':memory:' AS Customers;

                CREATE TABLE Customers.Customer (Id TEXT, IsBlacklisted INTEGER, Status INTEGER, CreditLimit NUMERIC, IsDeleted INTEGER);
                CREATE TABLE Loans.Loan (
                    Id TEXT, LoanNumber TEXT, CustomerId TEXT, LoanStatus INTEGER, PrincipalAmount NUMERIC, PrincipalRemaining NUMERIC,
                    TermMonths INTEGER, DaysOverdue INTEGER, DisbursementDate TEXT, MaturityDate TEXT, NextPaymentDueDate TEXT,
                    UpdatedOn TEXT, IsDeleted INTEGER, CreatedById TEXT);");

            _customerId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Customers.Customer VALUES (@Id, 0, 1, 100000, 0)", new { Id = _customerId });
            _loanId = InsertLoan(LoanStatus.Pending, 30000m);

            _approverId = Guid.NewGuid();
            Mock<ICurrentUser> currentUser = new();
            currentUser.SetupGet(u => u.UserId).Returns(() => _approverId);

            _handler = new TransitionLoanCommandHandler(_connection, currentUser.Object, NullLogger<TransitionLoanCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private Guid InsertLoan(LoanStatus status, decimal principal, decimal? remaining = null, int daysOverdue = 0, Guid? createdBy = null)
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                @"INSERT INTO Loans.Loan
                    (Id, LoanNumber, CustomerId, LoanStatus, PrincipalAmount, PrincipalRemaining, TermMonths, DaysOverdue,
                     DisbursementDate, MaturityDate, NextPaymentDueDate, IsDeleted, CreatedById)
                  VALUES (@Id, @Number, @CustomerId, @Status, @Principal, @Remaining, 12, @DaysOverdue,
                          '2020-01-01', '2021-01-01', '2020-02-01', 0, @CreatedBy)",
                new
                {
                    Id = id,
                    Number = $"LOAN-{id.ToString()[..6]}",
                    CustomerId = _customerId,
                    Status = (int)status,
                    Principal = principal,
                    Remaining = remaining ?? principal,
                    DaysOverdue = daysOverdue,
                    CreatedBy = createdBy,
                });
            return id;
        }

        private TransitionLoanCommand Command(LoanAction action, Guid? loanId = null) => new() { LoanId = loanId ?? _loanId, Action = action };

        private int StatusOf(Guid id) => _connection.ExecuteScalar<int>("SELECT LoanStatus FROM Loans.Loan WHERE Id = @Id", new { Id = id });

        private string TextOf(string column) =>
            _connection.ExecuteScalar<string>($"SELECT {column} FROM Loans.Loan WHERE Id = @Id", new { Id = _loanId })!;

        [Test]
        public async Task Approve_MovesAPendingLoanToApproved()
        {
            LoanTransitionResponse result = await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            result.LoanStatus.Should().Be(LoanStatus.Approved);
            StatusOf(_loanId).Should().Be((int)LoanStatus.Approved);
        }

        [Test]
        public async Task Disburse_StartsTheScheduleToday()
        {
            await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            LoanTransitionResponse result = await _handler.Handle(Command(LoanAction.Disburse), CancellationToken.None);

            result.LoanStatus.Should().Be(LoanStatus.Disbursed);
            TextOf("DisbursementDate").Should().Be(Today.ToString("yyyy-MM-dd"));
            TextOf("NextPaymentDueDate").Should().Be(Today.AddMonths(1).ToString("yyyy-MM-dd"));
            TextOf("MaturityDate").Should().Be(Today.AddMonths(12).ToString("yyyy-MM-dd"));
        }

        [Test]
        public async Task Approve_DoesNotTouchTheDates()
        {
            await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            TextOf("DisbursementDate").Should().Be("2020-01-01");
        }

        [Test]
        public async Task Cancel_ClosesALoanThatWasNeverDisbursed()
        {
            await _handler.Handle(Command(LoanAction.Cancel), CancellationToken.None);

            StatusOf(_loanId).Should().Be((int)LoanStatus.Closed);
        }

        [Test]
        public async Task Close_SettledLoan()
        {
            Guid paid = InsertLoan(LoanStatus.FullyPaid, 5000m, remaining: 0m);

            await _handler.Handle(Command(LoanAction.Close, paid), CancellationToken.None);

            StatusOf(paid).Should().Be((int)LoanStatus.Closed);
        }

        [TestCase(LoanStatus.Approved, LoanAction.Approve)]
        [TestCase(LoanStatus.Pending, LoanAction.Disburse)]
        [TestCase(LoanStatus.Active, LoanAction.Cancel)]
        [TestCase(LoanStatus.Active, LoanAction.Close)]
        public void InvalidTransitions_AreRejectedAndChangeNothing(LoanStatus status, LoanAction action)
        {
            Guid loan = InsertLoan(status, 5000m);

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(
                () => _handler.Handle(Command(action, loan), CancellationToken.None))!;

            exception.Message.Should().Contain(status.ToString());
            StatusOf(loan).Should().Be((int)status);
        }

        [Test]
        public void UnknownLoan_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(Command(LoanAction.Approve, Guid.NewGuid()), CancellationToken.None));
        }

        [Test]
        public void SoftDeletedLoan_IsNotFound()
        {
            _connection.Execute("UPDATE Loans.Loan SET IsDeleted = 1 WHERE Id = @Id", new { Id = _loanId });

            Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(Command(LoanAction.Approve), CancellationToken.None));
        }

        [Test]
        public void UnknownAction_IsAValidationError()
        {
            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(Command((LoanAction)99), CancellationToken.None));
        }

        [Test]
        public void Approve_ForABlacklistedCustomer_IsRefused()
        {
            _connection.Execute("UPDATE Customers.Customer SET IsBlacklisted = 1");

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(LoanAction.Approve), CancellationToken.None))!;

            exception.Message.Should().Contain("blacklisted");
            StatusOf(_loanId).Should().Be((int)LoanStatus.Pending);
        }

        [Test]
        public void Approve_ForAnInactiveCustomer_IsRefused()
        {
            _connection.Execute("UPDATE Customers.Customer SET Status = 3");

            Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(LoanAction.Approve), CancellationToken.None));
        }

        [Test]
        public void Approve_WhenTheLoanWouldExceedTheCreditLimit_CountsWhatIsAlreadyOwed()
        {
            InsertLoan(LoanStatus.Active, 90000m, remaining: 80000m);

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(LoanAction.Approve), CancellationToken.None))!;

            exception.Message.Should().Contain("credit limit");
        }

        [Test]
        public async Task Approve_IgnoresLoansThatAreNoLongerOwed()
        {
            InsertLoan(LoanStatus.Closed, 90000m, remaining: 0m);
            InsertLoan(LoanStatus.FullyPaid, 90000m, remaining: 0m);
            InsertLoan(LoanStatus.Pending, 90000m);

            LoanTransitionResponse result = await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            result.LoanStatus.Should().Be(LoanStatus.Approved);
        }

        [Test]
        public void Approve_WhenACustomerLoanIsMoreThanThirtyDaysOverdue_IsRefused()
        {
            InsertLoan(LoanStatus.Defaulted, 1000m, daysOverdue: 45);

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(LoanAction.Approve), CancellationToken.None))!;

            exception.Message.Should().Contain("overdue");
        }

        [Test]
        public void Approve_ByTheEmployeeWhoCreatedTheLoan_IsRefused()
        {
            Guid own = InsertLoan(LoanStatus.Pending, 1000m, createdBy: _approverId);

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(LoanAction.Approve, own), CancellationToken.None))!;

            exception.Message.Should().Contain("created it");
            StatusOf(own).Should().Be((int)LoanStatus.Pending);
        }

        [Test]
        public async Task Approve_ByAnotherEmployee_IsAllowed()
        {
            Guid someoneElses = InsertLoan(LoanStatus.Pending, 1000m, createdBy: Guid.NewGuid());

            await _handler.Handle(Command(LoanAction.Approve, someoneElses), CancellationToken.None);

            StatusOf(someoneElses).Should().Be((int)LoanStatus.Approved);
        }

        [Test]
        public async Task Approve_OfALoanWithNoRecordedCreator_IsAllowed()
        {
            await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            StatusOf(_loanId).Should().Be((int)LoanStatus.Approved);
        }

        [Test]
        public async Task TheCreatorCanStillCancelOrDisburseTheirOwnLoan()
        {
            Guid own = InsertLoan(LoanStatus.Pending, 1000m, createdBy: _approverId);
            await _handler.Handle(Command(LoanAction.Cancel, own), CancellationToken.None);

            StatusOf(own).Should().Be((int)LoanStatus.Closed);
        }

        [Test]
        public async Task Approve_DoesNotCountTheLoanBeingApprovedAgainstItself()
        {
            _connection.Execute("UPDATE Customers.Customer SET CreditLimit = 30000");

            LoanTransitionResponse result = await _handler.Handle(Command(LoanAction.Approve), CancellationToken.None);

            result.LoanStatus.Should().Be(LoanStatus.Approved);
        }
    }
}
