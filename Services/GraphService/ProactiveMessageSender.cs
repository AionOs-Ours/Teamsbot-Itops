namespace TeamsBot.Services.GraphService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph.Beta;
    using Microsoft.Graph.Beta.Models;

    public class ProactiveMessageSender
    {
        private readonly GraphServiceClient _graphClient;

        public ProactiveMessageSender(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        // Replace usage of ChatMembersCollectionPage with List<ConversationMember> for the Members property

        public async Task<User> SendMessageToUserAsync(string userEmail, string botAppId)
        {
            try
            {


                // 1. Lookup user by email

                var user = await _graphClient.Users[userEmail].GetAsync();
                return user;
               
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }

}
