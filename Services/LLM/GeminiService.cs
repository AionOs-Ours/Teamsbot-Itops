using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
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

        public async Task<GeminiResponse> GetGeminiResponseAsync(string userPrompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                new { parts = new[] { new { text = userPrompt } } }
            }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}?key={_apiKey}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var geminiResponse= JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            return geminiResponse;
        }
    }

}
