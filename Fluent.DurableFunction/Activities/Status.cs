using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Fluent.DurableFunction.Activities
{
    public static class Status
    {
        [Function(nameof(Status))]
        public static async Task<HttpStatusCode> RunAsync([ActivityTrigger] Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Status));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/status");

            logger.LogInformation("Status:{AbsoluteUri}", new Uri(client.BaseAddress, $"/v2/document/{guid}/status"));

            var response = await client.SendAsync(request);
            return response.StatusCode;
        }

    }
}
