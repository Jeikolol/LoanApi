using Application.Exceptions;
using Application.Features.Commands;
using Application.Features.Handlers;
using Application.Interfaces;
using Application.Services;
using Dapper;
using Domain.Entities;
using FluentAssertions;
using LoanApi.Test.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;

namespace LoanApi.Test
{
    [TestFixture]
    public class AuthenticateCommandHandlerTests
    {
        private const string Password = "Str0ng!Passw0rd";

        private SqliteConnection _connection = null!;
        private AuthenticateCommandHandler _handler = null!;
        private Guid _employeeId;

        [OneTimeSetUp]
        public void RegisterHandlers() => SqlMapper.AddTypeHandler(new SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Admin;
                CREATE TABLE Admin.Role (Id TEXT, Code TEXT, IsDeleted INTEGER);
                CREATE TABLE Admin.Employee (
                    Id TEXT, UserName TEXT, PasswordHash TEXT, RoleId TEXT, MustChangePassword INTEGER,
                    LastLoginOn TEXT, IsActive INTEGER, IsDeleted INTEGER);");

            Guid roleId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Admin.Role VALUES (@Id, 'MANAGER', 0)", new { Id = roleId });

            _employeeId = Guid.NewGuid();
            string hash = new PasswordHasher<Employee>().HashPassword(new Employee(), Password);
            _connection.Execute(
                "INSERT INTO Admin.Employee VALUES (@Id, 'maria', @Hash, @RoleId, 0, NULL, 1, 0)",
                new { Id = _employeeId, Hash = hash, RoleId = roleId });

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["JwtSettings:Key"] = "a-long-enough-key-for-the-tests-0123456789" })
                .Build();

            Mock<IJwtTokenService> jwt = new();
            jwt.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
               .Returns("token");

            _handler = new AuthenticateCommandHandler(configuration, jwt.Object, _connection);
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private AuthenticateCommand Login(string password = Password, string userName = "maria") =>
            new() { UserName = userName, Password = password, RememberMe = false };

        [Test]
        public async Task AnActiveEmployee_SignsInAndTheirLastLoginIsRecorded()
        {
            TokenResponse result = await _handler.Handle(Login(), CancellationToken.None);

            result.Token.Should().Be("token");
            _connection.ExecuteScalar<string?>("SELECT LastLoginOn FROM Admin.Employee").Should().NotBeNull();
        }

        [Test]
        public void ADeactivatedEmployee_CannotSignIn_EvenWithTheRightPassword()
        {
            _connection.Execute("UPDATE Admin.Employee SET IsActive = 0");

            UnauthorizedException exception = Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(Login(), CancellationToken.None))!;

            exception.Message.Should().Be("Invalid username or password.", "the answer must not reveal that the account exists");
        }

        [Test]
        public void ARemovedEmployee_CannotSignIn()
        {
            _connection.Execute("UPDATE Admin.Employee SET IsDeleted = 1");

            Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(Login(), CancellationToken.None));
        }

        [Test]
        public void AWrongPassword_IsRefused_AndRecordsNoLogin()
        {
            Assert.ThrowsAsync<UnauthorizedException>(() => _handler.Handle(Login("wrong"), CancellationToken.None));

            _connection.ExecuteScalar<string?>("SELECT LastLoginOn FROM Admin.Employee").Should().BeNull();
        }
    }
}
