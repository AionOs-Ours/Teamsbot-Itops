using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class UtilityService: IUtilityService
    {
        public UtilityService()
        {
                    
        }
        private readonly Dictionary<string, string[]> intents = new()
        {
            { "application_installation", new[] { "install", "setup", "download", "software", "application", "program" } },
            { "it_support", new[] { "help", "problem", "issue", "bug", "error", "not working", "fix", "troubleshoot" } },
            { "travel_expense", new[] { "expense", "travel", "reimbursement", "receipt", "cost", "claim", "refund" } },
            { "hr_queries", new[] { "leave", "vacation", "balance", "policy", "hr", "holiday", "time off", "sick" } },
            { "sales_proposal", new[] { "proposal", "quote", "quotation", "estimate", "pricing", "contract", "deal" } },
            { "pitch_deck", new[] { "presentation", "pitch", "deck", "slides", "demo", "showcase" } },
            { "sales_content", new[] { "content", "marketing", "copy", "email", "social", "campaign", "brochure" } }
        };

            // Detect intent by matching keywords
        public string DetectIntent(string input)
        {
            input = input.ToLower();

            foreach (var intent in intents)
            {
                if (intent.Value.Any(keyword => input.Contains(keyword)))
                {
                    return intent.Key;
                }
            }

            return "unknown";
        }

            // Simulate API calls based on intent
        private async Task CallApiForIntent(string intent, string request)
        {
            switch (intent)
            {
                case "application_installation":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Installing software for request: {request}");
                    break;

                case "it_support":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] IT support handling: {request}");
                    break;

                case "travel_expense":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Processing travel expense: {request}");
                    break;

                case "hr_queries":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Answering HR query: {request}");
                    break;

                case "sales_proposal":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Creating sales proposal: {request}");
                    break;

                case "pitch_deck":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Building pitch deck: {request}");
                    break;

                case "sales_content":
                    await Task.Delay(500);
                    Console.WriteLine($"[API] Preparing sales content: {request}");
                    break;

                default:
                    await Task.Delay(200);
                    Console.WriteLine($"[API] No matching intent found for: {request}");
                    break;
            }
        }
        public async Task RunProcess(string request) 
        {
                      
             string intent = DetectIntent(request);
             await CallApiForIntent(intent, request);
            // Run all tasks in parallel
        }
    }
}
