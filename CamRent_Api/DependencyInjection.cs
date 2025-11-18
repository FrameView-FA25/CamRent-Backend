using CamRent_Api.Commons;
using CamRent_Api.Swagger;
using CamRent_Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
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
				options.AddPolicy("OwnerOrManagerOrStaff", p => p.RequireRole("Owner", "BranchManager", "Staff", "Admin"));

				// ⚠️ Bỏ FallbackPolicy để Swagger và các endpoint không có [Authorize] không bị ép đăng nhập
				// Nếu muốn tất cả API (trừ [AllowAnonymous]) bắt buộc đăng nhập, cần dùng [Authorize] ở controller/action.
				// options.FallbackPolicy = new AuthorizationPolicyBuilder()
				// 	.RequireAuthenticatedUser()
				// 	.Build();
			});

			return services;
		}

		public static IServiceCollection AddSwaggerGen(this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo { Title = "CamRent_Api", Version = "v1" });

				options.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
				{
					Type = "string",
					Format = "binary"
				});

				// FIX schemaId conflict
				options.CustomSchemaIds(type =>
				{
					if (type.IsGenericType)
					{
						var name = type.Name[..type.Name.IndexOf('`')];
						var args = string.Join(",", type.GetGenericArguments().Select(a => a.Name));
						return $"{type.Namespace}.{name}[{args}]".Replace("+", ".");
					}
					return (type.FullName ?? type.Name).Replace("+", ".");
				});

				// Enable annotations if you use [SwaggerOperation], [SwaggerResponse], etc.
				options.EnableAnnotations();

				// JWT bearer (chỉ cần nhập token, Swagger tự thêm "Bearer ")
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "Dán JWT access token.",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT"
				});

				options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

				// Operation filter to: add lock icon per-action + append roles to summaries
				options.OperationFilter<AuthorizeCheckOperationFilter>();

				// Optional: include XML comments for better summaries/descriptions
				var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				if (File.Exists(xmlPath))
				{
					options.IncludeXmlComments(xmlPath);
				}
			});
			return services;
		}

		public static IServiceCollection AddApiDI(this IServiceCollection services, IConfiguration config)
		{

			services.AddControllers()
			.AddJsonOptions(o =>
			{
				o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
				// tuỳ chọn:
				// o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
				// o.JsonSerializerOptions.MaxDepth = 64; // nếu dữ liệu sâu
			});
			services.AddAutoMapper(
			   typeof(MappingProfileApi).Assembly,
			   typeof(MappingProfileApplication).Assembly // assembly của Application
			);
			services.Configure<CloudinarySettings>(
			config.GetSection("Cloudinary"));
			return services;
		}

	}
}
