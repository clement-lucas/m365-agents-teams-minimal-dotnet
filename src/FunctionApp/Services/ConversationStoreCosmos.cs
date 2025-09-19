using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Schema;
using FunctionApp.Models;

namespace FunctionApp.Services;

public interface IConversationStore
{
    Task UpsertAsync(ConversationReference reference);
    Task<IReadOnlyList<ConversationReference>> GetAllAsync();
}

public class ConversationStoreCosmos : IConversationStore
{
    private readonly CosmosClient _client;
    private readonly Container _container;

    public ConversationStoreCosmos(string endpoint, string key, string db, string container, int throughput)
    {
        _client = new CosmosClient(endpoint, key, new CosmosClientOptions { ApplicationName = "TeamsBot" });
        _client.CreateDatabaseIfNotExistsAsync(db, throughput).GetAwaiter().GetResult();
        _client.GetDatabase(db).CreateContainerIfNotExistsAsync(container, "/partitionKey").GetAwaiter().GetResult();
        _container = _client.GetContainer(db, container);
    }

    public async Task UpsertAsync(ConversationReference reference)
    {
        var doc = ConversationReferenceDocument.From(reference);
        await _container.UpsertItemAsync(doc, new PartitionKey(doc.PartitionKey));
    }

    public async Task<IReadOnlyList<ConversationReference>> GetAllAsync()
    {
        var query = _container.GetItemQueryIterator<ConversationReferenceDocument>(new QueryDefinition("SELECT * FROM c"));
        var list = new List<ConversationReference>();
        while (query.HasMoreResults)
        {
            var page = await query.ReadNextAsync();
            list.AddRange(page.Select(p => p.ToConversationReference()));
        }
        return list;
    }
}
