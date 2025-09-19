using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Bot.Builder;
using FunctionApp.Bot;

namespace FunctionApp.Functions;

public class BotControllerFunction
{
    private readonly AdapterWithErrorHandler _adapter;
    private readonly IBot _bot;

    public BotControllerFunction(AdapterWithErrorHandler adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [Function("messages")]
    public async Task<HttpResponseData> MessagesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "api/messages")] HttpRequestData req,
        FunctionContext ctx)
    {
        var resp = req.CreateResponse();
        await _adapter.ProcessAsync(req, resp, _bot);
        return resp;
    }
}
