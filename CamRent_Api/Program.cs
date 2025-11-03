using CamRent_Infrastructure.Persistence;
using CamRent_Infrastructure;
using CamRent_Application;
using CamRent_Api.HostedServices;
using CamRent_Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation.AspNetCore;
using FluentValidation;
using CamRent_Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<CamRentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingRequestValidator>();

// Auth
builder.Services
	.AddAuthentication(HeaderAuthHandler.SchemeName)
	.AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>(HeaderAuthHandler.SchemeName, null);

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
	options.AddPolicy("BranchManager", p => p.RequireRole("BranchManager", "Admin"));
	options.AddPolicy("Delivery", p => p.RequireRole("Delivery", "Admin"));
	options.AddPolicy("Owner", p => p.RequireRole("Owner", "Admin"));
	options.AddPolicy("Renter", p => p.RequireRole("Renter", "Admin"));
});

// Register DI from layers
builder.Services
	.AddInfrastructureDI()
	.AddApplicationDI();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
