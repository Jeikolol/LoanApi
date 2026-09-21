using Application.Features.Delinquency;
using Application.Features.Delinquency.Commands;
using Application.Features.Delinquency.Handlers;
using Application.Features.Loans.Commands;
using Application.Features.Customers.Commands;
using Application.Features.Dashboard.Handlers;
using Application.Features.Dashboard.Queries;
using Application.Features.Customers.Handlers;
using Application.Features.Loans.Handlers;
using Application.Features.Loans.Queries;
using Application.Features.Payments.Commands;
using Application.Features.Payments.Handlers;
using Application.Features.Payments.Queries;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence;

namespace LoanApi.Test
{
    /// <summary>
    /// Runs the money handlers against a real SQL Server, on a throwaway database built from the real migrations
    /// and seed data (SQLite cannot catch T-SQL or constraint differences). Opt in by setting LOANWEB_TEST_SQL to a
    /// connection string that may create databases, e.g.
    /// "Server=MSI;Integrated Security=true;TrustServerCertificate=true".
    /// </summary>
    [TestFixture]
    [Category("SqlServer")]
    [NonParallelizable]
    public class SqlServerIntegrationTests
    {
        private string? _server;
        private string _database = string.Empty;
        private SqlConnection _connection = null!;

        private string ConnectionString(string database) =>
            new SqlConnectionStringBuilder(_server!) { InitialCatalog = database }.ConnectionString;

        [OneTimeSetUp]
        public async Task CreateDatabase()
        {
            _server = Environment.GetEnvironmentVariable("LOANWEB_TEST_SQL");
            if (string.IsNullOrWhiteSpace(_server))
            {
                Assert.Ignore("Set LOANWEB_TEST_SQL to run the SQL Server integration tests.");
            }

            _database = $"LoanWebIntegration_{Guid.NewGuid():N}";
            // The SQLite fixtures register global handlers (Guid as text) that would break SQL Server; they re-register their own.
            SqlMapper.ResetTypeHandlers();
            SqlMapper.AddTypeHandler(new DateOnlyTestHandler());

            DbContextOptions<LoanDbContext> options = new DbContextOptionsBuilder<LoanDbContext>()
                .UseSqlServer(ConnectionString(_database), b => b.MigrationsAssembly("Migrations"))
                .Options;

            _connection = new SqlConnection(ConnectionString(_database));

            await using LoanDbContext context = new(options);
            await context.Database.MigrateAsync();

            await _connection.OpenAsync();
            await new ApplicationDbSeeder(_connection).SeedDatabaseAsync(context, CancellationToken.None);
        }

        [OneTimeTearDown]
        public async Task DropDatabase()
        {
            SqlMapper.ResetTypeHandlers();
            if (string.IsNullOrEmpty(_database)) return;

            await _connection.DisposeAsync();
            SqlConnection.ClearAllPools();
            await using SqlConnection master = new(ConnectionString("master"));
            await master.OpenAsync();
            await master.ExecuteAsync($"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_database}]", commandTimeout: 30);
        }

        private async Task<Guid> NewDisbursedLoan()
        {
            Guid customerId = await _connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [Customers].[Customer] WHERE IsDeleted = 0 AND IsBlacklisted = 0 AND Status = 1 ORDER BY Id");
            Guid branchId = await _connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [Admin].[Branch]");

            CreateLoanCommandHandler create = new(_connection, NullLogger<CreateLoanCommandHandler>.Instance);
            var loan = await create.Handle(
                new CreateLoanCommand { CustomerId = customerId, BranchId = branchId, PrincipalAmount = 12000, InterestRate = 12, TermMonths = 12 },
                CancellationToken.None);

            await _connection.ExecuteAsync(
                "UPDATE [Loans].[Loan] SET LoanStatus = @Status WHERE Id = @Id",
                new { Status = (int)LoanStatus.Disbursed, loan.Id });
            return loan.Id;
        }

        private Task<decimal> Value(string column, Guid loanId) =>
            _connection.ExecuteScalarAsync<decimal>($"SELECT {column} FROM [Loans].[Loan] WHERE Id = @loanId", new { loanId });

