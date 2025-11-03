using CamRent_Api;
using CamRent_Api.HostedServices;
using CamRent_Api.Validators;
using CamRent_Application;
using CamRent_Infrastructure;
using CamRent_Infrastructure.Persistence;
using CamRent_Infrastructure.Persistence.SeedData;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequestValidator>();


// Register DI from layers
builder.Services
	.AddInfrastructureDI(builder.Configuration)
	.AddApplicationDI()
	.AddSwaggerGen()
	.AddJwtAuthentication(builder.Configuration);

// Hosted services
builder.Services.AddHostedService<BookingStatusHostedService>();
builder.Services.AddHostedService<DemoDataSeeder>();

var app = builder.Build();

// Global error handling -> ProblemDetails
app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
		var problem = new ProblemDetails
		{
			Status = StatusCodes.Status500InternalServerError,
			Title = "An unexpected error occurred",
			Detail = exceptionHandlerPathFeature?.Error.Message,
			Instance = context.Request.Path
		};
		context.Response.StatusCode = problem.Status.Value;
		context.Response.ContentType = "application/problem+json";
		await context.Response.WriteAsJsonAsync(problem);
	});
});

var applyMigrations = Environment.GetEnvironmentVariable("APPLY_MIGRATIONS") == "true";
if (applyMigrations)
{
	await using var scope = app.Services.CreateAsyncScope();
	var db = scope.ServiceProvider.GetRequiredService<CamRentDbContext>();

	var pending = await db.Database.GetPendingMigrationsAsync();
	if (pending.Any())
	{
		await db.Database.MigrateAsync();
		// TODO: Seed nếu cần
	}
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Route gốc: chuyển sang Swagger hoặc trả JSON
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription(); // hoặc Results.Json(new { app="CamRent API", ok=true })

// Health check
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
