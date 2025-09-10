using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Graph.Beta.Models;
using MongoDB.Driver;
using TeamsBot.Models;
//using Newtonsoft.Json;

namespace TeamsBot.Services.LLM
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "AIzaSyDZBlctxepAYwTlGqBfpjbhZDjnd0WMkgs";
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        public GeminiService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }
        public async Task<bool> GetIsListGeminiResponseAsync(string userPrompt)
        {
            userPrompt = $"Determine if the user is asking for a list of software names. Respond only YES or NO.\n\nUser: {userPrompt}";
            var request = GetGeminiRequest(userPrompt);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            return geminiResponse.Candidates[0].Content.Parts[0].Text.ToString().ToLower().Contains("yes");
        }
        public async Task<GeminiResponse> GetGeminiResponseAsync(string userPrompt,bool isList=true)
        {
            var request = GetGeminiRequest(userPrompt);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var geminiResponse= JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            return geminiResponse;
        }

        public HttpRequestMessage GetGeminiRequest(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
               {
                new { parts = new[] { new { text = prompt } } }
            }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}?key={_apiKey}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            return request;
        }
    }

}
