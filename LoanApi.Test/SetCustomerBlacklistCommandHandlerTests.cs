using Application.Exceptions;
using Application.Features.Customers.Commands;
using Application.Features.Customers.Handlers;
using Dapper;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class SetCustomerBlacklistCommandHandlerTests
    {
        private SqliteConnection _connection = null!;
        private SetCustomerBlacklistCommandHandler _handler = null!;
        private Guid _customerId;

        [OneTimeSetUp]
        public void RegisterHandlers() => SqlMapper.AddTypeHandler(new SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Customers;
                CREATE TABLE Customers.Customer (
                    Id TEXT, CustomerType INTEGER, FirstName TEXT, LastName TEXT, CompanyName TEXT, IdentificationTypeId TEXT,
                    Identification TEXT, TaxId TEXT, Phone TEXT, AlternativePhone TEXT, Email TEXT, Address TEXT, Province TEXT,
                    CountryId TEXT, Status INTEGER, Occupation TEXT, AnnualIncome NUMERIC, IncomeSource INTEGER, CreditLimit NUMERIC,
                    RiskLevel INTEGER, IsBlacklisted INTEGER, BlacklistReason TEXT, EmergencyContactName TEXT,
                    EmergencyContactPhone TEXT, CreatedOn TEXT, UpdatedOn TEXT, IsDeleted INTEGER);");

            _customerId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Customers.Customer (Id, IsBlacklisted, IsDeleted) VALUES (@Id, 0, 0)", new { Id = _customerId });
            _handler = new SetCustomerBlacklistCommandHandler(_connection, NullLogger<SetCustomerBlacklistCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        /// <summary>The read-back builds a positional record SQLite cannot materialize; the change itself is asserted through SQL.</summary>
        private async Task Run(bool blacklisted, string? reason = null, Guid? id = null)
        {
            try
            {
                await _handler.Handle(new SetCustomerBlacklistCommand { Id = id ?? _customerId, Blacklisted = blacklisted, Reason = reason }, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private (bool Blacklisted, string? Reason) State()
        {
            dynamic row = _connection.QuerySingle("SELECT IsBlacklisted, BlacklistReason FROM Customers.Customer");
            return ((long)row.IsBlacklisted == 1, (string?)row.BlacklistReason);
        }

        [Test]
        public async Task Blacklisting_StoresTheTrimmedReason()
        {
            await Run(true, "  Fraude documentado  ");

            State().Should().Be((true, "Fraude documentado"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("abc")]
        public void Blacklisting_WithoutAReason_IsRejected(string? reason)
        {
            Assert.ThrowsAsync<ValidationException>(() => Run(true, reason));
            State().Blacklisted.Should().BeFalse();
        }

        [Test]
        public void Blacklisting_WithAReasonThatIsTooLong_IsRejected()
        {
            Assert.ThrowsAsync<ValidationException>(() => Run(true, new string('x', 251)));
        }

        [Test]
        public async Task Removing_ClearsTheReason()
        {
            await Run(true, "Fraude documentado");

            await Run(false);

            State().Should().Be((false, null));
        }

        [Test]
        public async Task Blacklisting_TwiceIsAConflict()
        {
            await Run(true, "Fraude documentado");

            Assert.ThrowsAsync<ConflictException>(() => Run(true, "Fraude documentado"));
        }

        [Test]
        public void Removing_SomeoneNotBlacklisted_IsAConflict()
        {
            Assert.ThrowsAsync<ConflictException>(() => Run(false));
        }

        [Test]
        public void UnknownCustomer_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => Run(true, "Fraude documentado", Guid.NewGuid()));
        }

        [Test]
        public void ADeletedCustomer_IsNotFound()
        {
            _connection.Execute("UPDATE Customers.Customer SET IsDeleted = 1");

            Assert.ThrowsAsync<NotFoundException>(() => Run(true, "Fraude documentado"));
        }
    }
}
