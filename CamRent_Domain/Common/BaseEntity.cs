using System;

namespace CamRent_Domain.Common
{
	public abstract class BaseEntity
	{
		public Guid Id { get; set; }

		public DateTime CreatedAt { get; set; }      // lưu UTC
		public Guid? CreatedByUserId { get; set; }

		public DateTime? UpdatedAt { get; set; }
		public Guid? UpdatedByUserId { get; set; }
	}

}

