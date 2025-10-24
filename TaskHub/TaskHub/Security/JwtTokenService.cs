using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TaskHub.Security
{
    public record JwtOptions
    {
        public string Key { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpirationMinutes { get; init; } = 60;
    }

    public interface IJwtTokenService
    {
        string GenerateToken(string userId, string username, string? email = null, IEnumerable<Claim>? additionalClaims = null);
    }

    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly JwtOptions _options;
        private readonly SymmetricSecurityKey _signingKey;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        }

        public string GenerateToken(string userId, string username, string? email = null, IEnumerable<Claim>? additionalClaims = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            ArgumentException.ThrowIfNullOrWhiteSpace(username);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.UniqueName, username),
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username)
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
            }

            if (additionalClaims is not null)
            {
                claims.AddRange(additionalClaims);
            }

            var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes > 0 ? _options.ExpirationMinutes : 60);

            var token = new JwtSecurityToken(
                issuer: string.IsNullOrWhiteSpace(_options.Issuer) ? null : _options.Issuer,
                audience: string.IsNullOrWhiteSpace(_options.Audience) ? null : _options.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
