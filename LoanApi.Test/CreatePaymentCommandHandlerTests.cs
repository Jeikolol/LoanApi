using Application.Exceptions;
using Application.Features.Payments.Commands;
using Application.Features.Payments.Handlers;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class CreatePaymentCommandHandlerTests
    {
        private SqliteConnection _connection = null!;
        private CreatePaymentCommandHandler _handler = null!;
        private Guid _loanId;
        private Guid _currencyId;

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
                    Id TEXT, CurrencyId TEXT, LoanStatus INTEGER, PrincipalRemaining NUMERIC, PrincipalPaid NUMERIC,
                    PaidInterest NUMERIC, RemainingInterest NUMERIC, AmortizationBalance NUMERIC,
                    LastPaymentDate TEXT, UpdatedOn TEXT, IsDeleted INTEGER,
                    TermMonths INTEGER, DisbursementDate TEXT, MaturityDate TEXT, NextPaymentDueDate TEXT,
                    InstallmentAmount NUMERIC, DaysOverdue INTEGER, DelinquencyPercentage NUMERIC,
                    LateFeesCharged NUMERIC DEFAULT 100);

                CREATE TABLE Loans.DelinquencyPolicy (DailyPenaltyRate NUMERIC, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);

                CREATE TABLE Loans.LoanPayment (
                    Id TEXT, LoanId TEXT, CurrencyId TEXT, PaymentDate TEXT, PrincipalAmount NUMERIC, InterestAmount NUMERIC,
                    PenaltyAmount NUMERIC, TotalPaymentAmount NUMERIC, PaymentMethod INTEGER, ReferenceNumber TEXT,
                    Status INTEGER, CreatedOn TEXT, CreatedById TEXT, IsActive INTEGER, IsDeleted INTEGER);");

            _loanId = Guid.NewGuid();
            _currencyId = Guid.NewGuid();
            SeedLoan(LoanStatus.Active, principalRemaining: 10000);

            _handler = new CreatePaymentCommandHandler(_connection, NullLogger<CreatePaymentCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private const int TermMonths = 12;
        private const decimal Installment = 1100m;

        /// <summary>A 12-month loan disbursed some months ago whose first installment is still unpaid.</summary>
        private void SeedLoan(LoanStatus status, decimal principalRemaining, int monthsAgo = 1)
        {
            DateOnly disbursement = Today.AddMonths(-monthsAgo);
            DateOnly nextDue = disbursement.AddMonths(1);

            _connection.Execute("DELETE FROM Loans.Loan");
            _connection.Execute(
                @"INSERT INTO Loans.Loan
                    (Id, CurrencyId, LoanStatus, PrincipalRemaining, PrincipalPaid, PaidInterest, RemainingInterest,
                     AmortizationBalance, IsDeleted, TermMonths, DisbursementDate, MaturityDate, NextPaymentDueDate,
                     InstallmentAmount, DaysOverdue, DelinquencyPercentage)
                  VALUES
                    (@Id, @CurrencyId, @Status, @Remaining, 0, 0, 500, @Remaining, 0, @Term, @Disbursement, @Maturity, @NextDue,
                     @Installment, @DaysOverdue, 4.5)",
                new
                {
                    Id = _loanId,
                    CurrencyId = _currencyId,
                    Status = (int)status,
                    Remaining = principalRemaining,
                    Term = TermMonths,
                    Disbursement = disbursement.ToString("yyyy-MM-dd"),
                    Maturity = disbursement.AddMonths(TermMonths).ToString("yyyy-MM-dd"),
                    NextDue = nextDue.ToString("yyyy-MM-dd"),
                    Installment,
                    DaysOverdue = Math.Max(0, Today.DayNumber - nextDue.DayNumber),
                });
        }

        private DateOnly NextDueValue() =>
            DateOnly.Parse(_connection.ExecuteScalar<string>("SELECT NextPaymentDueDate FROM Loans.Loan WHERE Id = @Id", new { Id = _loanId })!);

        private CreatePaymentCommand Command(decimal principal = 1000, decimal interest = 100, decimal penalty = 0) => new()
        {
            LoanId = _loanId,
            PrincipalAmount = principal,
            InterestAmount = interest,
            PenaltyAmount = penalty,
            PaymentMethod = PaymentMethod.Cash,
            ReferenceNumber = "REF-1",
        };

        private int PaymentCount() => _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Loans.LoanPayment");

        private decimal LoanValue(string column) =>
            _connection.ExecuteScalar<decimal>($"SELECT {column} FROM Loans.Loan WHERE Id = @Id", new { Id = _loanId });

        private int LoanStatusValue() => _connection.ExecuteScalar<int>("SELECT LoanStatus FROM Loans.Loan WHERE Id = @Id", new { Id = _loanId });

        [Test]
        public void Handle_PenaltyAboveTheFeesOwed_IsRejected()
        {
            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(Command(penalty: 100.01m), CancellationToken.None));
            PaymentCount().Should().Be(0);
        }

        [Test]
        public async Task Handle_PenaltiesAlreadyPaid_ReduceWhatIsOwed()
        {
            await _handler.Handle(Command(penalty: 60), CancellationToken.None);

            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(Command(principal: 10, interest: 0, penalty: 40.01m), CancellationToken.None));
            await _handler.Handle(Command(principal: 10, interest: 0, penalty: 40), CancellationToken.None);
        }

        [Test]
        public void Handle_PenaltyWithNoFeesCharged_IsRejected()
        {
            _connection.Execute("UPDATE Loans.Loan SET LateFeesCharged = 0");

            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(Command(penalty: 1), CancellationToken.None));
        }

        [Test]
        public async Task Handle_WithValidPayment_RecordsItAndUpdatesTheLoan()
        {
            var result = await _handler.Handle(Command(principal: 1000, interest: 100, penalty: 25), CancellationToken.None);

            result.LoanId.Should().Be(_loanId);
            result.TotalPaymentAmount.Should().Be(1125);
            result.PaymentMethod.Should().Be("Cash");
            result.Status.Should().Be(PaymentStatus.Completed);
            PaymentCount().Should().Be(1);

            LoanValue("PrincipalRemaining").Should().Be(9000);
            LoanValue("PrincipalPaid").Should().Be(1000);
            LoanValue("PaidInterest").Should().Be(100);
            LoanValue("RemainingInterest").Should().Be(400);
            LoanStatusValue().Should().Be((int)LoanStatus.Active);
        }

        [Test]
        public async Task Handle_UsesTheCurrencyOfTheLoan()
        {
            await _handler.Handle(Command(), CancellationToken.None);

            string currency = _connection.ExecuteScalar<string>("SELECT CurrencyId FROM Loans.LoanPayment");
            Guid.Parse(currency).Should().Be(_currencyId);
        }

        [Test]
        public async Task Handle_WhenPrincipalIsFullyPaid_MarksTheLoanFullyPaid()
        {
            await _handler.Handle(Command(principal: 10000, interest: 0), CancellationToken.None);

            LoanValue("PrincipalRemaining").Should().Be(0);
            LoanStatusValue().Should().Be((int)LoanStatus.FullyPaid);
        }

        [Test]
        public async Task Handle_CoveringAnInstallment_MovesTheDueDateOneMonthForward()
        {
            SeedLoan(LoanStatus.Defaulted, principalRemaining: 10000, monthsAgo: 3);
            DateOnly disbursement = Today.AddMonths(-3);

            await _handler.Handle(Command(principal: 1000, interest: 100), CancellationToken.None);

            NextDueValue().Should().Be(disbursement.AddMonths(2));
            LoanValue("DaysOverdue").Should().Be(Today.DayNumber - disbursement.AddMonths(2).DayNumber);
            LoanStatusValue().Should().Be((int)LoanStatus.Defaulted, "it is still behind on the schedule");
        }

        [Test]
        public async Task Handle_CatchingUp_ClearsTheDelinquencyAndReactivatesTheLoan()
        {
            SeedLoan(LoanStatus.Defaulted, principalRemaining: 10000, monthsAgo: 3);

            await _handler.Handle(Command(principal: 3000, interest: 300), CancellationToken.None);

            NextDueValue().Should().Be(Today.AddMonths(-3).AddMonths(4));
            LoanValue("DaysOverdue").Should().Be(0);
            LoanValue("DelinquencyPercentage").Should().Be(0);
            LoanStatusValue().Should().Be((int)LoanStatus.Active);
        }

        [Test]
        public async Task Handle_APenaltyAlone_DoesNotAdvanceTheSchedule()
        {
            SeedLoan(LoanStatus.Defaulted, principalRemaining: 10000, monthsAgo: 3);
            DateOnly before = NextDueValue();

            await _handler.Handle(Command(principal: 0, interest: 0, penalty: 50), CancellationToken.None);

            NextDueValue().Should().Be(before);
            LoanStatusValue().Should().Be((int)LoanStatus.Defaulted);
        }

        [Test]
        public async Task Handle_SettlingTheLoan_SetsTheDueDateToMaturityWithNothingOverdue()
        {
            SeedLoan(LoanStatus.Defaulted, principalRemaining: 10000, monthsAgo: 3);

            await _handler.Handle(Command(principal: 10000, interest: 0), CancellationToken.None);

            NextDueValue().Should().Be(Today.AddMonths(-3).AddMonths(TermMonths));
            LoanValue("DaysOverdue").Should().Be(0);
            LoanStatusValue().Should().Be((int)LoanStatus.FullyPaid);
        }

        [Test]
        public async Task Handle_WithInterestOnly_DoesNotDriveRemainingInterestNegative()
        {
            await _handler.Handle(Command(principal: 0, interest: 900), CancellationToken.None);

            LoanValue("RemainingInterest").Should().Be(0);
            LoanValue("PaidInterest").Should().Be(900);
            LoanValue("PrincipalRemaining").Should().Be(10000);
        }

        [Test]
        public void Handle_WhenPrincipalExceedsWhatIsOwed_IsRejectedAndChangesNothing()
        {
            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(Command(principal: 10000.01m), CancellationToken.None))!;

            exception.ErrorMessages.Should().ContainSingle(m => m.Contains("remaining principal"));
            PaymentCount().Should().Be(0);
            LoanValue("PrincipalRemaining").Should().Be(10000);
        }

        [Test]
        public async Task Handle_TwoPaymentsThatTogetherExceedTheBalance_RejectsTheSecond()
        {
            await _handler.Handle(Command(principal: 6000, interest: 0), CancellationToken.None);

            Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(Command(principal: 6000, interest: 0), CancellationToken.None));

            PaymentCount().Should().Be(1);
            LoanValue("PrincipalRemaining").Should().Be(4000);
        }

        [Test]
        public void Handle_WithUnknownLoan_ThrowsNotFound()
        {
            CreatePaymentCommand command = Command();
            command.LoanId = Guid.NewGuid();

            Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            PaymentCount().Should().Be(0);
        }

        [TestCase(LoanStatus.Pending)]
        [TestCase(LoanStatus.Approved)]
        [TestCase(LoanStatus.FullyPaid)]
        [TestCase(LoanStatus.WrittenOff)]
        [TestCase(LoanStatus.Closed)]
        public void Handle_OnALoanThatCannotReceivePayments_ThrowsConflict(LoanStatus status)
        {
            SeedLoan(status, principalRemaining: 10000);

            Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(Command(), CancellationToken.None));
            PaymentCount().Should().Be(0);
        }

        [TestCase(LoanStatus.Disbursed)]
        [TestCase(LoanStatus.Active)]
        [TestCase(LoanStatus.PartiallyPaid)]
        [TestCase(LoanStatus.Defaulted)]
        [TestCase(LoanStatus.Restructured)]
        public async Task Handle_OnAPayableLoan_IsAccepted(LoanStatus status)
        {
            SeedLoan(status, principalRemaining: 10000);

            await _handler.Handle(Command(), CancellationToken.None);

            PaymentCount().Should().Be(1);
        }

        [Test]
        public void Handle_WithNegativeAmount_IsRejected() =>
            AssertInvalid(c => c.InterestAmount = -1, "InterestAmount");

        [Test]
        public void Handle_WithMoreThanTwoDecimals_IsRejected() =>
            AssertInvalid(c => c.PrincipalAmount = 10.005m, "PrincipalAmount");

        [Test]
        public void Handle_WithAZeroTotal_IsRejected() =>
            AssertInvalid(c => { c.PrincipalAmount = 0; c.InterestAmount = 0; c.PenaltyAmount = 0; }, "greater than zero");

        [Test]
        public void Handle_WithAnEmptyLoanId_IsRejected() =>
            AssertInvalid(c => c.LoanId = Guid.Empty, "LoanId");

        [Test]
        public void Handle_WithAnUnknownPaymentMethod_IsRejected() =>
            AssertInvalid(c => c.PaymentMethod = (PaymentMethod)99, "PaymentMethod");

        [Test]
        public void Handle_WithATooLongReference_IsRejected() =>
            AssertInvalid(c => c.ReferenceNumber = new string('x', 101), "ReferenceNumber");

        private void AssertInvalid(Action<CreatePaymentCommand> breakIt, string expected)
        {
            CreatePaymentCommand command = Command();
            breakIt(command);

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains(expected));
            PaymentCount().Should().Be(0);
        }
    }
}
