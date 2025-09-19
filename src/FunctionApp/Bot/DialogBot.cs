using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace FunctionApp.Bot;

public class DialogBot
{
    private readonly ConversationState _conversationState;
    private readonly UserState _userState;
    private readonly Dialog _dialog;

    public DialogBot(ConversationState conversationState, UserState userState, MainDialog dialog)
    {
        _conversationState = conversationState;
        _userState = userState;
        _dialog = dialog;
    }

    public async Task RunAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        var dialogState = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
        await _dialog.RunAsync(turnContext, await dialogState.GetAsync(turnContext, () => new DialogState()), cancellationToken);
        await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
    }
}
