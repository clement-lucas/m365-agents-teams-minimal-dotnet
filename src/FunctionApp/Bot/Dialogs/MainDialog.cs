using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using FunctionApp.Services;

namespace FunctionApp.Bot;

public class MainDialog : ComponentDialog
{
    private const string OAuthPromptId = "OAuthPrompt";
    private readonly IConfiguration _cfg;
    private readonly GraphClientFactory _graphFactory;

    public MainDialog(IConfiguration cfg, GraphClientFactory graphFactory)
        : base(nameof(MainDialog))
    {
        _cfg = cfg;
        _graphFactory = graphFactory;

        var connectionName = _cfg["OAuth:ConnectionName"] ?? "TeamsSSO";

        AddDialog(new OAuthPrompt(
            OAuthPromptId,
            new OAuthPromptSettings
            {
                ConnectionName = connectionName,
                Text = "サインインが必要です。",
                Title = "サインイン",
                Timeout = 300000
            }));

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
        {
            RecognizeCommandAsync,
            EnsureLoginAsync,
            CallGraphAsync
        }));

        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> RecognizeCommandAsync(WaterfallStepContext stepContext, CancellationToken ct)
    {
        var text = (stepContext.Context.Activity.Text ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(text))
        {
            await stepContext.Context.SendActivityAsync("`me` と送るとプロフィールを取得します。", cancellationToken: ct);
            return await stepContext.EndDialogAsync(cancellationToken: ct);
        }

        stepContext.Values["cmd"] = text;
        return await stepContext.NextAsync(cancellationToken: ct);
    }

    private async Task<DialogTurnResult> EnsureLoginAsync(WaterfallStepContext stepContext, CancellationToken ct)
    {
        var cmd = (string)stepContext.Values["cmd"];
        if (cmd != "me")
        {
            await stepContext.Context.SendActivityAsync("`me` 以外のコマンドは未対応です。", cancellationToken: ct);
            return await stepContext.EndDialogAsync(cancellationToken: ct);
        }

        return await stepContext.BeginDialogAsync(OAuthPromptId, cancellationToken: ct);
    }

    private async Task<DialogTurnResult> CallGraphAsync(WaterfallStepContext stepContext, CancellationToken ct)
    {
        var tokenResponse = (TokenResponse?)stepContext.Result;
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
        {
            await stepContext.Context.SendActivityAsync("サインインが完了しませんでした。", cancellationToken: ct);
            return await stepContext.EndDialogAsync(cancellationToken: ct);
        }

        var graph = _graphFactory.CreateWithUserToken(tokenResponse.Token);
        var me = await graph.Me.GetAsync(cancellationToken: ct);
        await stepContext.Context.SendActivityAsync($"こんにちは {me?.DisplayName} さん！（UPN: {me?.UserPrincipalName}）", cancellationToken: ct);
        return await stepContext.EndDialogAsync(cancellationToken: ct);
    }
}
