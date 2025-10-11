using System.Threading.Tasks;

namespace TeamsBot.Services.Interfaces
{
    public interface IIntuneService
    {
        Task<string> DeployApp(string userId, string appId);
        Task<string> DeployScript(string userId, string scriptContent, string scriptName="");
    }
}
