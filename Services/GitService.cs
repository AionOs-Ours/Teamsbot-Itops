using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;
using TeamsBot.Mongo;
using TeamsBot.Services.Interfaces;

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
        public async Task<SoftwareSuite> GetSoftwareSuite(JArray softwareSuites,string softwareName)
        {
            SoftwareSuite softwareSuite = new SoftwareSuite();
            foreach (var item in softwareSuites.Children<JObject>().ToArray())
            {
                var name = item["SuiteName"]?.ToString().ToLower();
                if (name != null && name.Contains(softwareName.ToLower()))
                {
                    softwareSuite = item.ToObject<SoftwareSuite>();
                    continue;
                }
                foreach (var software in item["Softwares"].Children<JObject>().ToArray())
                {
                    var Swname = software["Name"]?.ToString().ToLower();
                    if (Swname != null && Swname.Contains(softwareName))
                    {
                        softwareSuite = item.ToObject<SoftwareSuite>();
                        continue;
                    }
                }
            }
            return softwareSuite;
        }
    }
}
