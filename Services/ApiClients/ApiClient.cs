using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContentManagementSystem.Services.ApiClients
{
    public class ApiResponseEnvelope<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<bool> PostAsync<T>(string endpoint, T data);
        Task<TResult?> PostAsync<TData, TResult>(string endpoint, TData data);
        Task<bool> PutAsync<T>(string endpoint, T data);
        Task<bool> DeleteAsync(string endpoint);
        Task<bool> PatchAsync(string endpoint);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _systemKey;

        public ApiClient(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5123";
            _systemKey = configuration["ApiSettings:SystemKey"] ?? "CMSPortalSecretKey2026!#";

            if (!_httpClient.BaseAddress?.ToString().Contains("http") ?? true)
            {
                _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
        {
            var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-System-Key", _systemKey);

            var context = _httpContextAccessor?.HttpContext;
            if (context?.User?.Identity?.IsAuthenticated == true)
            {
                var token = context.User.FindFirst("access_token")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            return request;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, endpoint);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API GET {Endpoint} trả về mã lỗi {StatusCode}", endpoint, response.StatusCode);
                    return default;
                }

                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var envelope = JsonSerializer.Deserialize<ApiResponseEnvelope<T>>(content, _jsonOptions);
                    if (envelope != null && envelope.Success)
                    {
                        return envelope.Data;
                    }
                }
                catch { }

                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API GET {Endpoint}: {Message}", endpoint, ex.Message);
                return default;
            }
        }

        public async Task<bool> PostAsync<T>(string endpoint, T data)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, endpoint);
                request.Content = JsonContent.Create(data, options: _jsonOptions);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API POST {Endpoint}: {Message}", endpoint, ex.Message);
                return false;
            }
        }

        public async Task<TResult?> PostAsync<TData, TResult>(string endpoint, TData data)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Post, endpoint);
                request.Content = JsonContent.Create(data, options: _jsonOptions);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var envelope = JsonSerializer.Deserialize<ApiResponseEnvelope<TResult>>(content, _jsonOptions);
                    if (envelope != null && envelope.Success)
                    {
                        return envelope.Data;
                    }
                }
                catch { }

                return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API POST {Endpoint}: {Message}", endpoint, ex.Message);
                return default;
            }
        }

        public async Task<bool> PutAsync<T>(string endpoint, T data)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Put, endpoint);
                request.Content = JsonContent.Create(data, options: _jsonOptions);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API PUT {Endpoint}: {Message}", endpoint, ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Delete, endpoint);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API DELETE {Endpoint}: {Message}", endpoint, ex.Message);
                return false;
            }
        }

        public async Task<bool> PatchAsync(string endpoint)
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Patch, endpoint);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API PATCH {Endpoint}: {Message}", endpoint, ex.Message);
                return false;
            }
        }
    }
}

