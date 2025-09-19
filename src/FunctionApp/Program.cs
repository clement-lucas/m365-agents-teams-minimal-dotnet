using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using FunctionApp.Bot;
using FunctionApp.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // Bot authentication
        services.AddSingleton<BotFrameworkAuthentication>(sp =>
            new ConfigurationBotFrameworkAuthentication(cfg));

        services.AddSingleton<AdapterWithErrorHandler>();

        // Bot & Dialog
        services.AddSingleton<IBot, TeamsBot>();
        services.AddSingleton<DialogBot>();
        services.AddSingleton<MainDialog>();

        // Storage/State
        services.AddSingleton<IStorage, MemoryStorage>();
        services.AddSingleton<UserState>();
        services.AddSingleton<ConversationState>();

        // Cosmos store for proactive conversation references
        services.AddSingleton<IConversationStore>(sp =>
            new ConversationStoreCosmos(
                cfg["Cosmos:Endpoint"]!,
                cfg["Cosmos:Key"]!,
                cfg["Cosmos:Database"]!,
                cfg["Cosmos:Container"]!,
                int.TryParse(cfg["Cosmos:ContainerThroughput"], out var ru) ? ru : 400));

        // Graph factory
        services.AddSingleton<GraphClientFactory>();
    })
    .Build();

await host.RunAsync();

// Simple global exception middleware (Functions isolated)
public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try { await next(context); }
        catch (Exception ex)
        {
            var logger = context.GetLogger("Functions");
            logger.LogError(ex, "Unhandled exception");
            throw;
        }
    }
}
