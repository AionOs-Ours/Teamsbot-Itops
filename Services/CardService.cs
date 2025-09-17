using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Graph.Beta.Models;
using Newtonsoft.Json;
using TeamsBot.Mongo;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class CardService: ICardService
    {
        public CardService()
        {
                    
        }
        public async Task<AdaptiveCard> GetCard(string responseMsg, string senderName, string serviceRequest,string objectId) {
            if(string.IsNullOrEmpty(responseMsg))
            {
                responseMsg = "Approved your request Please click on Ok when you are ready for the software to be insatlled.";
            }
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
            {
                Body = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock("🎯 User Request Status")
                            {
                                Size = AdaptiveTextSize.ExtraLarge,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                                Spacing = AdaptiveSpacing.Large
                            },
                            new AdaptiveImage("https://adaptivecards.io/content/cats/1.png")
                            {
                                Size = AdaptiveImageSize.Medium,
                                Style = AdaptiveImageStyle.Person,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                                AltText = "User Avatar"
                            },
                            new AdaptiveTextBlock($"📝 Response Summary of {serviceRequest}")
                            {
                                Size = AdaptiveTextSize.Medium,
                                Weight = AdaptiveTextWeight.Bolder,
                                Separator = true,
                                Spacing = AdaptiveSpacing.Medium
                            },
                            new AdaptiveTextBlock($"**{senderName}** - {responseMsg}")
                            {
                                Wrap = true,
                                Spacing = AdaptiveSpacing.Small,
                                Color = AdaptiveTextColor.Default
                            }
                        },
                Actions = new List<AdaptiveAction>
                        {
                            new AdaptiveSubmitAction
                            {
                                Title = "✅ OK",
                                Style = "positive",
                                Data = new { action = "Ok", requestId=serviceRequest ,objectId=objectId}
                            }
                        }
            };
            return card;
        }

        public async Task<AdaptiveCard> BuildSoftwareSuiteCard(SoftwareSuite suite)
        {
            var card = new AdaptiveCard("1.4")
            {
                Body = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = $"🧰 {suite.SuiteName}",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = $"**Category**: {suite.Category}",
                    Size = AdaptiveTextSize.Medium,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = "Included Software:",
                    Weight = AdaptiveTextWeight.Bolder,
                    Separator = true
                }
            }
            };

            foreach (var software in suite.Softwares)
            {
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = $"- **{software.Name}** v {software.Version}\n`{software.InstallScript}`",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Small
                });
            }

            card.Actions.AddRange(new List<AdaptiveAction>
                        {
                            new AdaptiveSubmitAction
                            {
                                Title = "✅ Install",
                                Style = "positive",
                                Data = new { action = "installSoftware", requestId=suite.Id.ToString() , name =suite.SuiteName, objectId= suite.Id.ToString()}
                            }
                        });

            return card;
        }
        public async Task<AdaptiveCard> BuildSoftwareApprovalCard(ServiceRequest serviceRequest,string userText,string sender, string suiteId)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
            {
                Body = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock("🎯 User Request")
                            {
                                Size = AdaptiveTextSize.ExtraLarge,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                                Spacing = AdaptiveSpacing.Large
                            },
                            new AdaptiveImage("https://adaptivecards.io/content/cats/1.png")
                            {
                                Size = AdaptiveImageSize.Medium,
                                Style = AdaptiveImageStyle.Person,
                                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                                AltText = "User Avatar"
                            },
                            new AdaptiveTextBlock($"📝 Request Summary {serviceRequest.TicketNumber}")
                            {
                                Size = AdaptiveTextSize.Medium,
                                Weight = AdaptiveTextWeight.Bolder,
                                Separator = true,
                                Spacing = AdaptiveSpacing.Medium
                            },
                            new AdaptiveTextBlock($"**{sender}** Requested: {userText}")
                            {
                                Wrap = true,
                                Spacing = AdaptiveSpacing.Small,
                                Color = AdaptiveTextColor.Default
                            }
                        },
                Actions = new List<AdaptiveAction>
                        {
                            new AdaptiveSubmitAction
                            {
                                Title = "✅ Approve",
                                Style = "positive",
                                Data = new { action = "approve" , requestId=serviceRequest.TicketNumber , objectId=suiteId }
                            },
                            new AdaptiveSubmitAction
                            {
                                Title = "❌ Reject",
                                Style = "destructive",
                                Data = new { action = "reject" }
                            },
                            new AdaptiveSubmitAction
                            {
                                Title = "🔍 More Info",
                                Data = new { action = "info" }
                            }
                        }
            };

            return card;
        }
    }
}

