using CamRent_Application.Interfaces;
using CamRent_Infrastructure.Data;
using CamRent_Infrastructure.Weaviate;
using CamRent_Infrastructure.Persistence;
using CamRent_Infrastructure.Persistence.SeedData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CamRent_Infrastructure.Email;
using CamRent_Application.Common;
using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Infrastructure.Embeddings;

namespace CamRent_Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructureDI(this IServiceCollection services, IConfiguration config)
		{
			services.AddDbContext<CamRentDbContext>(options =>options.UseNpgsql(config.GetConnectionString("Default")));

			services.AddScoped<DemoDataSeeder>();

			services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			// Register concrete open-generic so concrete type can be resolved explicitly
			services.AddScoped(typeof(GenericRepository<>));
			services.AddScoped<IUnitOfWork, UnitOfWork>();

			// Weaviate
			services.Configure<WeaviateOptions>(config.GetSection("Weaviate"));
			services.AddHttpClient<IWeaviateClient, WeaviateClient>((sp, http) =>
			{
				var opts = sp.GetRequiredService<IOptions<WeaviateOptions>>().Value;
				http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/'));
				if (!string.IsNullOrWhiteSpace(opts.ApiKey))
				{
					http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);
				}
				if (!string.IsNullOrWhiteSpace(opts.AdditionalAuthHeaderName) && !string.IsNullOrWhiteSpace(opts.AdditionalAuthHeaderValue))
				{
					http.DefaultRequestHeaders.Add(opts.AdditionalAuthHeaderName, opts.AdditionalAuthHeaderValue);
				}
			});
			services.AddSingleton<IVectorStore, WeaviateVectorStore>();
			services.AddSingleton<IIndexingService, IndexingBackgroundService>();
			services.AddHostedService(sp => (IndexingBackgroundService)sp.GetRequiredService<IIndexingService>());

			// Email
			services.Configure<EmailOptions>(config.GetSection("Email"));
			services.AddScoped<IEmailService, SmtpEmailService>();

			// Embeddings (Gemini)
			services.Configure<GeminiOptions>(config.GetSection("Gemini"));
			services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();
			return services;
		}
	}
}
