using System.Threading.Tasks;
using AdaptiveCards;

namespace TeamsBot.Services.Interfaces
{
    public interface ICardService
    {
        Task<AdaptiveCard> GetCard(string responseMsg, string senderName, string serviceRequest, string objectId);
    }
}
