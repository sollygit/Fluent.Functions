using Fluent.Common;
using Fluent.DurableFunction.Activities;
using Fluent.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Fluent.DurableFunction
{
    public static class DocumentOrchestration
    {
        [Function(nameof(DocumentOrchestration))]
        public static async Task<DocumentResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context, DocumentRequest document)
        {
            var result = await context.CallActivityAsync<DocumentResult>(nameof(Create), document);
            var statusCode = await context.CallActivityAsync<HttpStatusCode>(nameof(Status), result.Guid);
            var meta = await context.CallActivityAsync<MetaResult>(nameof(Meta), result.Guid);

            return new DocumentResult { Guid = result.Guid, NumberOfPages = meta.NumberOfPages, StatusCode = statusCode, Uri = meta.Uri };
        }

        [Function("DocumentOrchestration_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger(nameof(DocumentOrchestration));
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var document = content.FromJson<DocumentRequest>();
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(DocumentOrchestration), document);

            logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
