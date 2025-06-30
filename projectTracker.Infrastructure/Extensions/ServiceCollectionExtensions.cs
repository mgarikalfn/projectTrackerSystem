using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using projectTracker.Infrastructure.Adapter;
using projectTracker.Infrastructure.Middleware;
using projectTracker.Infrastructure.Services;
using ProjectTracker.Infrastructure.Data;
using ProjectTracker.Infrastructure.Services;
using System.Text;

namespace projectTracker.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProjectTrackerServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Database Configuration
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Identity Configuration
            services.AddIdentity<AppUser, UserRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.User.RequireUniqueEmail = false; // Keep this if you allow null/duplicate emails for sync
                                                         // FIX: Add allowed special characters for UserName.
                                                         // Jira Account IDs often contain ':' and '-'.
                                                         // Include common characters like '.', '_', '@' if you might use emails as usernames elsewhere
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+:"; // ADDED ':' and '-'

                // Configure Password options (the dummy password "TempPass!123" is usually fine with default options,
                // but check if your custom settings are stricter if you still face password errors)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true; // Make sure this is true for "TempPass!123"
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // 3. JWT Authentication
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Strict token expiration validation
                };

                // For debugging auth failures
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    }
                };
            });

            // 4. Application Services
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAuthorization, AuthorizationService>();

            // Don't register CustomAuthorizeAttribute here - it's not a service
            // It's registered as a filter in Program.cs

            return services;
        }
    }
}
