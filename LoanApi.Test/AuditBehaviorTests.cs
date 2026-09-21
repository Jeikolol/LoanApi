using Application.Behaviors;
using Application.Exceptions;
using Application.Interfaces;
using Dapper;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LoanApi.Test
{
    [TestFixture]
    public class AuditBehaviorTests
    {
        public record SampleCommand(string? UserName, string Password, string Notes, decimal Amount) : IRequest<string>;

        public record SampleQuery(string Term) : IRequest<string>;

        private SqliteConnection _connection = null!;
        private Mock<ICurrentUser> _currentUser = null!;
        private Guid _userId;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Admin;
                CREATE TABLE Admin.AuditLog (
                    Id TEXT, OccurredOn TEXT, UserId TEXT, UserName TEXT, Action TEXT, Payload TEXT,
                    Succeeded INTEGER, Error TEXT, IpAddress TEXT, DurationMs INTEGER);");

            _userId = Guid.NewGuid();
            _currentUser = new Mock<ICurrentUser>();
            _currentUser.SetupGet(u => u.UserId).Returns(_userId);
            _currentUser.SetupGet(u => u.UserName).Returns("maria");
            _currentUser.SetupGet(u => u.IpAddress).Returns("10.0.0.7");
        }

        [TearDown]
        public void Cleanup() => _connection.Dispose();

        private AuditBehavior<TRequest, string> Behavior<TRequest>() where TRequest : notnull =>
            new(_connection, _currentUser.Object, NullLogger<AuditBehavior<TRequest, string>>.Instance);

        private static SampleCommand Command(string? userName = null, string notes = "hello") =>
            new(userName, "s3cret-Password!", notes, 125.50m);

        private int LogCount() => _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Admin.AuditLog");

        private dynamic OnlyLog() => _connection.QuerySingle("SELECT * FROM Admin.AuditLog");

        [Test]
        public async Task Handle_WhenACommandSucceeds_RecordsWhoDidWhat()
        {
            string result = await Behavior<SampleCommand>().Handle(Command(), _ => Task.FromResult("done"), CancellationToken.None);

            result.Should().Be("done");
            LogCount().Should().Be(1);

            dynamic log = OnlyLog();
            ((string)log.Action).Should().Be("SampleCommand");
            ((long)log.Succeeded).Should().Be(1);
            ((string)log.UserName).Should().Be("maria");
            ((string)log.IpAddress).Should().Be("10.0.0.7");
            Guid.Parse((string)log.UserId).Should().Be(_userId);
            ((string)log.Payload).Should().Contain("125.50").And.Contain("hello");
        }

        [Test]
        public async Task Handle_NeverStoresPasswords()
        {
            await Behavior<SampleCommand>().Handle(Command(), _ => Task.FromResult("done"), CancellationToken.None);

            string payload = OnlyLog().Payload;

            payload.Should().NotContain("s3cret-Password!");
            payload.Should().Contain("\"Password\":\"***\"");
        }

        [Test]
        public async Task Handle_ReplacesVeryLongValuesWithTheirLength()
        {
            await Behavior<SampleCommand>().Handle(Command(notes: new string('x', 5000)), _ => Task.FromResult("done"), CancellationToken.None);

            string payload = OnlyLog().Payload;

            payload.Should().Contain("[5000 characters]");
            payload.Length.Should().BeLessThan(4000);
        }

        [Test]
        public void Handle_WhenACommandFails_RecordsTheFailureAndRethrows()
        {
            ConflictException thrown = Assert.ThrowsAsync<ConflictException>(() =>
                Behavior<SampleCommand>().Handle(Command(), _ => throw new ConflictException("Already exists"), CancellationToken.None))!;

            thrown.Message.Should().Be("Already exists");
            LogCount().Should().Be(1);

            dynamic log = OnlyLog();
            ((long)log.Succeeded).Should().Be(0);
            ((string)log.Error).Should().Be("Already exists");
        }

        [Test]
        public async Task Handle_WithoutASignedInUser_UsesTheUserNameFromTheRequest()
        {
            _currentUser.SetupGet(u => u.UserName).Returns((string?)null);
            _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

            await Behavior<SampleCommand>().Handle(Command(userName: "carlos"), _ => Task.FromResult("done"), CancellationToken.None);

            dynamic log = OnlyLog();
            ((string)log.UserName).Should().Be("carlos");
            ((object?)log.UserId).Should().BeNull();
        }

        [Test]
        public async Task Handle_DoesNotAuditQueries()
        {
            string result = await Behavior<SampleQuery>().Handle(new SampleQuery("abc"), _ => Task.FromResult("rows"), CancellationToken.None);

            result.Should().Be("rows");
            LogCount().Should().Be(0);
        }

        [Test]
        public async Task Handle_WhenTheAuditWriteFails_TheOperationStillSucceeds()
        {
            _connection.Execute("DROP TABLE Admin.AuditLog");

            string result = await Behavior<SampleCommand>().Handle(Command(), _ => Task.FromResult("done"), CancellationToken.None);

            result.Should().Be("done");
        }
    }
}
