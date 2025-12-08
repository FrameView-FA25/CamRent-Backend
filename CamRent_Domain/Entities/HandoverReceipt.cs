using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Domain.Entities
{
	public class HandoverReceipt : BaseEntity
	{
		// Liên kết với Contract (ContractInstance trước đó)
		public Guid ContractId { get; set; }
		public Contract Contract { get; set; } = default!;

		// Loại biên bản: Nhận hay Trả
		public HandoverType Type { get; set; }

		// Làm việc với ai: Owner hay Renter
		public HandoverPartyType PartyType { get; set; }

		// User bên kia ký nhận (Owner hoặc Renter)
		public Guid? UserId { get; set; }
		public User? User { get; set; } 

		// Nhân viên/Staff của platform lập biên bản
		public User? Staff { get; set; }

		// Thực hiện tại chi nhánh nào
		public Guid? BranchId { get; set; }
		public Branch? Branch { get; set; }

		// Liên kết Inspection nếu có (check condition, chụp hình, v.v.)
		public Guid? InspectionId { get; set; }
		public Inspection? Inspection { get; set; }

		// Thời điểm giao/nhận thực tế
		public DateTime HandoverAt { get; set; } = DateTime.UtcNow;

		// Link tới file chữ ký / pdf biên bản
		public string? PartySignatureUrl { get; set; }
	}
}
