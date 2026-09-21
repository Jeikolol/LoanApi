using Application.Exceptions;
using Application.Features.Commands;
using Application.Services;
using Dapper;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace Application.Features.Handlers
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, TokenResponse>
    {
        private const int MinimumPasswordLength = 12;

        private readonly IJwtTokenService _jwt;
        private readonly IDbConnection _connection;

        public ChangePasswordCommandHandler(IJwtTokenService jwt, IDbConnection connection)
        {
            _jwt = jwt;
            _connection = connection;
        }

        public async Task<TokenResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            Employee? employee = await _connection.QueryFirstOrDefaultAsync<Employee>(
                "SELECT * FROM Admin.Employee WHERE Id = @UserId AND IsActive = 1 AND IsDeleted = 0", new { request.UserId });

            if (employee is null)
                throw new UnauthorizedException("Invalid session.");

            var hasher = new PasswordHasher<Employee>();

            if (hasher.VerifyHashedPassword(employee, employee.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
                throw new ValidationException("The current password is incorrect.");

            List<string> errors = GetPolicyViolations(request.NewPassword);

            if (request.NewPassword == request.CurrentPassword)
                errors.Add("The new password must be different from the current one.");

            if (errors.Count > 0)
                throw new ValidationException("The new password does not meet the requirements.", errors);

            string newHash = hasher.HashPassword(employee, request.NewPassword);

            await _connection.ExecuteAsync(
                "UPDATE Admin.Employee SET PasswordHash = @newHash, MustChangePassword = 0, UpdatedOn = @Now WHERE Id = @UserId",
                new { newHash, Now = DateTime.UtcNow, request.UserId });

            string roleCode = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Code FROM Admin.Role WHERE Id = @RoleId AND IsDeleted = 0", new { employee.RoleId })
                ?? SystemRoles.User;

            return new TokenResponse(_jwt.GenerateToken(employee.Id, employee.UserName, roleCode, false, employee.FirstName, employee.LastName, employee.Email));
        }

        private static List<string> GetPolicyViolations(string password)
        {
            List<string> errors = [];

            if (password.Length < MinimumPasswordLength) errors.Add($"Must be at least {MinimumPasswordLength} characters long.");
            if (!password.Any(char.IsUpper)) errors.Add("Must contain an uppercase letter.");
            if (!password.Any(char.IsLower)) errors.Add("Must contain a lowercase letter.");
            if (!password.Any(char.IsDigit)) errors.Add("Must contain a digit.");
            if (password.All(char.IsLetterOrDigit)) errors.Add("Must contain a symbol.");

            return errors;
        }
    }
}
