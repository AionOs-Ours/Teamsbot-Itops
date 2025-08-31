using System;
using MongoDB.Bson;


namespace TeamsBot.Mongo
{
    public class ServiceRequest
    {
        public ServiceRequest(string teamsUserId, string approverId, string description,string sdpNumber="") 
        {
            TeamsUserId=teamsUserId;
            ApproverId=approverId;
            Descritption=description;
            Status = "New";
            SDPNumber=sdpNumber;
        }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TeamsUserId { get; set; }
        public string SDPNumber { get; set; }
        public string Status { get; set; }
        public string TicketNumber { get; set; }= DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        public string Descritption { get; set; }
        public string ApproverId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    }
}
