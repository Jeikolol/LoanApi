using Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerateToken(Guid userId, string username, string role, bool mustChangePassword = false, string? firstName = null, string? lastName = null, string? email = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Lets the API refuse tokens issued before an employee was deactivated, demoted or had the password reset.
                new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrWhiteSpace(firstName)) claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, firstName));
            if (!string.IsNullOrWhiteSpace(lastName)) claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, lastName));
            if (!string.IsNullOrWhiteSpace(email)) claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

            if (mustChangePassword)
            {
                claims.Add(new Claim("must_change_password", "true"));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));

            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public interface IJwtTokenService : ITransientService
    {
        string GenerateToken(Guid userId, string username, string role, bool mustChangePassword = false, string? firstName = null, string? lastName = null, string? email = null);
    }
}
