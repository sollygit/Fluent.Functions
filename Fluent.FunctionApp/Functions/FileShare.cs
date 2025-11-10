using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fluent.FunctionApp.Functions
{
    public class FileShare
    {
        private readonly IConfiguration _configuration;
        private readonly string FileShare_ConnectionString;

        public FileShare(IConfiguration configuration)
        {
            _configuration = configuration;
            FileShare_ConnectionString = _configuration["FileShare_ConnectionString"];
        }

        [Function("FileShare_Download")]
        public async Task<HttpResponseData> Download([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = $"FileShare/Download")] HttpRequestData req, FunctionContext executionContext)
        {
            var response = req.CreateResponse();
            var logger = executionContext.GetLogger(nameof(Download));

            try
            {

                logger.LogInformation("FileShare download:{FileShare_ConnectionString}", FileShare_ConnectionString);

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
                logger.LogError("FileShare_Download:{Message}", ex.Message);
                await response.WriteStringAsync(ex.Message);
                return response;
            }
        }
    }
}
