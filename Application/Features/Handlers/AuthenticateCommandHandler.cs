using Application.Exceptions;
using Application.Features.Commands;
using Application.Services;
using Dapper;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Application.Features.Handlers
{
    public class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, TokenResponse>
    {
        private readonly string _key;
        private readonly IConfiguration _configuration;
        private readonly IJwtTokenService _jwt;
        private readonly IDbConnection _connection;

        public AuthenticateCommandHandler(IConfiguration configuration, IJwtTokenService jwt, IDbConnection connection)
        {
            _configuration = configuration;

            _key = _configuration["JwtSettings:Key"]!;

            if (string.IsNullOrWhiteSpace(_key))
            {
                throw new InternalServerException("JWT Key is not configured in SecuritySettings:JwtSettings:Key");
            }

            _jwt = jwt;
            _connection = connection;
        }

        public async Task<TokenResponse> Handle(AuthenticateCommand request, CancellationToken cancellationToken)
        {
            string query = "SELECT * FROM Admin.Employee WHERE Username = @UserName AND IsActive = 1 AND IsDeleted = 0";

            var args = new
            {
               request.UserName,
               request.RememberMe
            };

            var employee = await _connection.QueryFirstOrDefaultAsync<Employee>(query, args);

            if (employee == null)
                throw new UnauthorizedException("Invalid username or password.");

            var hasher = new PasswordHasher<Employee>();

            var passRes = hasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password);

            if (passRes == PasswordVerificationResult.Failed)
                throw new UnauthorizedException("Invalid username or password.");

            var roleCode = await _connection.QueryFirstOrDefaultAsync<string>(
                "SELECT Code FROM Admin.Role WHERE Id = @RoleId AND IsDeleted = 0", new { employee.RoleId });

            bool enforcePasswordChange = !string.Equals(
                _configuration["Security:EnforcePasswordChange"], "false", StringComparison.OrdinalIgnoreCase);
            bool mustChangePassword = employee.MustChangePassword && enforcePasswordChange;

            await _connection.ExecuteAsync(
                "UPDATE Admin.Employee SET LastLoginOn = @Now WHERE Id = @Id", new { Now = DateTime.UtcNow, employee.Id });

            var token = _jwt.GenerateToken(employee.Id, employee.UserName, roleCode ?? SystemRoles.User, mustChangePassword,
                employee.FirstName, employee.LastName, employee.Email);

            return new TokenResponse(token, mustChangePassword);
        }
    }
}
