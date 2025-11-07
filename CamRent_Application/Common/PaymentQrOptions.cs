namespace CamRent_Application.Common
{
    public class PaymentQrOptions
    {
        public string BankBin { get; set; } = "970415"; // default: Techcombank
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 15;
        public string ContentTemplate { get; set; } = "CR-{PaymentId}"; // transfer memo
    }
}


