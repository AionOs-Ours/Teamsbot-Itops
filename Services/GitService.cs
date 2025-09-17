using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TeamsBot.Services
{
    public class GitService
    {
        public GitService() { }
        public async Task<JObject> GetGitJsonFile()
        {
            try
            {
                var rawUrl = "https://raw.githubusercontent.com/AionOs-Ours/SoftwareRepo/main/SoftwareJson.json";

                using var client = new HttpClient();
                var content = await client.GetAsync(rawUrl);
                var stringContent = await content.Content.ReadAsStringAsync();
                return JObject.Parse(stringContent);
                
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
