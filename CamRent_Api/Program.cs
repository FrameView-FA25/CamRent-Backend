using CamRent_Api.HostedServices;
using CamRent_Application;
using CamRent_Infrastructure;
using CamRent_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<CamRentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "CamRent_Api", Version = "v1" });

	// QUAN TRỌNG: đặt schemaId theo FullName và thay dấu '+' của nested types
	c.CustomSchemaIds(type =>
	{
		// xử lý cả generic types cho chắc
		if (type.IsGenericType)
		{
			var name = type.Name[..type.Name.IndexOf('`')];
			var args = string.Join(",", type.GetGenericArguments().Select(a => a.Name));
			return $"{type.Namespace}.{name}[{args}]".Replace("+", ".");
		}
		return (type.FullName ?? type.Name).Replace("+", ".");
	});
});
builder.Services.AddHealthChecks();

// Register DI from layers
builder.Services
	.AddInfrastructureDI()
	.AddApplicationDI();

// Hosted services
builder.Services.AddHostedService<BookingStatusHostedService>();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
