using System.Threading.Tasks;
using AdaptiveCards;
using TeamsBot.Mongo;

namespace TeamsBot.Services.Interfaces
{
    public interface ICardService
    {
        Task<AdaptiveCard> GetCard(string responseMsg, string senderName, string serviceRequest, string objectId);
        Task<AdaptiveCard> BuildSoftwareSuiteCard(SoftwareSuite suite);
        Task<AdaptiveCard> BuildSoftwareApprovalCard(ServiceRequest serviceRequest, string userText, string sender, string suiteId);
    }
}
