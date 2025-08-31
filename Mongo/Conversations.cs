using System;
using MongoDB.Bson;


namespace TeamsBot.Mongo
{
    public class Conversations
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Conversation { get; set; }

        public string TeamsUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    }
}
