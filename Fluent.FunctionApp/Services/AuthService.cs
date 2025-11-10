using Azure.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Fluent.FunctionApp.Settings;
using Fluent.Models;

namespace Fluent.FunctionApp.Services
{
    public interface IAuthService
    {
        Task<string> GetAccessTokenAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IOptions<OAuth2Settings> _authSettings;
        private readonly IMemoryCache _cache;
        private readonly HttpClient _httpClient;
        private const string CacheKey = "AuthToken";

        public AuthService(
            ILogger<AuthService> logger,
            IOptions<OAuth2Settings> authSettings,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authSettings = authSettings ?? throw new ArgumentNullException(nameof(authSettings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClient = httpClientFactory.CreateClient(nameof(AuthService));
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CacheKey, out string token))
            {
                _logger.LogInformation("Using cached access token.");
                return token;
            }

            // Not in cache, fetch a new token
            var newToken = await GetTokenAsync();
            if (newToken == null)
            {
                _logger.LogError("Failed to retrieve new token.");
                throw new InvalidOperationException("Token acquisition failed.");
            }

            // Refresh 1 minute before expiry 
            var cacheOptions = new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(newToken.ExpiresIn - 60) };

            // Store in cache
            _cache.Set(CacheKey, newToken.AccessToken, cacheOptions);

            _logger.LogInformation($"New access token acquired and cached.");

            return newToken.AccessToken;
        }

        private async Task<TokenResponse> GetTokenAsync()
        {
            try
            {
                var settings = _authSettings.Value;
                var url = $"https://login.microsoftonline.com/{settings.TenantId}/oauth2/v2.0/token";
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "scope", "https://graph.microsoft.com/.default" },
                    { "client_id", settings.ClientId },
                    { "client_secret", settings.ClientSecret },
                    { "grant_type", "client_credentials" }
                });

                using var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Token retrieval failed: {StatusCode} - {Content}", response.StatusCode, errorBody);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<TokenResponse>(json);

                return token ?? throw new JsonException("Failed to deserialize token response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while retrieving token.");
                return null;
            }
        }
    }
}
