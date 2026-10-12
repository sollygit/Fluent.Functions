using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FileFunctionApp;

public static class FileShare
{
    [Function(nameof(FileShare))]
    public static async Task<HttpResponseData> Download([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fileshare/download")]
        HttpRequestData req, FunctionContext context)
    {
        var configuration = context.InstanceServices.GetRequiredService<IConfiguration>();
        var logger = context.GetLogger(nameof(Download));
        var connectionString = configuration["FileShare_ConnectionString"] ?? throw new InvalidOperationException("FileShare_ConnectionString is required.");

        var response = req.CreateResponse();

        try
        {
            var shareName = req.Query.Get("sharename");
            var directory = req.Query.Get("directory");
            var filename = req.Query.Get("filename");
            var encoded = req.Query.Get("encoded");

            if (string.IsNullOrWhiteSpace(shareName) || string.IsNullOrWhiteSpace(filename))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("The sharename and filename parameters are required.");
                return response;
            }

            var shareClient = new ShareClient(connectionString, shareName);
            var dir = string.IsNullOrWhiteSpace(directory) ? shareClient.GetRootDirectoryClient() : shareClient.GetDirectoryClient(directory);
            var file = dir.GetFileClient(filename);
            var fileDownloadInfo = await file.DownloadAsync();

            byte[] rawBytes;

            using (var memoryStream = new MemoryStream())
            {
                await fileDownloadInfo.Value.Content.CopyToAsync(memoryStream);
                rawBytes = memoryStream.ToArray();
            }

            // Return Base64-encoded content when requested.
            if (string.Equals(encoded, "true", StringComparison.OrdinalIgnoreCase))
            {
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync(Convert.ToBase64String(rawBytes));
                return response;
            }

            var contentType = Path.GetExtension(filename).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".json" => "application/json",
                ".txt" => "text/plain",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            response.Headers.Add("Content-Type", contentType);
            await response.Body.WriteAsync(rawBytes);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FileShare_Download failed.");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync("An error occurred while downloading the file.");
            return response;
        }
    }
}