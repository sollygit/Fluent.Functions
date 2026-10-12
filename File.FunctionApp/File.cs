using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace File.FunctionApp;

public static class File
{
    [Function(nameof(File))]
    public static async Task<HttpResponseData> Download([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "file/download")] 
    HttpRequestData req, FunctionContext context)
    {
        var logger = context.GetLogger(nameof(Download));
        var response = req.CreateResponse();
        var _httpClientFactory = context.InstanceServices.GetRequiredService<IHttpClientFactory>();

        try
        {
            var request = await req.ReadFromJsonAsync<FileRequest>();

            if (string.IsNullOrWhiteSpace(request?.Path))
            {
                response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                await response.WriteStringAsync("URI_Path is required!");
                return response;
            }

            var client = _httpClientFactory.CreateClient();

            using var downloadResponse = await client.GetAsync(request.Path, HttpCompletionOption.ResponseHeadersRead);

            downloadResponse.EnsureSuccessStatusCode();

            var rawBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

            // Determine the file extension from the URL path.
            var uri = new Uri(request.Path);
            var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".json" => "application/json",
                ".txt" => "text/plain",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                "" or ".pdf" => "application/pdf",
                _ => downloadResponse.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
            };

            response.Headers.Add("Content-Type", contentType);
            await response.WriteBytesAsync(rawBytes);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File download failed");

            response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
            response.Headers.Add("Content-Type", "text/plain");

            await response.WriteStringAsync("Failed to download the file.");
            return response;
        }
    }
}