using Microsoft.Identity.Client;

namespace TeamsBot.Models
{
    public class Config
    {
        public AppConfig AppConfig { get; set; }
        public ServiceNowConfig ServiceNowConfig { get; set; }
    }
    public class AppConfig
    {
        public string BotId { get; set; }
        public string AppPassword { get; set; }
        public string TenantId { get; set; }
        public string ItAdminEmail { get; set; }
    }
    public class ServiceNowConfig
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
