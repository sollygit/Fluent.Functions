using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Fluent.DurableFunction.Activities
{
    public static class Meta
    {
        [Function(nameof(Meta))]
        public static async Task<MetaResult> RunAsync([ActivityTrigger] Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Meta));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/meta");
            
            request.Headers.Add("X-WINDWARD-LICENSE", Environment.GetEnvironmentVariable("WINDWARD_LICENSE") ?? string.Empty);
            request.Headers.Add("Accept", "application/json");

            logger.LogInformation("Meta:{AbsoluteUri}", new Uri(client.BaseAddress, $"/v2/document/{guid}/meta"));

            await Task.Delay(5000); // Add a short delay to ensure the meta is available
            var result = await client.SendAsync(request);
            var metaResponse = await result.Content.ReadFromJsonAsync<MetaResult>();
            return metaResponse;
        }
    }
}
