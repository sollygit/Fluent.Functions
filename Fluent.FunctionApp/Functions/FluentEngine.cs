using Fluent.Common;
using Fluent.FunctionApp.Services;
using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Fluent.FunctionApp.Functions
{
    public class FluentEngine
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly string FLUENT_ENGINE_URL;
        private readonly string WINDWARD_LICENSE;

        public FluentEngine(IHttpClientFactory httpClientFactory, IAuthService authService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _configuration = configuration;
            FLUENT_ENGINE_URL = _configuration["FLUENT_ENGINE_URL"];
            WINDWARD_LICENSE = _configuration["X_WINDWARD_LICENSE"];
        }

        [Function(nameof(Version))]
        public async Task<HttpResponseData> Version([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "FluentEngine/Version")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Version));
            var response = req.CreateResponse(HttpStatusCode.OK);

            logger.LogInformation($"GET: {FLUENT_ENGINE_URL}");

            var accessToken = await _authService.GetAccessTokenAsync();
            var client = _httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, "/v2/version");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken); // Not really required - for demo purposes
            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);

            var apiResponse = await client.SendAsync(request);

            if (!apiResponse.IsSuccessStatusCode)
            {
                logger.LogError($"Version API call failed with status code {apiResponse.StatusCode}");
                response.StatusCode = HttpStatusCode.BadGateway;
                await response.WriteStringAsync("Failed to retrieve version info");
                return response;
            }

            var content = await apiResponse.Content.ReadAsStringAsync();

            response.Headers.Add("Content-Type", "application/xml");
            await response.WriteStringAsync(content);
            return response;
        }

        [Function(nameof(Create))]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = $"FluentEngine/{nameof(Create)}")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var response = req.CreateResponse();
            var logger = executionContext.GetLogger(nameof(Create));

            try
            {
                var documentRequest = await req.ReadFromJsonAsync<DocumentRequest>();
                var body = documentRequest.ToJson();
                var client = _httpClientFactory.CreateClient("FluentEngineClient");
                var request = new HttpRequestMessage(HttpMethod.Post, "v2/document");

                request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var result = await client.SendAsync(request);

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    await response.WriteAsJsonAsync(result.Content.ReadAsStringAsync());
                    return response;
                }

                var content = await result.Content.ReadAsStringAsync();
                response.Headers.Add("Content-Type", "application/xml");

                await response.WriteStringAsync(content);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError("Something went wrong:{Message}", ex.Message);
                await response.WriteAsJsonAsync(ex.Message);
                return response;
            }
        }

        [Function(nameof(Status))]
        public async Task<HttpResponseData> Status([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"FluentEngine/{{guid}}/{nameof(Status)}")] HttpRequestData req, Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Status));
            var response = req.CreateResponse();
            var client = _httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/status");

            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);

            logger.LogInformation($"Request URI: {client.BaseAddress}{request.RequestUri}");

            var result = await client.SendAsync(request);

            if (result.StatusCode != HttpStatusCode.Found)
            {
                await response.WriteAsJsonAsync(result.Content.ReadAsStringAsync());
                return response;
            }

            await response.WriteStringAsync(result.StatusCode.ToString());
            return response;
        }

        [Function(nameof(Meta))]
        public async Task<HttpResponseData> Meta([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"FluentEngine/{{guid}}/{nameof(Meta)}")] HttpRequestData req, Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Meta));
            var response = req.CreateResponse();
            var client = _httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/meta");

            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);
            request.Headers.Add("Accept", "application/json");

            logger.LogInformation($"Request URI: {client.BaseAddress}{request.RequestUri}");

            var result = await client.SendAsync(request);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                await response.WriteAsJsonAsync(result.Content.ReadAsStringAsync());
                return response;
            }

            var metaResponse = await result.Content.ReadFromJsonAsync<MetaResponse>();
            await response.WriteAsJsonAsync(metaResponse);
            return response;
        }

        [Function(nameof(File))]
        public async Task<HttpResponseData> File([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"FluentEngine/{{guid:Guid}}/{nameof(File)}")] HttpRequestData req, Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(File));
            var response = req.CreateResponse();
            var client = _httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/file");
            
            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);

            logger.LogInformation($"Request URI: {client.BaseAddress}{request.RequestUri}");

            var result = await client.SendAsync(request);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                await response.WriteAsJsonAsync(result.Content.ReadAsStringAsync());
                return response;
            }

            var bytes = await result.Content.ReadAsByteArrayAsync();
            response.Headers.Add("Content-Type", "application/pdf");
            await response.WriteBytesAsync(bytes);
            return response;
        }
    }
}
