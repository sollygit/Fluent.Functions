using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Fluent.FunctionApp.Functions
{
    public class File
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public File(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Function("File_Download")]
        public async Task<HttpResponseData> Download([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "File/Download/{encoded?}")] HttpRequestData req, FunctionContext executionContext, bool encoded = false)
        {
            var logger = executionContext.GetLogger(nameof(Download));
            var response = req.CreateResponse();

            try
            {
                var request = await req.ReadFromJsonAsync<FileRequest>();
                if (string.IsNullOrEmpty(request.Path))
                {
                    await response.WriteStringAsync("URI_Path is required!");
                    return response;
                }

                logger.LogInformation("DownloadFromURI:{URI_Path}", request.Path);

                var client = _httpClientFactory.CreateClient();
                var rawBytes = await client.GetByteArrayAsync(request.Path);

                await (encoded ? 
                    response.WriteStringAsync(Convert.ToBase64String(rawBytes)) : 
                    response.WriteBytesAsync(rawBytes));

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError("File_Download:{Message}", ex.Message);
                await response.WriteStringAsync(ex.Message);
                return response;
            }
        }
    }
}
