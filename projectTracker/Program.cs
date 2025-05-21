using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Interfaces;
using projectTracker.Infrastructure.Adapter;
using projectTracker.Infrastructure.BackgroundTask;
using projectTracker.Infrastructure.Risk;
using projectTracker.Infrastructure.SyncManager;
using ProjectTracker.Infrastructure.Data; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IProjectManegementAdapter, JiraAdapter>();
builder.Services.AddScoped<IRiskCalculatorService, RiskCalculationService>();
// Register as a hosted service
builder.Services.AddHostedService<JiraSyncService>();

// Keep your SyncManager as scoped
builder.Services.AddScoped<ISyncManager, SyncManager>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .LogTo(Console.WriteLine, LogLevel.Information));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();