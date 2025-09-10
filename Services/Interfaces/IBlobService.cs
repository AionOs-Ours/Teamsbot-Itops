using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace TeamsBot.Services.Interfaces
{
    public interface IBlobService
    {
        Task<string> GetFileContent(string blobName="");
    }
}
