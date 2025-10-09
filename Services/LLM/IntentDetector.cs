using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TeamsBot.Services.LLM
{
    public enum IntentType
    {
        None,
        ListSoftware,
        SpecificSoftware
    }
    public class IntentDetector
    {
        private static readonly string[] ListKeywords = { "list", "show", "available", "all", "software", "apps", "programs" };
        private static readonly string[] SoftwareNames = { "python", "nodejs", "node js", "dotnet", ".net", "java", "vs code", "visual studio", "uv", "anaconda", "npm", "yarn", "pnpm" };
        private static readonly string[] ActionKeywords = { "install", "update", "download", "uninstall", "setup", "add", "get" };

        /// <summary>
        /// Detects the user's intent (ListSoftware or SpecificSoftware)
        /// and returns a tuple containing the intent and relevant software names.
        /// </summary>
        public static (IntentType Intent, string Software) DetectIntent(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (IntentType.None, "all");

            input = input.ToLower();
            var matchedSoftware = SoftwareNames.Where(s => input.Contains(s)).ToList();

            // Rule 1: Specific software detection with action
            if (ActionKeywords.Any(a => input.Contains(a)) && matchedSoftware.Any())
            {
                return (IntentType.SpecificSoftware, matchedSoftware.FirstOrDefault());
            }

            // Rule 2: List all software
            if (Regex.IsMatch(input, @"\b(list|show|available|what|which)\b.*\b(software|apps|programs)\b") ||
                ListKeywords.Any(k => input.Contains(k)))
            {
                return (IntentType.ListSoftware, "all");
            }

            // Rule 3: Ask about specific software without explicit action
            if (matchedSoftware.Any())
            {
                return (IntentType.SpecificSoftware, matchedSoftware.FirstOrDefault());
            }

            return (IntentType.None, "all");
        }
    }
}
