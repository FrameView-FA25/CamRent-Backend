using CamRent_Api.Commons;
using CamRent_Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Api
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
		{
			// Bind options
			services.Configure<JwtOptions>(config.GetSection("Jwt"));
			var jwt = config.GetSection("Jwt").Get<JwtOptions>()!;

			var keyBytes = Encoding.UTF8.GetBytes(jwt.Key);
			var signingKey = new SymmetricSecurityKey(keyBytes);

			services
				.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = jwt.Issuer,
						ValidAudience = jwt.Audience,
						IssuerSigningKey = signingKey,
						ClockSkew = TimeSpan.Zero
					};
				});

			services.AddAuthorization(options =>
			{
				options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
				options.AddPolicy("BranchManager", p => p.RequireRole("BranchManager", "Admin"));
				options.AddPolicy("Staff", p => p.RequireRole("Staff", "Admin"));
				options.AddPolicy("Owner", p => p.RequireRole("Owner", "Admin"));
				options.AddPolicy("Renter", p => p.RequireRole("Renter", "Admin"));
			});

			return services;
		}

		public static IServiceCollection AddSwaggerGen(this IServiceCollection services)
		{
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "CamRent_Api", Version = "v1" });

				// FIX schemaId conflict
				c.CustomSchemaIds(type =>
				{
					if (type.IsGenericType)
					{
						var name = type.Name[..type.Name.IndexOf('`')];
						var args = string.Join(",", type.GetGenericArguments().Select(a => a.Name));
						return $"{type.Namespace}.{name}[{args}]".Replace("+", ".");
					}
					return (type.FullName ?? type.Name).Replace("+", ".");
				});

				// JWT bearer (chỉ cần nhập token, Swagger tự thêm "Bearer ")
				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "Dán JWT access token.",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,  // <-- quan trọng
					Scheme = "bearer",               // <-- quan trọng
					BearerFormat = "JWT"
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme {
							Reference = new OpenApiReference {
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						},
						Array.Empty<string>()
					}
				});
			});
			return services;
		}

		public static IServiceCollection AddApiDI(this IServiceCollection services)
		{

			
			services.AddAutoMapper(
			   typeof(MappingProfileApi).Assembly,
			   typeof(MappingProfileApplication).Assembly // assembly của Application
			);
			return services;
		}

	}
}
