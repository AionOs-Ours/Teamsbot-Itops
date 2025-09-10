using Microsoft.Graph.Beta;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Azure.Identity;

namespace TeamsBot.Services.GraphService
{
    public class GraphHelper
    {
        private static GraphServiceClient _graphClient;

        public static GraphServiceClient GetGraphClient(string clientId, string tenantId, string clientSecret)
        {
            // Azure.Identity provides modern credentials
            var credential = new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret
            );

            // GraphServiceClient directly accepts a TokenCredential now
            var graphClient = new GraphServiceClient(credential,
                new[] { "https://graph.microsoft.com/.default" });

            return graphClient;
        }
    }
}
