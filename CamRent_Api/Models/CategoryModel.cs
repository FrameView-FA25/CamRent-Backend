using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class CategoryModel
	{
		public class CreateCategoryRequest { [Required, MinLength(2)] public string Name { get; set; } = string.Empty; public Guid? ParentId { get; set; } }
		public class LinkRequest { [Required] public Guid CategoryId { get; set; } [Required] public Guid DeviceId { get; set; } }
	}
}
