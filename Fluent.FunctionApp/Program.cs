using Azure.Monitor.OpenTelemetry.Exporter;
using Fluent.FunctionApp.Services;
using Fluent.FunctionApp.Settings;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddHttpClient("FluentEngineClient", client => {
    client.Timeout = TimeSpan.FromSeconds(30);
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("FLUENT_ENGINE_URL"));
});
builder.Services.AddMemoryCache();
builder.Services.Configure<JsonOptions>(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOptions<OAuth2Settings>().Configure<IConfiguration>((settings, configuration) => {
    configuration.GetSection(nameof(OAuth2Settings)).Bind(settings);
});

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}
builder.Services.AddSingleton<IAuthService, AuthService>();

builder.Build().Run();
