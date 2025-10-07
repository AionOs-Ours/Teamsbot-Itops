using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace TeamsBot.Services.Interfaces
{
    public interface IUtilityService
    {
        string DetectIntent(string input);
        Task RunProcess(string request);
    }
}
