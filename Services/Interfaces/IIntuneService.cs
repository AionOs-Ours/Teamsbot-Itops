using System.Threading.Tasks;

namespace TeamsBot.Services.Interfaces
{
    public interface IIntuneService
    {
        Task<string> DeployApp(string userId);
        Task<string> DeployScript(string userId, string scriptContent);
    }
}
