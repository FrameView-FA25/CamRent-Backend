using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.Common
{
	public static class EnumExtensions
	{
		public static string GetDisplayName(this Enum value)
		{
			var mem = value.GetType().GetMember(value.ToString());
			if (mem.Length > 0)
			{
				var attr = mem[0].GetCustomAttributes(typeof(DisplayAttribute), false)
								 .Cast<DisplayAttribute>()
								 .FirstOrDefault();
				if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
					return attr.Name!;
			}
			return value.ToString();
		}
	}
}
