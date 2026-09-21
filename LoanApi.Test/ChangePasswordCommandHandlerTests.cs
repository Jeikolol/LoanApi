using Application.Exceptions;
using Application.Features.Commands;
using Application.Features.Handlers;
using Application.Services;
using Dapper;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Moq;

namespace LoanApi.Test
{
    [TestFixture]
    public class ChangePasswordCommandHandlerTests
    {
        private const string CurrentPassword = "Old-Password-1!";
        private const string StrongPassword = "New-Password-2@";

        private SqliteConnection _connection = null!;
        private Mock<IJwtTokenService> _jwt = null!;
        private ChangePasswordCommandHandler _handler = null!;
        private Guid _userId;

        [OneTimeSetUp]
        public void RegisterGuidHandler() => SqlMapper.AddTypeHandler(new LoanApi.Test.TestHelpers.SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _userId = Guid.NewGuid();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Admin;
                CREATE TABLE Admin.Employee (
                    Id TEXT, UserName TEXT, PasswordHash TEXT, RoleId TEXT,
                    MustChangePassword INTEGER, UpdatedOn TEXT, IsDeleted INTEGER, IsActive INTEGER DEFAULT 1);
                CREATE TABLE Admin.Role (Id TEXT, Code TEXT, IsDeleted INTEGER);");

            var roleId = Guid.NewGuid();
            string hash = new PasswordHasher<Employee>().HashPassword(new Employee(), CurrentPassword);

            _connection.Execute("INSERT INTO Admin.Role VALUES (@roleId, 'ADMIN', 0)", new { roleId });
            _connection.Execute(
                "INSERT INTO Admin.Employee (Id, UserName, PasswordHash, RoleId, MustChangePassword, UpdatedOn, IsDeleted) VALUES (@Id, 'admin', @hash, @roleId, 1, NULL, 0)",
                new { Id = _userId, hash, roleId });

            _jwt = new Mock<IJwtTokenService>();
            _jwt.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), false,
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns("fresh-token");

            _handler = new ChangePasswordCommandHandler(_jwt.Object, _connection);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private ChangePasswordCommand Command(string current, string next) => new()
        {
            UserId = _userId,
            CurrentPassword = current,
            NewPassword = next,
        };

        [Test]
        public void Handle_WithWrongCurrentPassword_Throws()
        {
            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(Command("wrong", StrongPassword), CancellationToken.None))!;

            exception.Message.Should().Contain("current password");
        }

        [TestCase("short1!A")]
        [TestCase("alllowercase-1!")]
        [TestCase("ALLUPPERCASE-1!")]
        [TestCase("NoDigitsHere-!!")]
        [TestCase("NoSymbolsHere123")]
        public void Handle_WithWeakNewPassword_Throws(string weak)
        {
            Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(Command(CurrentPassword, weak), CancellationToken.None));
        }

        [Test]
        public void Handle_WithSamePassword_Throws()
        {
            ValidationException exception = Assert.ThrowsAsync<ValidationException>(
                () => _handler.Handle(Command(CurrentPassword, CurrentPassword), CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains("different"));
        }

        [Test]
        public async Task Handle_WithValidPassword_UpdatesHashClearsFlagAndReturnsToken()
        {
            TokenResponse result = await _handler.Handle(Command(CurrentPassword, StrongPassword), CancellationToken.None);

            result.Token.Should().Be("fresh-token");
            result.MustChangePassword.Should().BeFalse();

            (string hash, int mustChange) = _connection.QuerySingle<(string, int)>(
                "SELECT PasswordHash, MustChangePassword FROM Admin.Employee WHERE Id = @Id", new { Id = _userId });

            mustChange.Should().Be(0);
            var verification = new PasswordHasher<Employee>().VerifyHashedPassword(new Employee(), hash, StrongPassword);
            verification.Should().NotBe(PasswordVerificationResult.Failed);
        }

        [Test]
        public void Handle_WithUnknownUser_ThrowsUnauthorized()
        {
            ChangePasswordCommand command = Command(CurrentPassword, StrongPassword);
            command.UserId = Guid.NewGuid();

            Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
