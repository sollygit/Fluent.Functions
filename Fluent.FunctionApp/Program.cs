using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fluent.FunctionApp.Services;
using Fluent.FunctionApp.Settings;
using System.Text.Json.Serialization;

namespace Fluent.FunctionApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
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
                .ConfigureServices(services => {
                    services.AddHttpClient("FluentEngineClient", client => {
                        client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("FLUENT_ENGINE_URL"));
                    });
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                    services.AddMemoryCache();
                    services.Configure<JsonOptions>(options =>
                     {
                         options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                     });
                    services.AddOptions<OAuth2Settings>().Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection(nameof(OAuth2Settings)).Bind(settings);
                    });
                    services.AddSingleton<IAuthService, AuthService>();
                });
        }
    }
}
