using System.Threading;
using System.Threading.Tasks;

namespace GymCrudApp.Services
{
    public interface IOcrService
    {
        Task<string> ExtractRawTextAsync(
            byte[] fileBytes,
            string mimeType,
            CancellationToken cancellationToken = default);
    }
}