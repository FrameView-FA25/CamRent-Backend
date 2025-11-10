namespace CamRent_Infrastructure.Email
{
	public class EmailOptions
	{
		public string SmtpHost { get; set; } = string.Empty;
		public int SmtpPort { get; set; } = 587;
		public bool EnableSsl { get; set; } = true;
		public string User { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string From { get; set; } = string.Empty;
		public string FromName { get; set; } = "CamRent";
	}
}


