using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Domain.Entities
{
	public class MoneyFlatformSetting : BaseEntity
	{

		// 1️⃣ % khách phải thanh toán trước khi tạo Booking
		public decimal UpfrontPercent { get; set; }  // ví dụ 0.10m = 10%

		// 2️⃣ % phí nền tảng CamRent thu trên tiền thuê
		public decimal PlatformFeePercent { get; set; }  // ví dụ 0.20m = 20%

		// 3️⃣ % chia lại Owner từ phần tiền thuê ròng
		public decimal OwnerSharePercent { get; set; }  // ví dụ 0.75m = 75%

		// 4️⃣ Phạt trả trễ:
		public int LateFeeFirstNDays { get; set; }       // số ngày đầu áp dụng hệ số 1.5
		public decimal LateFeeFactorFirstN { get; set; } // 1.5m
		public decimal LateFeeFactorAfter { get; set; }  // 2.0m sau N ngày

		// 5️⃣ Downtime fee:
		public decimal DowntimeFactor { get; set; }      // ví dụ 0.5 × giá/ngày

		// Chỉ có 1 record duy nhất trong bảng
		public bool IsActive { get; set; } = true;
	}
}
