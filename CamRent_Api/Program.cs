using CamRent_Api;
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
using CamRent_Application.Common;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

builder.Services.AddFluentValidationAutoValidation();

const string CorsAllowAll = "AllowAll";
builder.Services.AddCors(options =>
{
	options.AddPolicy(CorsAllowAll, policy =>
		policy
			.AllowAnyOrigin()   // ✅ KHÔNG dùng kèm AllowCredentials()
			.AllowAnyMethod()
			.AllowAnyHeader()
	);
});

// Register DI from layers
builder.Services
	.AddInfrastructureDI(builder.Configuration)
	.AddApplicationDI()
	.AddSwaggerGen()
	.AddJwtAuthentication(builder.Configuration)
	.AddApiDI();

// Options
builder.Services.Configure<VnPayOptions>(builder.Configuration.GetSection("VNPay"));

// Hosted services

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
	var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();

	var pending = await db.Database.GetPendingMigrationsAsync();
	if (pending.Any())
	{
		await db.Database.MigrateAsync();
	}
	await seeder.RunAsync();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Route gốc: chuyển sang Swagger hoặc trả JSON
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription(); // hoặc Results.Json(new { app="CamRent API", ok=true })

// Health check
app.MapHealthChecks("/health");

app.UseHttpsRedirection();

app.UseCors(CorsAllowAll);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Đảm bảo preflight OPTIONS luôn 200
app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.Ok()).ExcludeFromDescription();

app.Run();
