using Application.Exceptions;
using Application.Features.Employees;
using Application.Interfaces;
using Dapper;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LoanApi.Test
{
    [TestFixture]
    public class EmployeeHandlerTests
    {
        private SqliteConnection _connection = null!;
        private Guid _me;
        private Guid _adminRole;
        private Guid _managerRole;
        private Guid _userRole;
        private Guid _branch;
        private ICurrentUser _currentUser = null!;

        [OneTimeSetUp]
        public void RegisterHandlers() => SqlMapper.AddTypeHandler(new SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Admin;

                CREATE TABLE Admin.Role (Id TEXT, Code TEXT, Name TEXT, IsDeleted INTEGER);
                CREATE TABLE Admin.Branch (Id TEXT, Code TEXT, Name TEXT, IsDeleted INTEGER);
                CREATE TABLE Admin.Employee (
                    Id TEXT, UserName TEXT, PasswordHash TEXT, PasswordSalt TEXT, FirstName TEXT, LastName TEXT, Email TEXT,
                    RoleId TEXT, BranchId TEXT, LastLoginOn TEXT, MustChangePassword INTEGER, TokensValidFrom TEXT,
                    CreatedOn TEXT, CreatedById TEXT, UpdatedOn TEXT, IsActive INTEGER, IsDeleted INTEGER);");

            _adminRole = Guid.NewGuid();
            _managerRole = Guid.NewGuid();
            _userRole = Guid.NewGuid();
            _branch = Guid.NewGuid();
            _connection.Execute("INSERT INTO Admin.Role VALUES (@Id, 'ADMIN', 'Administrador', 0)", new { Id = _adminRole });
            _connection.Execute("INSERT INTO Admin.Role VALUES (@Id, 'MANAGER', 'Gerente', 0)", new { Id = _managerRole });
            _connection.Execute("INSERT INTO Admin.Role VALUES (@Id, 'USER', 'Usuario', 0)", new { Id = _userRole });
            _connection.Execute("INSERT INTO Admin.Branch VALUES (@Id, 'HQ', 'Principal', 0)", new { Id = _branch });

            _me = InsertEmployee("admin", "admin@example.com", _adminRole);

            Mock<ICurrentUser> currentUser = new();
            currentUser.SetupGet(u => u.UserId).Returns(() => _me);
            _currentUser = currentUser.Object;
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private Guid InsertEmployee(string userName, string email, Guid roleId, bool active = true)
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                @"INSERT INTO Admin.Employee
                    (Id, UserName, PasswordHash, PasswordSalt, FirstName, LastName, Email, RoleId, BranchId, MustChangePassword,
                     CreatedOn, IsActive, IsDeleted)
                  VALUES (@Id, @UserName, 'old-hash', '', 'Nombre', 'Apellido', @Email, @RoleId, @Branch, 0,
                          '2026-01-01T00:00:00', @Active, 0)",
                new { Id = id, UserName = userName, Email = email, RoleId = roleId, Branch = _branch, Active = active ? 1 : 0 });
            return id;
        }

        private CreateEmployeeCommand NewEmployee() => new()
        {
            UserName = " Maria.Perez ",
            FirstName = " María ",
            LastName = "Pérez",
            Email = " Maria@Example.com ",
            RoleId = _managerRole,
            BranchId = _branch,
        };

        private CreateEmployeeCommandHandler CreateHandler() => new(_connection, _currentUser, NullLogger<CreateEmployeeCommandHandler>.Instance);
        private UpdateEmployeeCommandHandler UpdateHandler() => new(_connection, _currentUser);
        private SetEmployeeActiveCommandHandler ActiveHandler() => new(_connection, _currentUser);
        private ResetEmployeePasswordCommandHandler ResetHandler() => new(_connection, _currentUser);

        private dynamic Row(Guid id) => _connection.QuerySingle("SELECT * FROM Admin.Employee WHERE Id = @Id", new { Id = id });

        // ------------------------------------------------------------- create

        [Test]
        public async Task Create_StoresAHashedTemporaryPassword_AndForcesAChange()
        {
            EmployeeCredentialsResponse result = await CreateHandler().Handle(NewEmployee(), CancellationToken.None);

            result.TemporaryPassword.Should().HaveLength(EmployeeRules.TemporaryPasswordLength);
            result.Employee.UserName.Should().Be("maria.perez");
            result.Employee.Email.Should().Be("maria@example.com");
            result.Employee.RoleCode.Should().Be("MANAGER");
            result.Employee.MustChangePassword.Should().BeTrue();
            result.Employee.IsActive.Should().BeTrue();

            dynamic row = Row(result.Employee.Id);
            string hash = row.PasswordHash;
            hash.Should().NotContain(result.TemporaryPassword);
            new PasswordHasher<Employee>().VerifyHashedPassword(new Employee(), hash, result.TemporaryPassword)
                .Should().NotBe(PasswordVerificationResult.Failed);
            ((string)row.CreatedById).ToLowerInvariant().Should().Be(_me.ToString());
        }

        [Test]
        public async Task Create_NeverReturnsTheSamePasswordTwice()
        {
            EmployeeCredentialsResponse first = await CreateHandler().Handle(NewEmployee(), CancellationToken.None);
            CreateEmployeeCommand second = NewEmployee();
            second.UserName = "another";
            second.Email = "another@example.com";
            EmployeeCredentialsResponse other = await CreateHandler().Handle(second, CancellationToken.None);

            first.TemporaryPassword.Should().NotBe(other.TemporaryPassword);
        }

        [Test]
        public async Task Create_WithADuplicateUserNameOrEmail_IsAConflict()
        {
            await CreateHandler().Handle(NewEmployee(), CancellationToken.None);

            CreateEmployeeCommand sameName = NewEmployee();
            sameName.Email = "other@example.com";
            ConflictException byName = Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(sameName, CancellationToken.None))!;
            byName.Message.Should().Contain("user name");

            CreateEmployeeCommand sameEmail = NewEmployee();
            sameEmail.UserName = "someone.else";
            ConflictException byEmail = Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(sameEmail, CancellationToken.None))!;
            byEmail.Message.Should().Contain("email");
        }

        [Test]
        public void Create_TreatsUserNamesCaseInsensitively()
        {
            CreateEmployeeCommand command = NewEmployee();
            command.UserName = "ADMIN";
            command.Email = "new@example.com";

            Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(command, CancellationToken.None));
        }

        [Test]
        public void Create_WithInvalidData_ListsEveryProblem()
        {
            CreateEmployeeCommand command = new() { UserName = "x", FirstName = "", LastName = "", Email = "nope" };

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => CreateHandler().Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().HaveCountGreaterThanOrEqualTo(5);
        }

        [Test]
        public void Create_WithAnUnknownRoleOrBranch_IsRejected()
        {
            CreateEmployeeCommand command = NewEmployee();
            command.RoleId = Guid.NewGuid();
            command.BranchId = Guid.NewGuid();

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => CreateHandler().Handle(command, CancellationToken.None))!;

            exception.ErrorMessages.Should().HaveCount(2);
            _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Admin.Employee").Should().Be(1);
        }

        // ------------------------------------------------------------- update

        [Test]
        public async Task Update_ChangesTheProfileAndRole_AndRevokesTheirSessions()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);

            EmployeeResponse result = await UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = employee, FirstName = "Carlos", LastName = "Díaz", Email = "carlos.diaz@example.com", RoleId = _managerRole, BranchId = _branch,
            }, CancellationToken.None);

            result.RoleCode.Should().Be("MANAGER");
            result.Email.Should().Be("carlos.diaz@example.com");
            ((object?)Row(employee).TokensValidFrom).Should().NotBeNull("a role change ends the sessions that carry the old role");
        }

        [Test]
        public async Task Update_WithoutChangingTheRole_KeepsTheirSessions()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);

            await UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = employee, FirstName = "Carlos", LastName = "Díaz", Email = "carlos@example.com", RoleId = _userRole, BranchId = _branch,
            }, CancellationToken.None);

            ((object?)Row(employee).TokensValidFrom).Should().BeNull();
        }

        [Test]
        public void Update_CannotChangeYourOwnRole()
        {
            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = _me, FirstName = "A", LastName = "B", Email = "admin@example.com", RoleId = _userRole, BranchId = _branch,
            }, CancellationToken.None))!;

            exception.Message.Should().Contain("own role");
        }

        [Test]
        public void Update_CannotRemoveTheRoleOfTheLastAdministrator()
        {
            Guid secondAdmin = InsertEmployee("second", "second@example.com", _adminRole);
            _connection.Execute("UPDATE Admin.Employee SET IsActive = 0 WHERE Id = @Id", new { Id = _me });

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = secondAdmin, FirstName = "A", LastName = "B", Email = "second@example.com", RoleId = _userRole, BranchId = _branch,
            }, CancellationToken.None))!;

            exception.Message.Should().Contain("last active administrator");
            ((string)Row(secondAdmin).RoleId).ToLowerInvariant().Should().Be(_adminRole.ToString());
        }

        [Test]
        public async Task Update_CanDemoteAnAdministratorWhenAnotherOneRemains()
        {
            Guid secondAdmin = InsertEmployee("second", "second@example.com", _adminRole);

            EmployeeResponse result = await UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = secondAdmin, FirstName = "A", LastName = "B", Email = "second@example.com", RoleId = _managerRole, BranchId = _branch,
            }, CancellationToken.None);

            result.RoleCode.Should().Be("MANAGER");
        }

        [Test]
        public void Update_WithAnEmailOfAnotherEmployee_IsAConflict()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);

            Assert.ThrowsAsync<ConflictException>(() => UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = employee, FirstName = "A", LastName = "B", Email = "admin@example.com", RoleId = _userRole, BranchId = _branch,
            }, CancellationToken.None));
        }

        [Test]
        public void Update_OfAnUnknownEmployee_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => UpdateHandler().Handle(new UpdateEmployeeCommand
            {
                Id = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "x@example.com", RoleId = _userRole, BranchId = _branch,
            }, CancellationToken.None));
        }

        // ---------------------------------------------------------- activation

        [Test]
        public async Task Deactivate_DisablesTheAccount_AndRevokesTheirSessions()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);

            EmployeeResponse result = await ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = employee, Active = false }, CancellationToken.None);

            result.IsActive.Should().BeFalse();
            ((object?)Row(employee).TokensValidFrom).Should().NotBeNull();
        }

        [Test]
        public async Task Activate_ReenablesTheAccount()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole, active: false);

            EmployeeResponse result = await ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = employee, Active = true }, CancellationToken.None);

            result.IsActive.Should().BeTrue();
        }

        [Test]
        public void Deactivate_CannotDeactivateYourself()
        {
            Assert.ThrowsAsync<ConflictException>(() => ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = _me, Active = false }, CancellationToken.None));
        }

        [Test]
        public void Deactivate_CannotDeactivateTheLastActiveAdministrator()
        {
            Guid lastAdmin = InsertEmployee("second", "second@example.com", _adminRole);
            _connection.Execute("UPDATE Admin.Employee SET IsActive = 0 WHERE Id = @Id", new { Id = _me });

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(
                () => ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = lastAdmin, Active = false }, CancellationToken.None))!;

            exception.Message.Should().Contain("last active administrator");
            ((long)Row(lastAdmin).IsActive).Should().Be(1);
        }

        [Test]
        public async Task Deactivate_CanDeactivateAnAdministratorWhenAnotherOneRemains()
        {
            Guid other = InsertEmployee("second", "second@example.com", _adminRole);

            EmployeeResponse result = await ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = other, Active = false }, CancellationToken.None);

            result.IsActive.Should().BeFalse();
        }

        [Test]
        public void Deactivate_OfAnUnknownEmployee_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = Guid.NewGuid(), Active = false }, CancellationToken.None));
        }

        // ------------------------------------------------------------- reset

        [Test]
        public async Task Reset_IssuesANewPasswordThatMustBeChanged_AndEndsTheirSessions()
        {
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);

            EmployeeCredentialsResponse result = await ResetHandler().Handle(new ResetEmployeePasswordCommand { Id = employee }, CancellationToken.None);

            dynamic row = Row(employee);
            string hash = row.PasswordHash;
            hash.Should().NotBe("old-hash");
            new PasswordHasher<Employee>().VerifyHashedPassword(new Employee(), hash, result.TemporaryPassword).Should().NotBe(PasswordVerificationResult.Failed);
            ((long)row.MustChangePassword).Should().Be(1);
            ((object?)row.TokensValidFrom).Should().NotBeNull();
        }

        [Test]
        public void Reset_CannotResetYourOwnPassword()
        {
            ConflictException exception = Assert.ThrowsAsync<ConflictException>(
                () => ResetHandler().Handle(new ResetEmployeePasswordCommand { Id = _me }, CancellationToken.None))!;

            exception.Message.Should().Contain("change password");
        }

        [Test]
        public void Reset_OfAnUnknownEmployee_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => ResetHandler().Handle(new ResetEmployeePasswordCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        // ---------------------------------------------------- token validation

        [Test]
        public async Task TheAccessValidator_FollowsTheAccount()
        {
            EmployeeAccessValidator validator = new(_connection);
            Guid employee = InsertEmployee("carlos", "carlos@example.com", _userRole);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            (await validator.IsTokenStillValidAsync(employee, now)).Should().BeTrue();

            await ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = employee, Active = false }, CancellationToken.None);
            (await validator.IsTokenStillValidAsync(employee, now)).Should().BeFalse("deactivated");

            await ActiveHandler().Handle(new SetEmployeeActiveCommand { Id = employee, Active = true }, CancellationToken.None);
            (await validator.IsTokenStillValidAsync(employee, now - 60)).Should().BeFalse("issued before the deactivation");
            (await validator.IsTokenStillValidAsync(employee, now + 60)).Should().BeTrue("a new sign-in after reactivation");
            (await validator.IsTokenStillValidAsync(Guid.NewGuid(), now)).Should().BeFalse("unknown employee");
        }
    }
}
