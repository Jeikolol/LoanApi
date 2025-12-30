using Application.Exceptions;
using Application.Features.Commands;
using Application.Services;
using Dapper;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            string query = "SELECT * FROM Employee WHERE Username = @UserName";

            var args = new
            {
               request.UserName,
               request.RememberMe
            };

            var employee = await _connection.QueryFirstOrDefaultAsync<Employee>(query, args);

            if (employee == null)
                throw new NotFoundException("Invalid username or password.");

            var hasher = new PasswordHasher<Employee>();

            var passRes = hasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password);

            if (passRes == PasswordVerificationResult.Failed)
                throw new UnauthorizedException("Invalid username or password.");

            var token = _jwt.GenerateToken(employee.Id, employee.UserName);

            return new TokenResponse(token);
        }
    }
}
