using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Added for Include()
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using projectTracker.Application.Interfaces;
using ProjectTracker.Infrastructure.Data;

namespace ProjectTracker.Infrastructure.Services
{
    public class AuthorizationService : IAuthorization
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService; // Fixed typo
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;

        public AuthorizationService(
            AppDbContext context,
            IHttpContextAccessor httpContext,
            IConfiguration configuration,
            ITokenService tokenGenerator) // Fixed parameter name
        {
            _context = context;
            _httpContext = httpContext;
            _configuration = configuration;
            _tokenService = tokenGenerator;
        }

        public IEnumerable<Claim>? GetClaims(string token)
        {
            var jwtKey = _configuration["Jwt:SecretKey"]; // Match this with your appsettings.json
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // Don't validate expiration here (can be handled elsewhere)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                return principal.Claims;
            }
            catch
            {
                return null;
            }
        }

        //public bool IsAuthenticated(string token)
        //{
        //    var securityToken = _tokenService.Decrypt(token, _configuration["Jwt:Key"]);
        //    return securityToken != null &&
        //           securityToken.ValidTo != DateTime.MinValue &&
        //           securityToken.ValidTo > DateTime.UtcNow;
        //}

        public async Task<bool> IsAuthorizedAsync(string userId, string action) // Fixed return type
        {
            var roles = await _context.UserRoles
                .Where(a => a.UserId == userId)
                .Select(r => r.RoleId)
                .ToListAsync(); // Added Async

            if (!roles.Any()) return false;

            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => roles.Contains(rp.RoleId))
                .AnyAsync(rp =>
                    string.Equals(action, rp.Permission.Action, StringComparison.OrdinalIgnoreCase));

            return hasPermission;
        }

       
    }
}