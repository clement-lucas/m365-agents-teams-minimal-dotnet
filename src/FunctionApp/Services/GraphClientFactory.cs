using Microsoft.Graph;

namespace FunctionApp.Services;

public class GraphClientFactory
{
    public GraphServiceClient CreateWithUserToken(string accessToken)
    {
        return new GraphServiceClient(new DelegateAuthenticationProvider(req =>
        {
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return Task.CompletedTask;
        }));
    }
}
