using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeophiresLibrary.Services
{
    public interface ILLMBaseService
    {
        Task<string> SendRawAsync(
            HttpMethod method,
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            CancellationToken ct = default);

        IAsyncEnumerable<string> StreamRawAsync(
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            CancellationToken ct = default);
    }
}
