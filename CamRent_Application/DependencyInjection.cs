using CamRent_Application.Common;
using CamRent_Application.IServices;
using CamRent_Application.Services;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CamRent_Application
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplicationDI(this IServiceCollection services)
		{
			services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

			services.AddScoped<IBranchService, BranchService>();
			services.AddScoped<IAccessoryService, AccessoryService>();
			services.AddScoped<IAuthService, AuthService>();
			services.AddScoped<ICameraService, CameraService>();
			services.AddScoped<IBookingService, BookingService>();
			services.AddScoped<IPricingService, PricingService>();
			services.AddScoped<IAvailabilityService, AvailabilityService>();
			services.AddScoped<IPaymentService, PaymentService>();
			services.AddScoped<IQrPaymentService, QrPaymentService>();
			services.AddScoped<IContractService, ContractService>();
			services.AddScoped<IContractTemplateService, ContractTemplateService>();
			services.AddScoped<IInspectionService, InspectionService>();
			services.AddScoped<IDeliveryService, DeliveryService>();
			services.AddScoped<IVerificationService, VerificationService>();
			services.AddScoped<IReviewService, ReviewService>();
			services.AddScoped<ICategoryService, CategoryService>();
			services.AddScoped<IComboService, ComboService>();
			services.AddScoped<IUserService, UserService>();
			return services;
		}
	}
}
