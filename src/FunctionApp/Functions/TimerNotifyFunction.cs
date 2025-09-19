using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FunctionApp.Services;

namespace FunctionApp.Functions;

public class TimerNotifyFunction
{
    private readonly ILogger _logger;
    private readonly AdapterWithErrorHandler _adapter;
    private readonly IConversationStore _store;
    private readonly string _botAppId;

    public TimerNotifyFunction(ILoggerFactory loggerFactory, AdapterWithErrorHandler adapter, IConversationStore store, IConfiguration cfg)
    {
        _logger = loggerFactory.CreateLogger<TimerNotifyFunction>();
        _adapter = adapter;
        _store = store;
        _botAppId = cfg["Bot:MicrosoftAppId"]!;
    }

    [Function("TimerNotify")]
    public async Task RunAsync([TimerTrigger("%Timer:Cron%", RunOnStartup = false)] TimerInfo timer)
    {
        _logger.LogInformation("Timer fired at: {time}", DateTimeOffset.Now);

        var references = await _store.GetAllAsync();
        foreach (var reference in references)
        {
            await _adapter.ContinueConversationAsync(
                _botAppId,
                reference,
                async (context, ct) =>
                {
                    await context.SendActivityAsync($"⏰ 定刻通知: {DateTimeOffset.Now:t} にチェックしました。", ct: ct);
                },
                CancellationToken.None);
        }
    }
}
