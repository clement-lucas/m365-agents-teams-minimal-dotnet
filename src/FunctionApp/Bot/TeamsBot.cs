using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using FunctionApp.Services;

namespace FunctionApp.Bot;

public class TeamsBot : ActivityHandler
{
    private readonly DialogBot _dialogBot;
    private readonly IConversationStore _store;

    public TeamsBot(DialogBot dialogBot, IConversationStore store)
    {
        _dialogBot = dialogBot;
        _store = store;
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        await SaveConversationRefAsync(turnContext, cancellationToken);
        await _dialogBot.RunAsync(turnContext, cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        await SaveConversationRefAsync(turnContext, cancellationToken);
        await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);
    }

    private async Task SaveConversationRefAsync(ITurnContext turnContext, CancellationToken ct)
    {
        var reference = turnContext.Activity.GetConversationReference();
        await _store.UpsertAsync(reference);
    }
}