        [Test]
        public async Task ThePaymentThenItsReversal_LeavesTheLoanExactlyAsItWas()
        {
            Guid loanId = await NewDisbursedLoan();
            decimal remaining = await Value("PrincipalRemaining", loanId);
            decimal balance = await Value("AmortizationBalance", loanId);

            CreatePaymentCommandHandler pay = new(_connection, NullLogger<CreatePaymentCommandHandler>.Instance);
            var payment = await pay.Handle(
                new CreatePaymentCommand { LoanId = loanId, PrincipalAmount = 900, InterestAmount = 100, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None);

            (await Value("PrincipalRemaining", loanId)).Should().Be(remaining - 900);

            ReversePaymentCommandHandler reverse = new(_connection, NullLogger<ReversePaymentCommandHandler>.Instance);
            await reverse.Handle(new ReversePaymentCommand { Id = payment.Id, Reason = "Cheque devuelto" }, CancellationToken.None);

            (await Value("PrincipalRemaining", loanId)).Should().Be(remaining);
            (await Value("AmortizationBalance", loanId)).Should().Be(balance);
        }

        [Test]
        public async Task LateFees_AreChargedOnceAndOnlyThatMuchCanBePaid()
        {
            Guid loanId = await NewDisbursedLoan();
            // The first installment fell due about 10 days ago (the exact count depends on the month lengths).
            await _connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET DisbursementDate = DATEADD(month, -1, DATEADD(day, -10, CAST(GETUTCDATE() AS date))),
                      NextPaymentDueDate = DATEADD(month, 1, DATEADD(month, -1, DATEADD(day, -10, CAST(GETUTCDATE() AS date))))
                  WHERE Id = @loanId", new { loanId });
            int daysLate = await _connection.ExecuteScalarAsync<int>(
                "SELECT DATEDIFF(day, NextPaymentDueDate, CAST(GETUTCDATE() AS date)) FROM [Loans].[Loan] WHERE Id = @loanId", new { loanId });

            LateFeeCharger charger = new(_connection, NullLogger<LateFeeCharger>.Instance);
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

            var first = await charger.ChargeAsync(loanId, today, CancellationToken.None);
            var second = await charger.ChargeAsync(loanId, today, CancellationToken.None);

            first.DaysCharged.Should().Be(daysLate);
            second.DaysCharged.Should().Be(0);
            (await Value("LateFeesCharged", loanId)).Should().Be(first.AmountCharged);

            CreatePaymentCommandHandler pay = new(_connection, NullLogger<CreatePaymentCommandHandler>.Instance);
            Func<Task> tooMuch = () => pay.Handle(
                new CreatePaymentCommand { LoanId = loanId, PenaltyAmount = first.AmountCharged + 0.01m, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None);
            await tooMuch.Should().ThrowAsync<Application.Exceptions.ValidationException>();

            await pay.Handle(
                new CreatePaymentCommand { LoanId = loanId, PenaltyAmount = first.AmountCharged, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None);
        }

        [Test]
        public async Task ThePaymentListShowsWhoRegisteredEachPayment()
        {
            Guid loanId = await NewDisbursedLoan();
            Guid employeeId = await _connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [Admin].[Employee]");

            CreatePaymentCommandHandler pay = new(_connection, NullLogger<CreatePaymentCommandHandler>.Instance);
            await pay.Handle(
                new CreatePaymentCommand { LoanId = loanId, PrincipalAmount = 100, InterestAmount = 10, PaymentMethod = PaymentMethod.Cash, CreatedById = employeeId },
                CancellationToken.None);
            await pay.Handle(
                new CreatePaymentCommand { LoanId = loanId, PrincipalAmount = 100, InterestAmount = 10, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None);

            GetPaymentsByLoanQueryHandler list = new(_connection, NullLogger<GetPaymentsByLoanQueryHandler>.Instance);
            var page = await list.Handle(new GetPaymentsByLoanQuery(loanId, 1, 10), CancellationToken.None);

            page.Data.Should().HaveCount(2);
            page.Data.Count(p => !string.IsNullOrWhiteSpace(p.RegisteredBy)).Should().Be(1, "only the payment with an employee shows one");
        }

        [Test]
        public async Task TheLoanDetailIncludesTheBranchName()
        {
            Guid loanId = await NewDisbursedLoan();

            var detail = await new GetLoanByIdQueryHandler(_connection, NullLogger<GetLoanByIdQueryHandler>.Instance).Handle(new GetLoanByIdQuery(loanId), CancellationToken.None);

            detail.BranchName.Should().NotBeNullOrWhiteSpace();
            detail.MaxCredit.Should().BeGreaterThanOrEqualTo(0);
        }

        [Test]
        public async Task OnlyOneDelinquencyPolicyCanBeActive()
        {
            (await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM [Loans].[DelinquencyPolicy] WHERE IsActive = 1 AND IsDeleted = 0"))
                .Should().Be(1);

            Func<Task> second = () => _connection.ExecuteAsync(
                @"INSERT INTO [Loans].[DelinquencyPolicy]
                    (Id, Name, DaysUntilEarlyDelinquency, EarlyDelinquencyPenalty, DaysUntilSevereDelinquency, SevereDelinquencyPenalty,
                     DaysUntilWriteOff, AutomaticallyChargeLateFees, DailyPenaltyRate, CreatedOn, IsActive, IsDeleted)
                  VALUES (NEWID(), 'Otra', 5, 1, 30, 2, 90, 1, 0.001, GETUTCDATE(), 1, 0)");

            await second.Should().ThrowAsync<SqlException>("a second active policy would make every fee calculation ambiguous");
        }

        [Test]
        public async Task ABlacklistedCustomerCannotGetALoan_AndCanBeRestored()
        {
            Guid customerId = await _connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [Customers].[Customer] WHERE IsDeleted = 0 AND IsBlacklisted = 0 AND Status = 1 ORDER BY Id");
            Guid branchId = await _connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM [Admin].[Branch]");
            SetCustomerBlacklistCommandHandler blacklist = new(_connection, NullLogger<SetCustomerBlacklistCommandHandler>.Instance);
            CreateLoanCommandHandler create = new(_connection, NullLogger<CreateLoanCommandHandler>.Instance);
            CreateLoanCommand request = new() { CustomerId = customerId, BranchId = branchId, PrincipalAmount = 1000, InterestRate = 12, TermMonths = 6 };

            var listed = await blacklist.Handle(new SetCustomerBlacklistCommand { Id = customerId, Blacklisted = true, Reason = "Fraude documentado" }, CancellationToken.None);
            listed.IsBlacklisted.Should().BeTrue();
            listed.BlacklistReason.Should().Be("Fraude documentado");
            await create.Invoking(h => h.Handle(request, CancellationToken.None)).Should().ThrowAsync<Application.Exceptions.ConflictException>();

            var restored = await blacklist.Handle(new SetCustomerBlacklistCommand { Id = customerId, Blacklisted = false }, CancellationToken.None);
            restored.IsBlacklisted.Should().BeFalse();
            restored.BlacklistReason.Should().BeNull();
            await create.Invoking(h => h.Handle(request, CancellationToken.None)).Should().NotThrowAsync();
        }

        [Test]
        public async Task AnOverdueLoanIsWrittenOffOnlyOnPurpose_AndThePercentageFollowsTheRate()
        {
            Guid loanId = await NewDisbursedLoan();
            await _connection.ExecuteAsync(
                "UPDATE [Loans].[Loan] SET NextPaymentDueDate = DATEADD(day, -120, CAST(GETUTCDATE() AS date)), LoanStatus = @Defaulted WHERE Id = @loanId",
                new { loanId, Defaulted = (int)LoanStatus.Defaulted });

            // A recalculation flags it delinquent (days x rate) but never writes it off.
            await new CalculateDelinquencyCommandHandler(_connection, NullLogger<CalculateDelinquencyCommandHandler>.Instance, new Application.Services.DelinquencyService())
                .Handle(new CalculateDelinquencyCommand(loanId), CancellationToken.None);
            (await Value("LoanStatus", loanId)).Should().Be((decimal)(int)LoanStatus.Defaulted);
            (await Value("DelinquencyPercentage", loanId)).Should().Be(12.00m, "120 days x 0.1% x 100");

            WriteOffLoanCommandHandler writeOff = new(_connection, NullLogger<WriteOffLoanCommandHandler>.Instance);
            var result = await writeOff.Handle(new WriteOffLoanCommand { LoanId = loanId, Reason = "Incobrable tras gestión de cobro" }, CancellationToken.None);

            result.LoanStatus.Should().Be(LoanStatus.WrittenOff);
            (await Value("LoanStatus", loanId)).Should().Be((decimal)(int)LoanStatus.WrittenOff);
        }

        [Test]
        public async Task TheDashboardNumbersFollowTheirLabels()
        {
            Guid loanId = await NewDisbursedLoan();
            long before = (await new GetPortfolioSummaryQueryHandler(_connection, NullLogger<GetPortfolioSummaryQueryHandler>.Instance)
                .Handle(new GetPortfolioSummaryQuery(), CancellationToken.None)).MonthlyCollections;

            await new CreatePaymentCommandHandler(_connection, NullLogger<CreatePaymentCommandHandler>.Instance).Handle(
                new CreatePaymentCommand { LoanId = loanId, PrincipalAmount = 900, InterestAmount = 100, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None);

            var summary = await new GetPortfolioSummaryQueryHandler(_connection, NullLogger<GetPortfolioSummaryQueryHandler>.Instance)
                .Handle(new GetPortfolioSummaryQuery(), CancellationToken.None);
            summary.MonthlyCollections.Should().Be(before + 1000, "the whole month counts, not just today");
            summary.TotalPortfolio.Should().BeGreaterThan(0);

            var flow = await new GetCashFlowQueryHandler(_connection).Handle(new GetCashFlowQuery(), CancellationToken.None);
            flow.Labels.Should().HaveCount(4);
            flow.Collections[^1].Should().BeGreaterThanOrEqualTo(1000, "the latest week is last and holds today's payment");
        }

        [Test]
        public async Task ALoanSeveralInstallmentsBehind_PaysTheFeeOfEachOne()
        {
            Guid loanId = await NewDisbursedLoan();
            // Disbursed 100 days ago with nothing paid: three installments are already due.
            await _connection.ExecuteAsync(
                @"UPDATE [Loans].[Loan]
                  SET DisbursementDate = DATEADD(day, -100, CAST(GETUTCDATE() AS date)),
                      NextPaymentDueDate = DATEADD(month, 1, DATEADD(day, -100, CAST(GETUTCDATE() AS date)))
                  WHERE Id = @loanId", new { loanId });
            decimal installment = await Value("InstallmentAmount", loanId);
            int daysSinceFirstDue = await _connection.ExecuteScalarAsync<int>(
                "SELECT DATEDIFF(day, NextPaymentDueDate, CAST(GETUTCDATE() AS date)) FROM [Loans].[Loan] WHERE Id = @loanId", new { loanId });

            var result = await new LateFeeCharger(_connection, NullLogger<LateFeeCharger>.Instance)
                .ChargeAsync(loanId, DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);

            decimal oneInstallmentOnly = daysSinceFirstDue * LateFeeRules.DailyFee(installment, LateFeeCharger.DefaultDailyRate);
            result.DaysCharged.Should().Be(daysSinceFirstDue);
            result.AmountCharged.Should().BeGreaterThan(oneInstallmentOnly * 1.4m, "the second and third installments add their own fees");
        }

        [Test]
        public async Task TheDailyRefreshMarksALoanThatStoppedPayingAsDelinquent()
        {
            Guid loanId = await NewDisbursedLoan();
            await _connection.ExecuteAsync(
                "UPDATE [Loans].[Loan] SET NextPaymentDueDate = DATEADD(day, -25, CAST(GETUTCDATE() AS date)), LoanStatus = @Active, DaysOverdue = 0 WHERE Id = @loanId",
                new { loanId, Active = (int)LoanStatus.Active });

            var result = await new DelinquencyRefresher(_connection, NullLogger<DelinquencyRefresher>.Instance)
                .RefreshAsync(DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);

            result.NowDelinquent.Should().BeGreaterThanOrEqualTo(1);
            (await Value("LoanStatus", loanId)).Should().Be((decimal)(int)LoanStatus.Defaulted);
            (await Value("DaysOverdue", loanId)).Should().Be(25m);
            (await Value("DelinquencyPercentage", loanId)).Should().Be(2.50m, "25 days x 0.1% x 100");
        }

        private sealed class DateOnlyTestHandler : SqlMapper.TypeHandler<DateOnly>
        {
            public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);

            public override void SetValue(System.Data.IDbDataParameter parameter, DateOnly value)
            {
                parameter.DbType = System.Data.DbType.Date;
                parameter.Value = value.ToDateTime(TimeOnly.MinValue);
            }
        }
    }
}
