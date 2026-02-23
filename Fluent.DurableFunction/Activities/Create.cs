using Fluent.Common;
using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Xml.Serialization;

namespace Fluent.DurableFunction.Activities
{
    public static class Create
    {
        [Function(nameof(Create))]
        public static async Task<DocumentResult> RunAsync([ActivityTrigger] DocumentRequest document, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Create));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;

            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Post, "v2/document")
            {
                Content = new StringContent(document.ToJson(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-WINDWARD-LICENSE", Environment.GetEnvironmentVariable("WINDWARD_LICENSE") ?? string.Empty);

            logger.LogInformation("Create:{AbsoluteUri}", new Uri(client.BaseAddress, "v2/document"));

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var serializer = new XmlSerializer(typeof(DocumentResult));
            using var reader = new StringReader(responseContent);
            var docResponse = (DocumentResult)serializer.Deserialize(reader);

            return docResponse;
        }
    }
}