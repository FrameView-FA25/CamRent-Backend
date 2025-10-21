namespace CamRent_Domain.Common
{
    public enum UserRole
    {
        Guest = 0,
        Renter = 1,
        Owner = 2,
        BranchManager = 3,
        Delivery = 4,
        Admin = 5
    }

    public enum DeviceCategory
    {
        Camera = 1,
        Lens = 2,
        Accessory = 3
    }

    public enum OwnershipType
    {
        Owner = 1,
        Platform = 2
    }

    public enum BookingStatus
    {
        Draft = 0,
        PendingApproval = 1,
        Confirmed = 2,
        PickedUp = 3,
        InUse = 4,
        Returned = 5,
        Completed = 6,
        Cancelled = 7,
        Overdue = 8,
        NoShow = 9
    }

    public enum BookingType
    {
        Rental = 1,
        Blockout = 2
    }

    public enum InspectionType
    {
        Pre = 1,
        Post = 2
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
        Cancelled = 4
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Authorized = 2,
        Captured = 3,
        Refunded = 4,
        Failed = 5
    }
}

