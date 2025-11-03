using CamRent_Api.Models;
using FluentValidation;

namespace CamRent_Api.Validators
{
	public class CreateBookingRequestValidator : AbstractValidator<BookingModel.CreateBookingRequest>
	{
		public CreateBookingRequestValidator()
		{
			RuleFor(x => x.RenterId).NotEmpty();
			RuleFor(x => x.PickupAt).NotEmpty();
			RuleFor(x => x.ReturnAt).NotEmpty();
			RuleFor(x => x).Must(x => x.ReturnAt > x.PickupAt)
				.WithMessage("ReturnAt must be greater than PickupAt");
		}
	}

	public class AddItemRequestValidator : AbstractValidator<BookingModel.AddItemRequest>
	{
		public AddItemRequestValidator()
		{
			RuleFor(x => x.Quantity).GreaterThan(0);
			RuleFor(x => x).Must(x => (x.CameraId.HasValue ^ x.AccessoryId.HasValue))
				.WithMessage("Exactly one of CameraId or AccessoryId must be provided");
		}
	}

	public class UpdateTimesRequestValidator : AbstractValidator<BookingModel.UpdateTimesRequest>
	{
		public UpdateTimesRequestValidator()
		{
			RuleFor(x => x.PickupAt).NotEmpty();
			RuleFor(x => x.ReturnAt).NotEmpty();
			RuleFor(x => x).Must(x => x.ReturnAt > x.PickupAt)
				.WithMessage("ReturnAt must be greater than PickupAt");
		}
	}
}
