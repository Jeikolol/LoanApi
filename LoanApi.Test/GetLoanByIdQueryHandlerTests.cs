using Application.Exceptions;
using Application.Features.Loans.Commands;
using Application.Features.Loans.Handlers;
using Application.Features.Loans.Queries;
using Application.Models.Responses;
using Dapper;
using FluentAssertions;
using LoanApi.Tests.TestHelpers;
using Moq;
using System.Data;

namespace LoanApi.Test
{
    [TestFixture]
    public class GetLoanByIdQueryHandlerTests
    {
        private Mock<IDbConnectionWrapper> _mockConnection;
        private CreateLoanCommandHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockConnection = new Mock<IDbConnectionWrapper>();
            //_handler = new CreateLoanCommandHandler(_mockConnection.Object);
        }

        [Test]
        public async Task Handle_WithValidCommand_CreateLoanAndReturnResponse()
        {
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 1000,
                InterestRate = 3.5m,
                TermMonths = 6,
            };

            var expectedResponse = new LoanDetailResponseBuilder().Build();

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(1);

            _mockConnection
                .Setup(c => c.QuerySingleOrDefaultAsync<LoanDetailResponse>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(expectedResponse);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Test]
        [Category("Happy Path")]
        public async Task Handle_WithSmallLoan_CreateSuccessfully()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 1000,
                InterestRate = 3.5m,
                TermMonths = 6,
            };

            var response = new LoanDetailResponseBuilder()
                .WithPrincipalAmount(1000)
                .Build();

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(1);

            _mockConnection
                .Setup(c => c.QuerySingleOrDefaultAsync<LoanDetailResponse>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.PrincipalAmount.Should().Be(1000);
            result.InterestRate.Should().Be(3.5m);
            result.TermMonths.Should().Be(6);
        }

        [Test]
        [Category("Happy Path")]
        public async Task Handle_WithLargeLoan_CreateSuccessfully()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 1_000_000,
                InterestRate = 6.5m,
                TermMonths = 120,
            };

            var response = new LoanDetailResponseBuilder()
                .WithPrincipalAmount(1_000_000)
                .Build();

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(1);

            _mockConnection
                .Setup(c => c.QuerySingleOrDefaultAsync<LoanDetailResponse>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.PrincipalAmount.Should().Be(1_000_000);
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithZeroPrincipalAmount_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 0,
                InterestRate = 5,
                TermMonths = 12,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithNegativePrincipalAmount_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = -5000,
                InterestRate = 5,
                TermMonths = 12,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithNegativeInterestRate_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 10000,
                InterestRate = -2,
                TermMonths = 12,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithZeroTermMonths_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 10000,
                InterestRate = 5,
                TermMonths = 0,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithEmptyCustomerId_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.Empty,
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 10000,
                InterestRate = 5,
                TermMonths = 12,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Validation")]
        public void Handle_WithEmptyBranchId_ThrowValidationException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.Empty,
                PrincipalAmount = 10000,
                InterestRate = 5,
                TermMonths = 12,
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [TestCase(100000, 2.5, 12)]
        [TestCase(50000, 5, 24)]
        [TestCase(25000, 7.5, 36)]
        [TestCase(75000, 4.0, 60)]
        [Category("Multiple Scenarios")]
        public async Task Handle_WithVariousValidAmounts_CreateSuccessfully(
            decimal amount,
            decimal rate,
            int months)
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = amount,
                InterestRate = rate,
                TermMonths = months,
            };

            var response = new LoanDetailResponseBuilder()
                .WithPrincipalAmount(amount)
                .Build();

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(1);

            _mockConnection
                .Setup(c => c.QuerySingleOrDefaultAsync<LoanDetailResponse>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(response);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PrincipalAmount.Should().Be(amount);
            result.InterestRate.Should().Be(rate);
            result.TermMonths.Should().Be(months);
        }

        [Test]
        [Category("Database")]
        public async Task Handle_WhenDatabaseInsertFails_ThrowException()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 10000,
                InterestRate = 5,
                TermMonths = 12,
            };

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        [Category("Database")]
        public async Task Handle_ValidCommand_CallsDatabaseOnce()
        {
            // Arrange
            var command = new CreateLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                PrincipalAmount = 10000,
                InterestRate = 5,
                TermMonths = 12,
            };

            var response = new LoanDetailResponseBuilder().Build();

            _mockConnection
                .Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(1);

            _mockConnection
                .Setup(c => c.QuerySingleOrDefaultAsync<LoanDetailResponse>(
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .ReturnsAsync(response);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert - verify ExecuteAsync was called exactly once
            _mockConnection.Verify(
                c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()),
                Times.Once);
        }

        [TearDown]
        public void Cleanup()
        {
            _mockConnection?.Reset();
        }
    }
}
