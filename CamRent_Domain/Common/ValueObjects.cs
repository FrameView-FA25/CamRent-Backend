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
    }
}

