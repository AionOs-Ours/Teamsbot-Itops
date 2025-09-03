using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamsBot.Mongo;
using TeamsBot.Services.GraphService;
using TeamsBot.Services.Interfaces;
using TeamsBot.Services.LLM;

namespace TeamsBot.Services
{
    public class BotService : IBotService
    {
        private readonly IServiceProvider _provider;
        private readonly IIntuneService _intuneService;
        private readonly IServiceNowService _serviceNowService;
        private readonly IBlobService _blobService;
        private readonly GeminiService _geminiService;
        private readonly IConfiguration _config;
        private readonly MongoDb _mongoDb;
        private readonly string botId;
        public BotService(IServiceProvider provider, IConfiguration config)
        {
            _provider = provider;
            _intuneService = _provider.GetRequiredService<IIntuneService>();
            _serviceNowService = _provider.GetRequiredService<IServiceNowService>();
            _blobService = _provider.GetRequiredService<IBlobService>();
            _geminiService = new GeminiService();
            _mongoDb = new MongoDb();
            _config = config;
            botId= _config["Config:AppConfig:BotId"];
        }

        public async Task ProcessBotMessage(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userText = turnContext.Activity.Text?.Trim();
            var senderName = turnContext.Activity.From.Name;
            var graphClient = GraphHelper.GetGraphClient(_config["Config:AppConfig:BotId"], _config["Config:AppConfig:TenantId"], _config["Config:AppConfig:AppPassword"]);
            var proactiveMessageSender = new ProactiveMessageSender(graphClient);
            var userB = await proactiveMessageSender.SendMessageToUserAsync(_config["Config:AppConfig:ItAdminEmail"], _config["Config:AppConfig:BotId"]);
            var systemAdmin = await _mongoDb.FindConversationAsync(userB.Id);
            var collection = _mongoDb.GetConversationsCollection();
            string userId = turnContext.Activity.From.AadObjectId;
            var cardService = new CardService();
            var findUser = await _mongoDb.FindConversationAsync(userId);
            bool isFirst = true;
            string suiteId = string.Empty;
            if (turnContext.Activity.Value is not null && turnContext.Activity.Value.ToString().Contains("installSoftware"))
            {
                userText= JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["name"].ToString();
                suiteId = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["objectId"].ToString();
                isFirst = false;
            }

            if (userText == null) //it has to be deepak singh
            {
                var jObjectReq = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["requestId"].ToString();
                var serviceRequest = await _mongoDb.FindServiceRequestAsync(jObjectReq);

                if (turnContext.Activity.Value.ToString().Contains("approve"))
                {
                    suiteId = Convert.ToString(JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["objectId"]);
                    var card = await cardService.GetCard("Approved your request Please click Ok when you are ready for the software to be installed.", senderName, jObjectReq,suiteId);


                    var cardAttachment = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    };

                    var reply = MessageFactory.Attachment(cardAttachment);
                    // updating the status of the ticket in the db and servicenow
                    // await _serviceNowService.UpdateIncidentStatusAsync(serviceRequest.TicketNumber, "2", "In Progress");
                    var user = await _mongoDb.FindConversationAsync(serviceRequest.TeamsUserId);
                    await turnContext.Adapter.ContinueConversationAsync(
                   botId,
                   JsonConvert.DeserializeObject<ConversationReference>(user.Conversation),
                   async (proactiveTurnContext, proactiveCancellationToken) =>
                   {

                       await proactiveTurnContext.SendActivityAsync(reply, cancellationToken: proactiveCancellationToken);
                   },
                   cancellationToken);
                    //ManageEngineAuthManager manageEngineAuthManager = new ManageEngineAuthManager();
                    //var accessToken = await manageEngineAuthManager.GetAccessTokenAsync();
                    //SDPOnDemandService sDPOnDemandService = new SDPOnDemandService(accessToken);
                    //sDPOnDemandService.CreateRequestAsync("")
                }
                else if (turnContext.Activity.Value.ToString().Contains("reject"))
                {
                    var card = cardService.GetCard("Rejected your request Due to some Restrictions. please contact It Admin.", senderName, jObjectReq,suiteId);
                    var cardAttachment = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    };
                    var user = await _mongoDb.FindConversationAsync(serviceRequest.TeamsUserId);
                    var reply = MessageFactory.Attachment(cardAttachment);
                    await _serviceNowService.UpdateIncidentStatusAsync(serviceRequest.TicketNumber, "8", "Cancelled");
                    await turnContext.Adapter.ContinueConversationAsync(
                    botId,
                    JsonConvert.DeserializeObject<ConversationReference>(user.Conversation),
                    async (proactiveTurnContext, proactiveCancellationToken) =>
                    {

                        await proactiveTurnContext.SendActivityAsync(reply, cancellationToken: proactiveCancellationToken);
                    },
                    cancellationToken);

                }
                else if (turnContext.Activity.Value.ToString().Contains("Ok"))
                {
                    // check the status of the ticket in the db
                    suiteId = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["objectId"].ToString();
                    await turnContext.SendActivityAsync(MessageFactory.Text("Thank You, Your Silent Installation is underway."), cancellationToken);
                    if (suiteId == string.Empty)
                    {
                        await _intuneService.DeployApp(turnContext.Activity.From.AadObjectId);
                    }
                    else
                    {
                        var suiteCollection = _mongoDb.FindSoftwareSuiteAsync(suiteId);
                        var blob = await _blobService.GetFileContent(suiteCollection.Result.ScriptName);
                        await _intuneService.DeployScript(turnContext.Activity.From.AadObjectId, blob);
                    }
                }
            }
            else
            {
                if (isFirst) { 
                var isInstallation = userText.ToLower().Contains("install ") || userText.ToLower().Contains("notepad++ ");
                var isList = await _geminiService.GetIsListGeminiResponseAsync(userText);
                if (isList && !isInstallation)
                {
                    var softwareSuite = _mongoDb.GetSoftwareSuiteCollection().Find(Builders<SoftwareSuite>.Filter.Empty).ToListAsync();
                        foreach (var item in softwareSuite.Result)
                        {
                            var card = cardService.BuildSoftwareSuiteCard(item); // your method
                            var attachment = new Attachment
                            {
                                ContentType = "application/vnd.microsoft.card.adaptive",
                                Content = card.Result
                            };

                            var reply = MessageFactory.Attachment(attachment);
                            await turnContext.SendActivityAsync(reply, cancellationToken);
                        }
                    
                    return;
                }
                var llmRes = await _geminiService.GetGeminiResponseAsync(userText);

                if (!isInstallation)
                {
                    var llmReply = $"Aries: {llmRes.Candidates[0].Content.Parts[0].Text}";
                    await turnContext.SendActivityAsync(MessageFactory.Text(llmReply, llmReply), cancellationToken);
                    return;
                }
                }
                // var replyText = $"Echo: {turnContext.Activity.Text}";
                //await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                var conversationReference = turnContext.Activity.GetConversationReference();
                // Store the conversation reference for the current user
                
                if (findUser is null)
                {
                    var jsonString = JsonConvert.SerializeObject(conversationReference);
                    await _mongoDb.CreateConversationAsync(new Conversations { Conversation = jsonString, TeamsUserId = userId });
                }
                // create the entry in the system ticket which is created by the user
                // TODO: integrate with manage engine and create the ticket and use the ticket number in the mongo collection
                var serviceNowTicket=await _serviceNowService.CreateIncidentAsync(userText);
                var serviceRequestCollection = _mongoDb.GetServiceRequestCollection();
                var serviceRequest = new ServiceRequest(userId, userText, userB.Id,"121");

                await _mongoDb.CreateServiceRequestAsync(serviceRequest);

                // Now, send a proactive message to the target user.
                // NOTE: This will only work if the target user has initiated a conversation with the bot before.


                // gettiing the It Admin Conversation from db
                // collection.FindAsync(Builders<Conversations>.Filter.Eq("TeamsUserId", userB.Id)).Result.ToListAsync();
                if (systemAdmin is not null)// && userId != userB.Id)
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
                            new AdaptiveTextBlock($"**{senderName}** Requested: {userText}")
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
                    var cardAttachment = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    };

                    var reply = MessageFactory.Attachment(cardAttachment);
                    // await turnContext.SendActivityAsync(reply, cancellationToken);
                    await turnContext.Adapter.ContinueConversationAsync(
                    botId,
                    JsonConvert.DeserializeObject<ConversationReference>(systemAdmin.Conversation),
                    async (proactiveTurnContext, proactiveCancellationToken) =>
                    {

                        await proactiveTurnContext.SendActivityAsync(reply, cancellationToken: proactiveCancellationToken);
                    },
                    cancellationToken);
                }
            }
        }
    }
}