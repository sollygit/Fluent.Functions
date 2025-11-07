using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fluent.Functions
{
    public class FileWebJob
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string FileShare_ConnectionString;

        public FileWebJob(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            FileShare_ConnectionString = _configuration["FileShare_ConnectionString"];
        }

        [Function(nameof(FileDownload))]
        public async Task<HttpResponseData> FileDownload([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "File/Download")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(FileDownload));
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

                if (request.Encoded)
                {
                    var encoded = Convert.ToBase64String(rawBytes);
                    await response.WriteStringAsync(encoded);
                }
                else
                {
                    await response.WriteBytesAsync(rawBytes);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError("DownloadFromURI:{Message}", ex.Message);
                await response.WriteStringAsync(ex.Message);
                return response;
            }
        }

        [Function(nameof(FileShareDownload))]
        public async Task<HttpResponseData> FileShareDownload([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"FileShare/Download")] HttpRequestData req, FunctionContext executionContext)
        {
            var response = req.CreateResponse();
            var logger = executionContext.GetLogger(nameof(FileShareDownload));

            try
            {

                logger.LogInformation("DownloadFromFileShare:{FileShare_ConnectionString}", FileShare_ConnectionString);

                var shareName = req.Query.Get("sharename");
                var directory = req.Query.Get("directory");
                var filename = req.Query.Get("filename");
                var shareClient = new ShareClient(FileShare_ConnectionString, shareName);
                var dir = shareClient.GetDirectoryClient(directory);
                var file = dir.GetFileClient(filename);
                var fileDownloadInfo = await file.DownloadAsync();
                var bytes = new byte[fileDownloadInfo.Value.ContentLength];
                using (var stream = file.OpenRead(new ShareFileOpenReadOptions(false)))
                {
                    stream.Read(bytes, 0, bytes.Length);
                }
                await response.WriteBytesAsync(bytes);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError("DownloadFromFileShare:{Message}", ex.Message);
                await response.WriteStringAsync(ex.Message);
                return response;
            }
        }
    }
}
