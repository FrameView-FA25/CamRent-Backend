using System.ComponentModel.DataAnnotations;

namespace CamRent_Domain.Common
{
    public enum UserRole
    {
        Guest = 0,
        Renter = 1,
        Owner = 2,
        BranchManager = 3,
        Staff = 4,
        Admin = 5
    }

    public enum UserStatus
    {
        Ban = 0,
        Active = 1,
    }
    public enum FileOwnerType
    {
        Camera = 1,
        Accessory = 2,
        UserAvatar = 3,
        Combo = 4,
        Inspection = 5,
        ContractDocument = 6
    }
    public enum DeviceCategory
    {
        Camera = 1,
        Lens = 2,
        Accessory = 3
    }

    public enum AssetLocation
    {
        Platform = 0,   // At platform / branch / warehouse
        WithOwner = 1,  // Currently with owner
        WithRenter = 2, // Currently with renter
        InDelivery = 3,  // Being delivered
        Maintenance = 4 // Under maintenance / inspection
    }

    public enum BookingStatus
    {
        [Display(Name = "Giỏ hàng")]
        Draft = 0,

        [Display(Name = "Chờ duyệt")]
        PendingApproval = 1,

        [Display(Name = "Đã xác nhận")]
        Confirmed = 2,

        [Display(Name = "Đã nhận máy")]
        PickedUp = 3,

        [Display(Name = "Đang sử dụng")]
        InUse = 4,

        [Display(Name = "Đã trả")]
        Returned = 5,

        [Display(Name = "Hoàn tất")]
        Completed = 6,

        [Display(Name = "Đã hủy")]
        Cancelled = 7,

        [Display(Name = "Quá hạn")]
        Overdue = 8,

        [Display(Name = "Vắng mặt")]
        NoShow = 9
    }

    public enum BookingType
    {
        Rental = 1,
        Blockout = 2
    }

    public enum InspectionType
    {
        Booking = 1,
        Verification = 2
    }


    public enum DeliveryTaskStatus
    {
        Assigned = 1,
        InTransit = 2,
        Delivered = 3,
        Returned = 4,
        Cancelled = 5
    }

    public enum ContractStatus
    {
        Draft = 1,
        Sent = 2,
        Signed = 3,
        Cancelled = 4,
        Completed = 5
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Authorized = 2,
        Captured = 3,
        Refunded = 4,
        Failed = 5
    }

    public enum ReviewStatus
    {
        Pending,      // Chờ duyệt
        Approved,     // Đã duyệt
        Rejected,     // Bị từ chối (bình luận xấu)
        Flagged       // Bị báo cáo
    }

    public enum ItemType
    {
        Camera = 1,
        Accessory = 2,
        Combo = 3
    }

    public enum HandoverType
    {
        Pickup = 0,  // Giao / nhận lúc bắt đầu
        Return = 1   // Giao / nhận lúc kết thúc
    }

    public enum HandoverPartyType
    {
        Owner = 0,
        Renter = 1
    }

    public enum VerificationStatus
    {
        Pending = 0,
        Verified = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum ContractType
    {
        Booking = 1,
        Verification = 2
	}
}

