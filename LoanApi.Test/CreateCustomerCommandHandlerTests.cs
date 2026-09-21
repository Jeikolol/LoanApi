using Application.Exceptions;
using Application.Features.Customers.Commands;
using Application.Features.Customers.Handlers;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class CreateCustomerCommandHandlerTests
    {
        private SqliteConnection _connection = null!;
        private CreateCustomerCommandHandler _handler = null!;
        private Guid _identificationTypeId;
        private Guid _rncTypeId;
        private Guid _passportTypeId;
        private Guid _countryId;
        private Guid _unitedStatesId;

        [OneTimeSetUp]
        public void RegisterHandlers() => SqlMapper.AddTypeHandler(new SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Admin;
                ATTACH DATABASE ':memory:' AS Reference;
                ATTACH DATABASE ':memory:' AS Customers;

                CREATE TABLE Admin.IdentificationType (Id TEXT, Code TEXT, IsDeleted INTEGER);
                CREATE TABLE Reference.Country (Id TEXT, CodeIso2 TEXT, IsDeleted INTEGER);
                CREATE TABLE Customers.Customer (
                    Id TEXT, CustomerType INTEGER, FirstName TEXT, LastName TEXT, CompanyName TEXT, IdentificationTypeId TEXT,
                    Identification TEXT, TaxId TEXT, Phone TEXT, AlternativePhone TEXT, Email TEXT, Address TEXT, Province TEXT,
                    CountryId TEXT, NationalityCountryId TEXT, Status INTEGER, Occupation TEXT, AnnualIncome NUMERIC,
                    IncomeSource INTEGER, CreditLimit NUMERIC, RiskLevel INTEGER, IsBlacklisted INTEGER,
                    EmergencyContactName TEXT, EmergencyContactPhone TEXT, CreatedOn TEXT, CreatedById TEXT,
                    IsActive INTEGER, IsDeleted INTEGER);");

            _identificationTypeId = Guid.NewGuid();
            _rncTypeId = Guid.NewGuid();
            _passportTypeId = Guid.NewGuid();
            _countryId = Guid.NewGuid();
            _unitedStatesId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Admin.IdentificationType VALUES (@Id, 'CEDULA', 0)", new { Id = _identificationTypeId });
            _connection.Execute("INSERT INTO Admin.IdentificationType VALUES (@Id, 'RNC', 0)", new { Id = _rncTypeId });
            _connection.Execute("INSERT INTO Admin.IdentificationType VALUES (@Id, 'PASSPORT', 0)", new { Id = _passportTypeId });
            _connection.Execute("INSERT INTO Reference.Country VALUES (@Id, 'DO', 0)", new { Id = _countryId });
            _connection.Execute("INSERT INTO Reference.Country VALUES (@Id, 'US', 0)", new { Id = _unitedStatesId });

            _handler = new CreateCustomerCommandHandler(_connection, NullLogger<CreateCustomerCommandHandler>.Instance);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private CreateCustomerCommand ValidCommand() => new()
        {
            CustomerType = CustomerType.PhysicalPerson,
            FirstName = "  Ana ",
            LastName = "Perez",
            IdentificationTypeId = _identificationTypeId,
            Identification = "402-1234567-8",
            Phone = "829-555-0101",
            Address = "Calle 1 #2",
            Province = "Santo Domingo",
            CountryId = _countryId,
            NationalityCountryId = _countryId,
            CreditLimit = 50000,
        };

        private int CustomerCount() => _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Customers.Customer");

        [Test]
        public async Task Handle_WithValidCustomer_StoresItAsActive()
        {
            CreateCustomerCommand command = ValidCommand();
            command.CreatedById = Guid.NewGuid();

            CreatedCustomerResponse result = await _handler.Handle(command, CancellationToken.None);

            result.Id.Should().NotBe(Guid.Empty);
            CustomerCount().Should().Be(1);

            dynamic row = _connection.QuerySingle("SELECT * FROM Customers.Customer");
            ((string)row.FirstName).Should().Be("Ana");
            ((long)row.Status).Should().Be((long)CustomerStatus.Active);
            ((long)row.IsBlacklisted).Should().Be(0);
            ((long)row.IsActive).Should().Be(1);
            ((string)row.Occupation).Should().BeEmpty();
            Guid.Parse((string)row.CreatedById).Should().Be(command.CreatedById!.Value);
        }

        [Test]
        public async Task Handle_StoresBlankOptionalFieldsAsNull()
        {
            CreateCustomerCommand command = ValidCommand();
            command.Email = "   ";
            command.TaxId = "";
            command.AlternativePhone = " ";

            await _handler.Handle(command, CancellationToken.None);

            dynamic row = _connection.QuerySingle("SELECT * FROM Customers.Customer");
            ((object?)row.Email).Should().BeNull();
            ((object?)row.TaxId).Should().BeNull();
            ((object?)row.AlternativePhone).Should().BeNull();
        }

        [Test]
        public void Handle_WithALegalPersonAndNoCompany_IsRejected() =>
            AssertInvalid(c => { c.CustomerType = CustomerType.LegalPerson; c.CompanyName = null; }, "CompanyName");

        [Test]
        public async Task Handle_WithALegalPersonAndACompany_IsAccepted()
        {
            CreateCustomerCommand command = ValidCommand();
            command.CustomerType = CustomerType.LegalPerson;
            command.CompanyName = "Acme SRL";
            command.IdentificationTypeId = _rncTypeId;
            command.Identification = "131-12345-6";

            await _handler.Handle(command, CancellationToken.None);

            CustomerCount().Should().Be(1);
        }

        [Test]
        public void Handle_WithoutAFirstName_IsRejected() => AssertInvalid(c => c.FirstName = "  ", "FirstName");

        [Test]
        public void Handle_WithATooLongIdentification_IsRejected() => AssertInvalid(c => c.Identification = new string('1', 51), "Identification");

        [Test]
        public void Handle_WithAnInvalidPhone_IsRejected() => AssertInvalid(c => c.Phone = "abc", "Phone");

        [Test]
        public void Handle_WithAnInvalidAlternativePhone_IsRejected() => AssertInvalid(c => c.AlternativePhone = "12", "AlternativePhone");

        [Test]
        public void Handle_WithAnInvalidEmail_IsRejected() => AssertInvalid(c => c.Email = "not-an-email", "Email");

        [Test]
        public void Handle_WithANegativeCreditLimit_IsRejected() => AssertInvalid(c => c.CreditLimit = -1, "CreditLimit");

        [Test]
        public void Handle_WithMoreThanTwoDecimalsInIncome_IsRejected() => AssertInvalid(c => c.AnnualIncome = 10.001m, "AnnualIncome");

        [Test]
        public void Handle_WithAnUnknownCustomerType_IsRejected() => AssertInvalid(c => c.CustomerType = (CustomerType)9, "CustomerType");

        [Test]
        public void Handle_WithEmptyReferenceIds_IsRejected()
        {
            CreateCustomerCommand command = ValidCommand();
            command.IdentificationTypeId = Guid.Empty;
            command.CountryId = Guid.Empty;
            command.NationalityCountryId = Guid.Empty;

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().HaveCountGreaterThanOrEqualTo(3);
            CustomerCount().Should().Be(0);
        }

        [Test]
        public void Handle_WithAnIdentificationTypeThatDoesNotExist_IsRejected() =>
            AssertInvalid(c => c.IdentificationTypeId = Guid.NewGuid(), "identification type");

        [Test]
        public void Handle_WithACountryThatDoesNotExist_IsRejected() =>
            AssertInvalid(c => c.CountryId = Guid.NewGuid(), "country does not exist");

        [Test]
        public void Handle_WithANationalityThatDoesNotExist_IsRejected() =>
            AssertInvalid(c => c.NationalityCountryId = Guid.NewGuid(), "nationality");

        [Test]
        public async Task Handle_WithADuplicateIdentification_ThrowsConflict()
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);

            CreateCustomerCommand second = ValidCommand();
            second.Phone = "809-555-9999";

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(second, CancellationToken.None))!;

            exception.Message.Should().Contain("identification").And.NotContain("phone");
            CustomerCount().Should().Be(1);
        }

        [Test]
        public async Task Handle_WithADuplicatePhone_ThrowsConflict()
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);

            CreateCustomerCommand second = ValidCommand();
            second.Identification = "402-0000000-1";

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(second, CancellationToken.None))!;

            exception.Message.Should().Contain("phone");
            CustomerCount().Should().Be(1);
        }

        [Test]
        public async Task Handle_WhenBothAreDuplicated_ReportsBoth()
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(ValidCommand(), CancellationToken.None))!;

            exception.Message.Should().Contain("identification").And.Contain("phone");
        }

        [Test]
        public async Task Handle_StoresACédulaTypedWithoutDashesInItsCanonicalForm()
        {
            CreateCustomerCommand command = ValidCommand();
            command.Identification = "40212345678";

            await _handler.Handle(command, CancellationToken.None);

            _connection.ExecuteScalar<string>("SELECT Identification FROM Customers.Customer").Should().Be("402-1234567-8");
        }

        [Test]
        public void Handle_WithACédulaThatIsNotElevenDigits_IsRejected() =>
            AssertInvalid(c => c.Identification = "402-123-4", "cédula");

        [Test]
        public async Task Handle_TheSameCédulaInAnotherFormat_IsADuplicate()
        {
            await _handler.Handle(ValidCommand(), CancellationToken.None);

            CreateCustomerCommand second = ValidCommand();
            second.Identification = "40212345678";
            second.Phone = "809-555-9999";

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(second, CancellationToken.None))!;

            exception.Message.Should().Contain("identification");
            CustomerCount().Should().Be(1);
        }

        [Test]
        public async Task Handle_StoresAnRncInItsCanonicalForm()
        {
            CreateCustomerCommand command = ValidCommand();
            command.CustomerType = CustomerType.LegalPerson;
            command.CompanyName = "Acme SRL";
            command.IdentificationTypeId = _rncTypeId;
            command.Identification = "131123456";

            await _handler.Handle(command, CancellationToken.None);

            _connection.ExecuteScalar<string>("SELECT Identification FROM Customers.Customer").Should().Be("131-12345-6");
        }

        [Test]
        public void Handle_WithAnRncThatIsNotNineDigits_IsRejected() =>
            AssertInvalid(c => { c.CustomerType = CustomerType.LegalPerson; c.CompanyName = "Acme"; c.IdentificationTypeId = _rncTypeId; c.Identification = "1234"; }, "RNC");

        [Test]
        public void Handle_AnRncForAnIndividual_IsRejected() =>
            AssertInvalid(c => { c.IdentificationTypeId = _rncTypeId; c.Identification = "131-12345-6"; }, "only be used for a legal person");

        [Test]
        public void Handle_ACédulaForAForeignNational_IsRejected() =>
            AssertInvalid(c => c.NationalityCountryId = _unitedStatesId, "Dominican nationals");

        [Test]
        public async Task Handle_ADominicanCanUseAPassport()
        {
            CreateCustomerCommand command = ValidCommand();
            command.IdentificationTypeId = _passportTypeId;
            command.Identification = "DO12345678";

            await _handler.Handle(command, CancellationToken.None);

            CustomerCount().Should().Be(1);
        }

        [Test]
        public void Handle_AnIndividualWithAnyOtherDocumentType_IsRejected()
        {
            Guid otherTypeId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Admin.IdentificationType VALUES (@Id, 'DRIVER_LICENSE', 0)", new { Id = otherTypeId });

            AssertInvalid(c => c.IdentificationTypeId = otherTypeId, "cédula or passport");
        }

        [Test]
        public void Handle_ALegalPersonWithACédula_IsRejected() =>
            AssertInvalid(c => { c.CustomerType = CustomerType.LegalPerson; c.CompanyName = "Acme"; }, "identified by RNC");

        [Test]
        public async Task Handle_StoresAPassportWithoutSpacesInUpperCase()
        {
            CreateCustomerCommand command = ValidCommand();
            command.IdentificationTypeId = _passportTypeId;
            command.NationalityCountryId = _unitedStatesId;
            command.Identification = "us 1234-5678";

            await _handler.Handle(command, CancellationToken.None);

            _connection.ExecuteScalar<string>("SELECT Identification FROM Customers.Customer").Should().Be("US12345678");
        }

        [Test]
        public void Handle_WithAPassportFromAnotherCountryThanTheNationality_IsRejected() =>
            AssertInvalid(c => { c.IdentificationTypeId = _passportTypeId; c.NationalityCountryId = _unitedStatesId; c.Identification = "ES12345678"; }, "ISO country code");

        [Test]
        public void Handle_WithAPassportThatDoesNotStartWithTwoLetters_IsRejected() =>
            AssertInvalid(c => { c.IdentificationTypeId = _passportTypeId; c.NationalityCountryId = _unitedStatesId; c.Identification = "12345678"; }, "passport");

        private void AssertInvalid(Action<CreateCustomerCommand> breakIt, string expected)
        {
            CreateCustomerCommand command = ValidCommand();
            breakIt(command);

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains(expected, StringComparison.OrdinalIgnoreCase));
            CustomerCount().Should().Be(0);
        }
    }
}
