using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using projectTracker.Application;
using projectTracker.Infrastructure;
using projectTracker.Infrastructure.BackgroundTask;
using projectTracker.Infrastructure.Extensions;
using projectTracker.Infrastructure.Middleware;
using ProjectTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

//// For ASP.NET Core
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//1.Register all core services (JWT, DbContext, Identity)
builder.Services.AddProjectTrackerServices(builder.Configuration);

// 2. Configure controllers with global authorization
builder.Services.AddControllers(options =>
{
    options.Filters.Add<CustomAuthorizeAttribute>(); // ? Correct global filter registration
});

// 3. Swagger configuration with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Project Tracker API", Version = "v1" });

    // Add JWT Auth to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});



builder.Services.AddInfrastructure();
builder.Services.AddApplication();
// 4. Register other services
builder.Services.AddHttpClient();
builder.Services.AddHostedService<JiraSyncService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

//app.UseCors("AllowFrontend");

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
// Critical order: Authentication -> Authorization -> Controllers

app.UseHttpsRedirection();

// ?? Must be in this exact order:
app.UseAuthentication(); // Validates JWT tokens
app.UseAuthorization();  // Enables [Authorize] and your CustomAuthorizeAttribute

app.MapControllers();

// Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed initial data
//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();

//    await DbSeeder.Seed(dbContext, userManager, roleManager);
//}

app.Run();