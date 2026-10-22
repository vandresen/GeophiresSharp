using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GeophiresLibrary.Services
{
    public class LLMBaseService : ILLMBaseService
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly IHttpClientFactory _httpClientFactory;

        public LLMBaseService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> SendRawAsync(
            HttpMethod method,
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient();
            using var request = BuildRequest(method, url, body, headers);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(DefaultTimeout);

            try
            {
                using var response = await client.SendAsync(request, timeout.Token);
                var responseText = await response.Content.ReadAsStringAsync(timeout.Token);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"{(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(responseText)}",
                        null,
                        response.StatusCode);

                return responseText;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"No response from {new Uri(url).Host} within {DefaultTimeout.TotalSeconds:N0} seconds.");
            }
        }

        public async IAsyncEnumerable<string> StreamRawAsync(
            string url,
            object? body = null,
            Dictionary<string, string>? headers = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient();
            using var request = BuildRequest(HttpMethod.Post, url, body, headers);

            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"{(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(error)}",
                    null,
                    response.StatusCode);
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                yield return line;
            }
        }

        private static HttpRequestMessage BuildRequest(
            HttpMethod method,
            string url,
            object? body,
            Dictionary<string, string>? headers)
        {
            var request = new HttpRequestMessage(method, url);

            if (headers != null)
                foreach (var (key, value) in headers)
                    request.Headers.TryAddWithoutValidation(key, value);

            if (body != null)
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            return request;
        }

        private static string Truncate(string s, int max = 1000) =>
            s.Length <= max ? s : s[..max] + "...";
    }
}