using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TeamsBot.Services.ManageEngine
{
    public class ManageEngineAuthManager
    {
        private readonly string _clientId = "1000.POQH62CWQFBABIP8HW8RVBCFHP0N8N";
        private readonly string _clientSecret = "cbe5ef2eb313ffeee9b47433ca35b6b40e29709868";
        private readonly string _scope = "DesktopCentralCloud.Common.READ,DesktopCentralCloud.Common.Update,DesktopCentral.SoftwareDeployment.Read,DesktopCentral.SoftwareDeployment.Write,DesktopCentral.Computer.Read,SDPOnDemand.requests.READ,SDPOnDemand.requests.CREATE,SDPOnDemand.requests.UPDATE,SDPOnDemand.solutions.READ";
       private readonly string _redirectUri = "https://zylkerapps.com/oauth2callback";
        private readonly string _authUrl = "https://accounts.zoho.in/oauth/v2/auth";
        private readonly string _tokenUrl = "https://accounts.zoho.in/oauth/v2/token";
        private readonly string code = string.Empty;
        public ManageEngineAuthManager()
        {
            string redirectUri = "https://zylkerapps.com/oauth2callback";
            string state = Guid.NewGuid().ToString(); // generate a random state to validate later

            string authUrl = $"https://accounts.zoho.in/oauth/v2/auth" +
                             $"?response_type=code" +
                             $"&client_id={_clientId}" +
                             $"&scope={Uri.EscapeDataString(_scope)}" +
                             $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                             $"&access_type=offline" +
                             $"&prompt=consent" +
                             $"&state={state}";

            code = "update code from auth url";
            Console.WriteLine("Open this URL in a browser:");
            Console.WriteLine(authUrl);

        }

        public async Task<string> GetAccessTokenAsync()
    {
        var client = new HttpClient();
        var values = new Dictionary<string, string>
    {
        { "code", code },
        { "grant_type", "authorization_code" },
        { "client_id", "1000.POQH62CWQFBABIP8HW8RVBCFHP0N8N" },
        { "client_secret", "cbe5ef2eb313ffeee9b47433ca35b6b40e29709868" },
        { "redirect_uri", "https://zylkerapps.com/oauth2callback" },
        { "scope", "DesktopCentralCloud.Common.READ,DesktopCentralCloud.Common.Update,DesktopCentral.SoftwareDeployment.Read, DesktopCentral.SoftwareDeployment.Write, DesktopCentral.Computer.Read, SDPOnDemand.requests.READ, " +
        "SDPOnDemand.requests.CREATE, SDPOnDemand.requests.UPDATE, SDPOnDemand.solutions.READ" }
    };

        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync("https://accounts.zoho.in/oauth/v2/token", content);

        return await response.Content.ReadAsStringAsync();
    }

}
}
