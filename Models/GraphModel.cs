using Microsoft.Graph.Beta;

namespace TeamsBot.Models
{
    public class GraphModel
    {
        public GraphServiceClient graphServiceClient { get; set; }
        public string utilityId { get; set; }
    }
}
