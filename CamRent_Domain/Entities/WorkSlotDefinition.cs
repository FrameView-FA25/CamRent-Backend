using CamRent_Domain.Common;

namespace CamRent_Domain.Entities
{
	/// <summary>
	/// Cấu hình khung giờ làm việc (slot) cho lịch hiển thị.
	/// Admin có thể chỉnh thời lượng/mốc giờ của từng slot.
	/// Đây chỉ là cấu hình UI, không thay đổi dữ liệu Booking/Verification thực tế.
	/// </summary>
	public class WorkSlotDefinition : BaseEntity
	{
		public int SlotIndex { get; set; }          // 0..12
		public TimeSpan StartTime { get; set; }     // 08:00
		public TimeSpan EndTime { get; set; }       // 10:00

		// Áp dụng cho toàn bộ hệ thống (không phân branch)
		public bool IsActive { get; set; } = true;
	}
}


