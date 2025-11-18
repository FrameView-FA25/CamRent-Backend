using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IContractService
	{
		Task<Guid> CreateInstanceAsync(Guid bookingId, Guid templateId);
		Task MarkSignedAsync(Guid contractInstanceId, string? signedFileUrl);
		/// <summary>
		/// Tự động tạo (nếu chưa có) và sinh file PDF hợp đồng chính thức cho một booking,
		/// lưu file lên storage và cập nhật trạng thái Contract.
		/// Trả về Id của Contract.
		/// </summary>
		Task<Guid> GenerateAndStoreContractAsync(Guid bookingId, CancellationToken cancellationToken = default);
	}
}
