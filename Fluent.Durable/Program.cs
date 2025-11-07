using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.AddUserSecrets<Program>();
        }
        builder.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddHttpClient("FluentEngineClient", client => {
            client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("FLUENT_ENGINE_URL"));
        });
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
