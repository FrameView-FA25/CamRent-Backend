using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CamRent_Api.Swagger
{
	public class AuthorizeCheckOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			// Skip if AllowAnonymous present on method or controller
			var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
				|| context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;
			if (hasAllowAnonymous) return;

			// Collect [Authorize] attributes on method + controller
			var authorizeAttrs = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
				.Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>())
				.ToList();

			if (!authorizeAttrs.Any()) return;

			// Add security requirement -> Swagger UI shows lock icon
			operation.Security ??= new System.Collections.Generic.List<OpenApiSecurityRequirement>();
			var schemeRef = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } };
			operation.Security.Add(new OpenApiSecurityRequirement { [schemeRef] = Array.Empty<string>() });

			// Append roles (if present) to operation summary
			var roles = authorizeAttrs
				.Where(a => !string.IsNullOrWhiteSpace(a.Roles))
				.SelectMany(a => a.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.Distinct()
				.ToList();

			if (roles.Any())
			{
				var rolesText = string.Join(", ", roles);
				if (!string.IsNullOrWhiteSpace(operation.Summary))
					operation.Summary = $"{operation.Summary} ({rolesText})";
				else if (!string.IsNullOrWhiteSpace(operation.Description))
					operation.Description = $"{operation.Description} ({rolesText})";
				else
					operation.Summary = $"(Roles: {rolesText})";
			}
		}
	}
}
