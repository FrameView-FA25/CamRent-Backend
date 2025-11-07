using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
    public interface IContractTemplateService
    {
        Task<byte[]> GeneratePreviewPdfAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }
}


