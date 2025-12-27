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
			services.AddScoped<IPayOsService, PayOsService>();
			services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
			services.AddScoped<IWalletService, WalletService>();
			services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
			services.AddScoped<IBranchService, BranchService>();
			services.AddScoped<IAccessoryService, AccessoryService>();
			services.AddScoped<IAuthService, AuthService>();
			services.AddScoped<ICameraService, CameraService>();
			services.AddScoped<IBookingService, BookingService>();
			services.AddScoped<IPaymentService, PaymentService>();
			services.AddScoped<IContractService, ContractService>();
			services.AddScoped<IContractTemplateService, ContractTemplateService>();
			services.AddScoped<IInspectionService, InspectionService>();
			services.AddScoped<IInspectionChecklistService, InspectionChecklistService>();
			services.AddScoped<IInspectionMethodService, InspectionMethodService>();
			services.AddScoped<IInspectionFormService, InspectionFormService>();
			services.AddScoped<IDeliveryService, DeliveryService>();
			services.AddScoped<IDashboardService, DashboardService>();
			services.AddScoped<IVerificationService, VerificationService>();
			services.AddScoped<IReviewService, ReviewService>();
			services.AddScoped<ICategoryService, CategoryService>();
			services.AddScoped<IComboService, ComboService>();
			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IAIRecommendationService, AIRecommendationService>();
			services.AddScoped<IDisputeService, DisputeService>();
			services.AddScoped<IPasswordResetService, PasswordResetService>();
			services.AddScoped<IWorkSlotService, WorkSlotService>();
			services.AddScoped<IHomePageService, HomePageService>();
			services.AddScoped<IMoneyPlatformSettingsService, MoneyPlatformSettingsService>();
			services.AddScoped<IBookingReportService, BookingReportService>();
			return services;
		}
	}
}
