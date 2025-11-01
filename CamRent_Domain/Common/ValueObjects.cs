namespace CamRent_Domain.Common
{
    public class Money
    {
        public string Currency { get; set; } = "VND";
        public decimal Amount { get; set; }
    }

    public class Address
    {
        public string Country { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

