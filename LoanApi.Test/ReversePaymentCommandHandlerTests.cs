using Application.Exceptions;
using Application.Features.Payments.Commands;
using Application.Features.Payments.Handlers;
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
    public class ReversePaymentCommandHandlerTests
    {
        private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
        private const int TermMonths = 12;
        private const decimal Installment = 1100m;
        private const decimal Principal = 10000m;
        private const decimal TotalInterest = 3200m;

        private SqliteConnection _connection = null!;
        private CreatePaymentCommandHandler _create = null!;
        private ReversePaymentCommandHandler _reverse = null!;
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

                CREATE TABLE Loans.Loan (
                    Id TEXT, CurrencyId TEXT, LoanStatus INTEGER, PrincipalAmount NUMERIC, PrincipalRemaining NUMERIC, PrincipalPaid NUMERIC,
                    PaidInterest NUMERIC, RemainingInterest NUMERIC, TotalInterestAmount NUMERIC, AmortizationBalance NUMERIC,
                    LastPaymentDate TEXT, UpdatedOn TEXT, IsDeleted INTEGER,
                    TermMonths INTEGER, DisbursementDate TEXT, MaturityDate TEXT, NextPaymentDueDate TEXT,
                    InstallmentAmount NUMERIC, DaysOverdue INTEGER, DelinquencyPercentage NUMERIC,
                    LateFeesCharged NUMERIC DEFAULT 1000);

                CREATE TABLE Loans.DelinquencyPolicy (DailyPenaltyRate NUMERIC, IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);

                CREATE TABLE Loans.LoanPayment (
                    Id TEXT, LoanId TEXT, CurrencyId TEXT, PaymentDate TEXT, PrincipalAmount NUMERIC, InterestAmount NUMERIC,
                    PenaltyAmount NUMERIC, TotalPaymentAmount NUMERIC, PaymentMethod INTEGER, ReferenceNumber TEXT,
                    Status INTEGER, CreatedOn TEXT, CreatedById TEXT, IsActive INTEGER, IsDeleted INTEGER,
                    ReversalReason TEXT, ReversedOn TEXT, UpdatedOn TEXT);");

            _loanId = Guid.NewGuid();
            SeedLoan(LoanStatus.Defaulted, monthsAgo: 3);

            _create = new CreatePaymentCommandHandler(_connection, NullLogger<CreatePaymentCommandHandler>.Instance);
            _reverse = new ReversePaymentCommandHandler(_connection, NullLogger<ReversePaymentCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        /// <summary>A 12-month loan with nothing paid yet, disbursed some months ago.</summary>
        private void SeedLoan(LoanStatus status, int monthsAgo)
        {
            DateOnly disbursement = Today.AddMonths(-monthsAgo);
            DateOnly nextDue = disbursement.AddMonths(1);

            _connection.Execute("DELETE FROM Loans.Loan");
            _connection.Execute(
                @"INSERT INTO Loans.Loan
                    (Id, CurrencyId, LoanStatus, PrincipalAmount, PrincipalRemaining, PrincipalPaid, PaidInterest, RemainingInterest,
                     TotalInterestAmount, AmortizationBalance, IsDeleted, TermMonths, DisbursementDate, MaturityDate, NextPaymentDueDate,
                     InstallmentAmount, DaysOverdue, DelinquencyPercentage)
                  VALUES
                    (@Id, @Currency, @Status, @Principal, @Principal, 0, 0, @Interest, @Interest, @Principal, 0, @Term,
                     @Disbursement, @Maturity, @NextDue, @Installment, @DaysOverdue, 4.5)",
                new
                {
                    Id = _loanId,
                    Currency = Guid.NewGuid(),
                    Status = (int)status,
                    Principal,
                    Interest = TotalInterest,
                    Term = TermMonths,
                    Disbursement = disbursement.ToString("yyyy-MM-dd"),
                    Maturity = disbursement.AddMonths(TermMonths).ToString("yyyy-MM-dd"),
                    NextDue = nextDue.ToString("yyyy-MM-dd"),
                    Installment,
                    DaysOverdue = Math.Max(0, Today.DayNumber - nextDue.DayNumber),
                });
        }

        private async Task<LoanPaymentResponse> Pay(decimal principal, decimal interest, decimal penalty = 0) =>
            await _create.Handle(new CreatePaymentCommand
            {
                LoanId = _loanId,
                PrincipalAmount = principal,
                InterestAmount = interest,
                PenaltyAmount = penalty,
                PaymentMethod = PaymentMethod.Cash,
            }, CancellationToken.None);

        private Task<LoanPaymentResponse> Reverse(Guid paymentId, string reason = "Pago registrado por error") =>
            _reverse.Handle(new ReversePaymentCommand { Id = paymentId, Reason = reason }, CancellationToken.None);

        private LoanState State()
        {
            dynamic row = _connection.QuerySingle("SELECT * FROM Loans.Loan WHERE Id = @Id", new { Id = _loanId });
            return new LoanState(
                Convert.ToDecimal((object)row.PrincipalPaid), Convert.ToDecimal((object)row.PrincipalRemaining),
                Convert.ToDecimal((object)row.PaidInterest), Convert.ToDecimal((object)row.RemainingInterest),
                Convert.ToDecimal((object)row.AmortizationBalance), (string)row.NextPaymentDueDate,
                (int)(long)row.DaysOverdue, (LoanStatus)(int)(long)row.LoanStatus, (string?)row.LastPaymentDate);
        }

        private sealed record LoanState(
            decimal PrincipalPaid, decimal PrincipalRemaining, decimal PaidInterest, decimal RemainingInterest,
            decimal AmortizationBalance, string NextPaymentDueDate, int DaysOverdue, LoanStatus Status, string? LastPaymentDate);

        // ------------------------------------------------------------------ the inverse

        [Test]
        public async Task ReversingAPayment_RestoresTheLoanExactlyToWhatItWasBefore()
        {
            LoanState before = State();
            LoanPaymentResponse payment = await Pay(1000, 100);
            State().Should().NotBe(before);

            await Reverse(payment.Id);

            State().Should().Be(before);
        }

        [Test]
        public async Task TheReversedPaymentStaysInTheHistory_WithItsReason()
        {
            LoanPaymentResponse payment = await Pay(1000, 100);

            LoanPaymentResponse result = await Reverse(payment.Id, "  Cheque devuelto por el banco  ");

            result.Status.Should().Be(PaymentStatus.Reversed);
            result.ReversalReason.Should().Be("Cheque devuelto por el banco");
            result.TotalPaymentAmount.Should().Be(1100);
            _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Loans.LoanPayment").Should().Be(1, "it is not deleted");
            _connection.ExecuteScalar<string?>("SELECT ReversedOn FROM Loans.LoanPayment").Should().NotBeNull();
        }

        [Test]
        public async Task ReversingTheCatchUpPayment_MakesTheLoanDelinquentAgain()
        {
            LoanState before = State();
            LoanPaymentResponse catchUp = await Pay(3000, 300);
            State().Status.Should().Be(LoanStatus.Active);
            State().DaysOverdue.Should().Be(0);

            await Reverse(catchUp.Id);

            LoanState after = State();
            after.Status.Should().Be(LoanStatus.Defaulted);
            after.DaysOverdue.Should().Be(before.DaysOverdue).And.BeGreaterThan(0);
            after.NextPaymentDueDate.Should().Be(before.NextPaymentDueDate);
        }

        [Test]
        public async Task ReversingTheFinalPayment_ReopensASettledLoan()
        {
            SeedLoan(LoanStatus.Active, monthsAgo: 1);
            LoanPaymentResponse payoff = await Pay(Principal, 0);
            State().Status.Should().Be(LoanStatus.FullyPaid);

            await Reverse(payoff.Id);

            LoanState after = State();
            after.Status.Should().Be(LoanStatus.Active, "it is on schedule again");
            after.PrincipalRemaining.Should().Be(Principal);
        }

        [Test]
        public async Task ReopeningASettledLoanThatFellBehind_MakesItDelinquent()
        {
            LoanPaymentResponse payoff = await Pay(Principal, 0);
            State().Status.Should().Be(LoanStatus.FullyPaid);

            await Reverse(payoff.Id);

            State().Status.Should().Be(LoanStatus.Defaulted);
        }

        [Test]
        public async Task ReversingAnEarlierPayment_KeepsTheLaterOnes()
        {
            LoanPaymentResponse first = await Pay(1000, 100);
            await Task.Delay(30);
            LoanPaymentResponse second = await Pay(1000, 100);
            string lastPaymentOfTheSecond = State().LastPaymentDate!;

            await Reverse(first.Id);

            LoanState after = State();
            after.PrincipalPaid.Should().Be(1000);
            after.PaidInterest.Should().Be(100);
            after.NextPaymentDueDate.Should().Be(Today.AddMonths(-3).AddMonths(2).ToString("yyyy-MM-dd"), "one installment is still covered");
            after.LastPaymentDate.Should().NotBeNull();
            _connection.ExecuteScalar<int>("SELECT Status FROM Loans.LoanPayment WHERE Id = @Id", new { Id = second.Id }).Should().Be((int)PaymentStatus.Completed);
            lastPaymentOfTheSecond.Should().NotBeNull();
        }

        [Test]
        public async Task ReversingAPenaltyOnlyPayment_ChangesNoBalance()
        {
            LoanState before = State();
            LoanPaymentResponse penalty = await Pay(0, 0, penalty: 50);

            await Reverse(penalty.Id);

            State().Should().Be(before);
        }

        [Test]
        public async Task AfterAReversal_APaymentCanBeRegisteredAgain()
        {
            LoanPaymentResponse payment = await Pay(1000, 100);
            await Reverse(payment.Id);

            await Pay(1000, 100);

            State().PrincipalPaid.Should().Be(1000);
            _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Loans.LoanPayment WHERE Status = 1").Should().Be(1);
        }

        // ------------------------------------------------------------------ refusals

        [Test]
        public async Task APaymentCannotBeReversedTwice()
        {
            LoanPaymentResponse payment = await Pay(1000, 100);
            await Reverse(payment.Id);
            LoanState afterFirst = State();

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => Reverse(payment.Id))!;

            exception.Message.Should().Contain("Reversed");
            State().Should().Be(afterFirst, "a second reversal must not change the loan again");
        }

        [Test]
        public void UnknownPayment_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => Reverse(Guid.NewGuid()));
        }

        [TestCase(LoanStatus.Closed)]
        [TestCase(LoanStatus.WrittenOff)]
        public async Task ThePaymentsOfAClosedOrWrittenOffLoan_AreFinal(LoanStatus status)
        {
            LoanPaymentResponse payment = await Pay(1000, 100);
            _connection.Execute("UPDATE Loans.Loan SET LoanStatus = @Status", new { Status = (int)status });

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => Reverse(payment.Id))!;

            exception.Message.Should().Contain(status.ToString());
            _connection.ExecuteScalar<int>("SELECT Status FROM Loans.LoanPayment").Should().Be((int)PaymentStatus.Completed);
        }

        [TestCase("")]
        [TestCase("    ")]
        [TestCase("abc")]
        public async Task AReasonIsRequired(string reason)
        {
            LoanPaymentResponse payment = await Pay(1000, 100);

            Assert.ThrowsAsync<ValidationException>(() => Reverse(payment.Id, reason));

            _connection.ExecuteScalar<int>("SELECT Status FROM Loans.LoanPayment").Should().Be((int)PaymentStatus.Completed);
        }

        [Test]
        public async Task AReasonCannotBeLongerThanTheColumn()
        {
            LoanPaymentResponse payment = await Pay(1000, 100);

            Assert.ThrowsAsync<ValidationException>(() => Reverse(payment.Id, new string('x', 251)));
        }

        [Test]
        public async Task WhenTheLoanTotalsCannotAbsorbIt_NothingChanges()
        {
            LoanPaymentResponse payment = await Pay(1000, 100);
            _connection.Execute("UPDATE Loans.Loan SET PrincipalPaid = 10");
            string statusBefore = _connection.ExecuteScalar<string>("SELECT Status FROM Loans.LoanPayment")!;

            Assert.ThrowsAsync<ConflictException>(() => Reverse(payment.Id));

            _connection.ExecuteScalar<string>("SELECT Status FROM Loans.LoanPayment").Should().Be(statusBefore, "the whole reversal rolls back");
        }
    }
}
