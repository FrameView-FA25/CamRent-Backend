using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.Common
{
	public class AppException : Exception
	{
		public HttpStatusCode StatusCode { get; }

		public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
			: base(message)
		{
			StatusCode = statusCode;
		}
	}
}
