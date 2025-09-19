using Microsoft.Bot.Schema;
using System.Text.Json.Serialization;

namespace FunctionApp.Models;

public class ConversationReferenceDocument
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("partitionKey")] public string PartitionKey { get; set; } = "default";

    public string? ActivityId { get; set; }
    public ChannelAccount? User { get; set; }
    public ChannelAccount? Bot { get; set; }
    public ConversationAccount? Conversation { get; set; }
    public string? ChannelId { get; set; }
    public string? ServiceUrl { get; set; }

    public static ConversationReferenceDocument From(ConversationReference r) => new()
    {
        ActivityId = r.ActivityId,
        User = r.User,
        Bot = r.Bot,
        Conversation = r.Conversation,
        ChannelId = r.ChannelId,
        ServiceUrl = r.ServiceUrl,
        PartitionKey = r.Conversation?.TenantId ?? "default",
        Id = r.Conversation?.Id?.Replace(":", "_") ?? Guid.NewGuid().ToString("N")
    };

    public ConversationReference ToConversationReference() => new()
    {
        ActivityId = ActivityId,
        User = User,
        Bot = Bot,
        Conversation = Conversation,
        ChannelId = ChannelId,
        ServiceUrl = ServiceUrl
    };
}
