using System.Net.Http.Json;

namespace MaidenAIAgent.Infrastructure.Services
{
    public interface IExternalAPIService
    {
        Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body);
    }

    public class ExternalAPIService : IExternalAPIService
    {
        private readonly HttpClient _httpClient;

        public ExternalAPIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? queryParams = null)
        {
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                endpoint = $"{endpoint}?{queryString}";
            }

            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, body);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
    }
}
