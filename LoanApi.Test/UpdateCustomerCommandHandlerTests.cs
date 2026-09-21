using Application.Exceptions;
using Application.Features.Customers.Commands;
using Application.Features.Customers.Handlers;
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
    public class UpdateCustomerCommandHandlerTests
    {
        private SqliteConnection _connection = null!;
        private UpdateCustomerCommandHandler _handler = null!;
        private Guid _individualId;
        private Guid _legalId;

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
                    CountryId TEXT, NationalityCountryId TEXT, Status INTEGER, Occupation TEXT, AnnualIncome NUMERIC,
                    IncomeSource INTEGER, CreditLimit NUMERIC, RiskLevel INTEGER, IsBlacklisted INTEGER, BlacklistReason TEXT,
                    EmergencyContactName TEXT, EmergencyContactPhone TEXT, CreatedOn TEXT, UpdatedOn TEXT, CreatedById TEXT,
                    IsActive INTEGER, IsDeleted INTEGER);");

            _individualId = Insert(CustomerType.PhysicalPerson, "001-1234567-8", "+18095550001");
            _legalId = Insert(CustomerType.LegalPerson, "131-12345-6", "+18095550002");

            _handler = new UpdateCustomerCommandHandler(_connection, NullLogger<UpdateCustomerCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private Guid Insert(CustomerType type, string identification, string phone)
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                @"INSERT INTO Customers.Customer
                    (Id, CustomerType, FirstName, LastName, CompanyName, IdentificationTypeId, Identification, Phone, Address,
                     Province, CountryId, NationalityCountryId, Status, Occupation, CreditLimit, IsBlacklisted, CreatedOn,
                     IsActive, IsDeleted)
                  VALUES
                    (@Id, @Type, 'Ana', 'Perez', @Company, @Other, @Identification, @Phone, 'Calle 1', 'Santo Domingo',
                     @Other, @Other, 1, 'Docente', 50000, 0, @Now, 1, 0)",
                new
                {
                    Id = id,
                    Type = (int)type,
                    Company = type == CustomerType.LegalPerson ? "Perez SRL" : null,
                    Other = Guid.NewGuid(),
                    Identification = identification,
                    Phone = phone,
                    Now = DateTime.UtcNow,
                });
            return id;
        }

        /// <summary>
        /// Runs the handler. The final read-back materializes a positional record, which SQLite cannot satisfy
        /// (it returns Int64/String where SQL Server returns decimal/DateTime), so that one error is ignored here
        /// and the effect is asserted on the row itself.
        /// </summary>
        private async Task Apply(UpdateCustomerCommand command)
        {
            try
            {
                await _handler.Handle(command, CancellationToken.None);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("materialization"))
            {
            }
        }

        private dynamic Row(Guid id) => _connection.QuerySingle("SELECT * FROM Customers.Customer WHERE Id = @Id", new { Id = id.ToString().ToUpperInvariant() });

        private UpdateCustomerCommand ValidCommand(Guid id) => new()
        {
            Id = id,
            FirstName = "  Ana María ",
            LastName = "Pérez",
            CompanyName = "Perez SRL",
            Phone = "809-555-0100",
            Email = " ana@example.com ",
            Address = "Calle 2",
            Province = "Santiago",
            Occupation = "Ingeniera",
            AnnualIncome = 900000m,
            IncomeSource = IncomeSource.Employment,
            CreditLimit = 200000m,
        };

        [Test]
        public async Task Handle_ReplacesTheEditableProfile_AndKeepsTheIdentification()
        {
            await Apply(ValidCommand(_individualId));

            dynamic row = Row(_individualId);
            ((string)row.FirstName).Should().Be("Ana María");
            ((string)row.Phone).Should().Be("+18095550100", "a national number is stored in the canonical international form");
            ((string)row.Email).Should().Be("ana@example.com");
            ((string)row.Province).Should().Be("Santiago");
            Convert.ToDecimal((object)row.CreditLimit).Should().Be(200000m);
            Convert.ToDecimal((object)row.AnnualIncome).Should().Be(900000m);
            ((string)row.Identification).Should().Be("001-1234567-8");
            ((long)row.CustomerType).Should().Be((long)CustomerType.PhysicalPerson);
            ((object?)row.UpdatedOn).Should().NotBeNull();
        }

        [Test]
        public async Task Handle_ClearsOptionalFieldsWhenTheyAreEmpty()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.AlternativePhone = "   ";
            command.Email = null;

            await Apply(command);

            dynamic row = Row(_individualId);
            ((object?)row.Email).Should().BeNull();
            ((object?)row.AlternativePhone).Should().BeNull();
        }

        [Test]
        public async Task Handle_KeepingItsOwnPhone_IsNotADuplicate()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = "809-555-0001";

            await Apply(command);

            ((string)Row(_individualId).Province).Should().Be("Santiago");
        }

        [TestCase("809-555-0002")]
        [TestCase("(809) 555-0002")]
        [TestCase("8095550002")]
        [TestCase("1-809-555-0002")]
        [TestCase("+1 809 555 0002")]
        public async Task Handle_WithAnotherSpellingOfAnExistingPhone_IsStillADuplicate(string spelling)
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = spelling;

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Test]
        public async Task Handle_WithAForeignNumber_KeepsItsCountryCode()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = "+57 300 123 4567";
            command.AlternativePhone = "+34 612 345 678";

            await Apply(command);

            dynamic row = Row(_individualId);
            ((string)row.Phone).Should().Be("+573001234567");
            ((string)row.AlternativePhone).Should().Be("+34612345678");
        }

        [TestCase("12345")]
        [TestCase("+1")]
        [TestCase("+0123456789")]
        [TestCase("809-555-01ab")]
        [TestCase("+1 809 555 0100 ext 5")]
        public async Task Handle_WithAPhoneThatIsNotAPhone_IsRejected(string phone)
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = phone;

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            (await act.Should().ThrowAsync<ValidationException>()).Which.ErrorMessages!.Should().Contain(e => e.Contains("Phone"));
        }

        [Test]
        public async Task Handle_WithAPhoneOfAnotherCustomer_ThrowsConflict()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = "809-555-0002";

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Test]
        public async Task Handle_WithAnUnknownCustomer_ThrowsNotFound()
        {
            Func<Task> act = () => _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task Handle_WithASoftDeletedCustomer_ThrowsNotFound()
        {
            _connection.Execute("UPDATE Customers.Customer SET IsDeleted = 1 WHERE Id = @Id", new { Id = _individualId });

            Func<Task> act = () => _handler.Handle(ValidCommand(_individualId), CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task Handle_ALegalPersonWithoutCompanyName_IsRejected()
        {
            UpdateCustomerCommand command = ValidCommand(_legalId);
            command.CompanyName = null;

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            (await act.Should().ThrowAsync<ValidationException>()).Which.ErrorMessages!.Should().Contain(e => e.Contains("CompanyName"));
        }

        [Test]
        public async Task Handle_WithInvalidValues_ListsEveryProblem()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.FirstName = "";
            command.Phone = "abc";
            command.Email = "not-an-email";
            command.CreditLimit = -5m;
            command.AnnualIncome = 10.123m;

            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            (await act.Should().ThrowAsync<ValidationException>()).Which.ErrorMessages!.Should().HaveCount(5);
        }

        [Test]
        public async Task Handle_WhenValidationFails_DoesNotTouchTheRow()
        {
            UpdateCustomerCommand command = ValidCommand(_individualId);
            command.Phone = "abc";

            try { await _handler.Handle(command, CancellationToken.None); } catch (ValidationException) { }

            _connection.QuerySingle<string>("SELECT Province FROM Customers.Customer WHERE Id = @Id", new { Id = _individualId })
                .Should().Be("Santo Domingo");
        }
    }
}
