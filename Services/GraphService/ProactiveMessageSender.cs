namespace TeamsBot.Services.GraphService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Models;

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
                var chat = new Chat
                {
                    ChatType = ChatType.OneOnOne,
                    Members = new List<ConversationMember>
                    {
                        new AadUserConversationMember
                        {
                            Roles = new List<string>{"owner"},
                            AdditionalData = new Dictionary<string, object>()
                            {
                                {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users/{user.Id}"}
                            }
                        },
                        new AadUserConversationMember
                        {
                            Roles = new List<string>{"owner"},
                            AdditionalData = new Dictionary<string, object>()
                            {
                                {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users/{botAppId}"}
                            }
                        }
                    }
                };

                var createdChat = await _graphClient.Chats.PostAsync(chat);

                Console.WriteLine($"Chat created: {chat.Id}");

                // 3. Send a message into that chat
                var message = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        Content = "👋 Hello! This is a proactive message from the bot."
                    }
                };

                await _graphClient.Chats[chat.Id].Messages.PostAsync(message);

                Console.WriteLine("Message sent!");
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }

}
