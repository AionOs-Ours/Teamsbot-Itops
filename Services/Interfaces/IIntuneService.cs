using System.Threading.Tasks;

namespace TeamsBot.Services.Interfaces
{
    public interface IIntuneService
    {
        Task<string> PushSoftware(string userId);
    }
}
