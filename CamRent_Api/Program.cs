using CamRent_Infrastructure.Persistence;
using CamRent_Infrastructure;
using CamRent_Application;
using CamRent_Api.HostedServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<CamRentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Register DI from layers
builder.Services
	.AddInfrastructureDI()
	.AddApplicationDI();

// Hosted services
builder.Services.AddHostedService<BookingStatusHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Route gốc: chuyển sang Swagger hoặc trả JSON
app.MapGet("/", () => Results.Redirect("/swagger")); // hoặc Results.Json(new { app="CamRent API", ok=true })

// Health check
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
