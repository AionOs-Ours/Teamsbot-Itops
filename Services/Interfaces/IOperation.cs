using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using TeamsBot.Models;

namespace TeamsBot.Services.Interfaces
{
    public interface IOperation
    {
        Task<string> WelcomeMessage();
        Task<OperationResponse> ExecuteOperation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken);
    }
}
