using Fluent.Common;
using Fluent.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Xml.Serialization;

namespace Fluent.Durable
{
    public class DocumentSequence
    {
        private readonly IConfiguration _configuration;
        private readonly string WINDWARD_LICENSE;

        public DocumentSequence(IConfiguration configuration)
        {
            _configuration = configuration;
            WINDWARD_LICENSE = _configuration["X_WINDWARD_LICENSE"];
        }

        [Function(nameof(DocumentOrchestration))]
        public async Task<HttpResponseData> DocumentOrchestration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Orchestration/{functionName}")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            string functionName,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(DocumentOrchestration));
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var document = content.FromJson<DocumentRequest>();
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(functionName, document);

            logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }

        [Function(nameof(DocumentProcess))]
        public async Task<DocumentResult> DocumentProcess([OrchestrationTrigger] TaskOrchestrationContext context, DocumentRequest document)
        {
            var result = await context.CallActivityAsync<DocumentResult>(nameof(Create), document);
            var statusCode = await context.CallActivityAsync<HttpStatusCode>(nameof(Status), result.Guid);
            var meta = await context.CallActivityAsync<MetaResult>(nameof(Meta), result.Guid);

            return new DocumentResult { Guid = result.Guid, NumberOfPages = meta.NumberOfPages, StatusCode = statusCode, Uri = meta.Uri };
        }

        [Function(nameof(Create))]
        public async Task<DocumentResult> Create([ActivityTrigger] DocumentRequest document, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Create));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;

            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Post, "v2/document")
            {
                Content = new StringContent(document.ToJson(), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);

            logger.LogInformation("Create:{AbsoluteUri}", new Uri(client.BaseAddress, "v2/document"));

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var serializer = new XmlSerializer(typeof(DocumentResult));
            using var reader = new StringReader(responseContent);
            var docResponse = (DocumentResult)serializer.Deserialize(reader);

            return docResponse;
        }

        [Function(nameof(Status))]
        public async Task<HttpStatusCode> Status([ActivityTrigger] Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Status));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/status");

            logger.LogInformation("Status:{AbsoluteUri}", new Uri(client.BaseAddress, $"/v2/document/{guid}/status"));

            var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        [Function(nameof(Meta))]
        public async Task<MetaResult> Meta([ActivityTrigger] Guid guid, FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(Meta));
            var httpClientFactory = executionContext.InstanceServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            var client = httpClientFactory.CreateClient("FluentEngineClient");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/document/{guid}/meta");

            request.Headers.Add("X-WINDWARD-LICENSE", WINDWARD_LICENSE);
            request.Headers.Add("Accept", "application/json");

            logger.LogInformation("Meta:{AbsoluteUri}", new Uri(client.BaseAddress, $"/v2/document/{guid}/meta"));

            await Task.Delay(5000); // Add a short delay to ensure the meta is available
            var result = await client.SendAsync(request);
            var metaResponse = await result.Content.ReadFromJsonAsync<MetaResult>();
            return metaResponse;
        }
    }
}
