using AdaptiveCards;
using TeamsBot.Mongo;

namespace TeamsBot.Models
{
    public class OperationResponse
    {
        public OperationResponse(string reply,AdaptiveCard card, bool toOtherUser, Conversations conversation)
        {
            Reply = reply;
            Card = card;
            ToOtherUser = toOtherUser;
            EndUserConversation = conversation;
        }
        public string Reply { get; set; }
        public AdaptiveCard Card { get; set; }
        public bool ToOtherUser { get; set; }
        public Conversations EndUserConversation { get; set; }
    }
}
