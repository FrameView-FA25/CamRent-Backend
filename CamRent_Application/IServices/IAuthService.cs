using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CamRent_Application.DTOs.AuthDTO;

namespace CamRent_Application.IServices
{
	public interface IAuthService
	{
		Task<AuthResponse> GetToken(string email, string password);
		Task<bool> Register(RegisterRequest user);
	}
}
