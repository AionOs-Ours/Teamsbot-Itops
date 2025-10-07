using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamsBot.Models;
using TeamsBot.Models.Enums;
using TeamsBot.Mongo;
using TeamsBot.Services.Abstraction;
using TeamsBot.Services.GraphService;
using TeamsBot.Services.Interfaces;
using TeamsBot.Services.LLM;
using TeamsBot.Services.Operations;

namespace TeamsBot.Services
{
    public class BotService : IBotService
    {
        private readonly IServiceProvider _provider;
        private readonly IIntuneService _intuneService;
        private readonly IServiceNowService _serviceNowService;
        private readonly IBlobService _blobService;
        private readonly IUtilityService _utilityService;
        private readonly ICardService _cardService;
        private readonly GeminiService _geminiService;
        private readonly IConfiguration _config;
        private Operation operation;
        private readonly MongoDb _mongoDb;
        private readonly string botId;
        private readonly GitService _gitService;
        public BotService(IServiceProvider provider, IConfiguration config)
        {
            _provider = provider;
            _intuneService = _provider.GetRequiredService<IIntuneService>();
            _serviceNowService = _provider.GetRequiredService<IServiceNowService>();
            _blobService = _provider.GetRequiredService<IBlobService>();
            _cardService = _provider.GetRequiredService<ICardService>();
            _utilityService = _provider.GetRequiredService<IUtilityService>();
            _geminiService = new GeminiService();
            _mongoDb = new MongoDb();
            _gitService = new GitService();
            _config = config;
            botId = _config["Config:AppConfig:BotId"];
        }
        public async Task ProcessMessage(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken) 
        {
            try
            {
                //we need to find the user conversation in the db and check if the user has selected any menu
                var userText = turnContext.Activity.Text?.Trim();
                string userId = turnContext.Activity.From.AadObjectId;
                var user = await _mongoDb.FindConversationAsync(userId);
                //if user doesn't have chat history then create one
                var isChatAllowed = await CheckConversation(user, userId, turnContext, cancellationToken);
                // check if the daily chat llimit is done or not 
                if (isChatAllowed == ChatAccessEnum.Reject)
                {
                    return;
                }
                var intent = GetCardResponseIntent(turnContext, cancellationToken).Result;
                // selectedMenu is not there it will show him the menu
                if (user.SelectedMenu==0 || (userText!=null && userText.ToLower().Contains("menu"))) 
                {
                    await CreateInitialMenu(turnContext, cancellationToken);
                }
                //if user text is null and card select option is from main menu
                else if(userText == null && intent != "unknown")
                {
                    _= Enum.TryParse<MainMenuEnum>(intent, ignoreCase: true, out var result);
                    await _mongoDb.UpdateConversationAsync(new Conversations
                    {
                        Conversation = user.Conversation,
                        TeamsUserId = user.TeamsUserId,
                        SelectedMenu = (int)result
                    });
                    switch (intent)
                    {
                        case "application_installation":
                            {
                                
                                operation = new ApplicationInstallationService(_provider, _config);
                                sendMessage(turnContext,new OperationResponse(await operation.WelcomeMessage(),null,false,null),cancellationToken); 
                            }
                            break;
                        case "it_support":
                            {
                                //operation = _provider.GetRequiredService<ITSupportService>();
                                //sendMessage(turnContext, new OperationResponse(await operation.WelcomeMessage(), null, false, null), cancellationToken);
                            }
                            break;
                    }

                }
                else if (userText == null && user.SelectedMenu!=0)
                {
                    await GetIntentAndAct(userText, user, turnContext, cancellationToken);
                    
                }
                

            }
            catch (Exception)
            {

                throw;
            }
        }
        private async Task sendMessage(ITurnContext<IMessageActivity> turnContext,OperationResponse response, CancellationToken cancellationToken)
        {
            IMessageActivity reply;
            if (response.Card is not null )
            {
                var attachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = response.Card
                };

                reply = MessageFactory.Attachment(attachment);
            }
            else {
                reply = MessageFactory.Text(response.Reply, response.Reply);
            }
            if (response.ToOtherUser)
            {
                await turnContext.Adapter.ContinueConversationAsync(
                  botId,
                  JsonConvert.DeserializeObject<ConversationReference>(response.EndUserConversation.Conversation),
                  async (proactiveTurnContext, proactiveCancellationToken) =>
                  {

                      await proactiveTurnContext.SendActivityAsync(reply, cancellationToken: proactiveCancellationToken);
                  },
                  cancellationToken);
            }
            else
            {
                turnContext.SendActivityAsync(reply, cancellationToken);
            }
        }

        public async Task ProcessBotMessage(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var userText = turnContext.Activity.Text?.Trim();
                var senderName = turnContext.Activity.From.Name;
                var graphClient = GraphHelper.GetGraphClient(_config["Config:AppConfig:BotId"], _config["Config:AppConfig:TenantId"], _config["Config:AppConfig:AppPassword"]);
                var proactiveMessageSender = new ProactiveMessageSender(graphClient);
                var userB = await proactiveMessageSender.SendMessageToUserAsync(_config["Config:AppConfig:ItAdminEmail"], _config["Config:AppConfig:BotId"]);
                var systemAdmin = await _mongoDb.FindConversationAsync(userB.Id);
                var collection = _mongoDb.GetConversationsCollection();
                string userId = turnContext.Activity.From.AadObjectId;
                var findUser = await _mongoDb.FindConversationAsync(userId);
                bool isFirst = true;
                string suiteId = string.Empty;
                var softwareSuites = _mongoDb.GetSoftwareSuiteCollection();
                if (await GetCardResponseIntent(turnContext,cancellationToken)== "application_installation") { 
                if (turnContext.Activity.Value is not null && turnContext.Activity.Value.ToString().Contains("installSoftware"))
                {
                    userText = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["name"].ToString();
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
                        var card = await _cardService.GetCard("Approved your request Please click Ok when you are ready for the software to be installed.", senderName, jObjectReq, suiteId);


                        var cardAttachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = card
                        };

                        var reply = MessageFactory.Attachment(cardAttachment);
                        // updating the status of the ticket in the db and servicenow
                        var user = await _mongoDb.FindConversationAsync(serviceRequest.TeamsUserId);
                        await turnContext.Adapter.ContinueConversationAsync(
                       botId,
                       JsonConvert.DeserializeObject<ConversationReference>(user.Conversation),
                       async (proactiveTurnContext, proactiveCancellationToken) =>
                       {

                           await proactiveTurnContext.SendActivityAsync(reply, cancellationToken: proactiveCancellationToken);
                       },
                       cancellationToken);
                    }
                    else if (turnContext.Activity.Value.ToString().Contains("reject"))
                    {
                        var card = _cardService.GetCard("Rejected your request Due to some Restrictions. please contact It Admin.", senderName, jObjectReq, suiteId);
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
                            var scriptName = Convert.ToString(suiteCollection.Result.FirstOrDefault()["ScriptName"]);
                            var blob = await _blobService.GetFileContent(scriptName);
                            await _intuneService.DeployScript(turnContext.Activity.From.AadObjectId, blob, scriptName);
                        }
                    }
                }
                else
                {
                    
                    var isChatAllowed = await CheckConversation(findUser, userId, turnContext, cancellationToken);
                    if (isChatAllowed == ChatAccessEnum.Reject)
                    {
                        return;
                    }
                    if (isFirst)
                    {
                        var isInstallation = userText.ToLower().Contains("install ") || userText.ToLower().Contains("notepad++ ");
                        var isList = await _geminiService.GetIsListGeminiResponseAsync(userText);
                        if (isList && !isInstallation)
                        {

                            foreach (var item in softwareSuites.Result.Children<JObject>().ToArray())
                            {
                                var card = _cardService.BuildSoftwareSuiteCard(item.ToObject<SoftwareSuite>()); // your method
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
                            var llmReply = $"**Aries:** {llmRes.Candidates[0].Content.Parts[0].Text}";
                            await turnContext.SendActivityAsync(MessageFactory.Text(llmReply, llmReply), cancellationToken);
                            return;
                        }
                    }

                    //create the entry in the system ticket which is created by the user
                    // TODO: integrate with manage engine and create the ticket and use the ticket number in the mongo collection
                    var serviceNowTicket = await _serviceNowService.CreateIncidentAsync(userText);
                    var serviceRequestCollection = _mongoDb.GetServiceRequestCollection();
                    var serviceRequest = new ServiceRequest(userId, userB.Id, userText, "121");

                    await _mongoDb.CreateServiceRequestAsync(serviceRequest);

                    //Now, send a proactive message to the target user.
                    // NOTE: This will only work if the target user has initiated a conversation with the bot before.


                    // gettiing the It Admin Conversation from db
                    if (systemAdmin is not null)// && userId != userB.Id)
                    {
                        var approvalCard = await _cardService.BuildSoftwareApprovalCard(serviceRequest, userText, senderName, suiteId);
                        var cardAttachment = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = approvalCard
                        };

                        var reply = MessageFactory.Attachment(cardAttachment);
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
            catch (Exception ex)
            {
                var errorMessage = $"**Aries**: Oops! Something went wrong. Please try again later.";
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                // Log the exception (ex) as needed for further investigation
            }
        }
        public static DateTime ConvertUtcToIst(DateTime utcTime)
        {
            TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, istZone);
            return istTime;
        }
        // Fix missing semicolon in CheckConversation method
        private async Task<ChatAccessEnum> CheckConversation(Conversations conversation, string userId, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var intent = await GetCardResponseIntent(turnContext, cancellationToken);
            var conversationReference = turnContext.Activity.GetConversationReference();
            // Store the conversation reference for the current user
            var jsonString = JsonConvert.SerializeObject(conversationReference);
            if (conversation is null)
            {
                await _mongoDb.CreateConversationAsync(new Conversations { Conversation = jsonString, TeamsUserId = userId, PromptCount = 0, });
            }
            else
            {
                if (conversation.PromptCount >= 20)
                {
                    var replyText = $"**Aries**: Your 20 prompts for the day is consumed. Please try again tomorrow.";
                    await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                    return ChatAccessEnum.Reject;
                }
                else
                {
                    var lastEntry = ConvertUtcToIst(conversation.ModifiedAt);
                    var currentTime = ConvertUtcToIst(DateTime.UtcNow);
                    if (lastEntry.Date < currentTime.Date)
                    {
                        conversation.PromptCount = 0;
                    }
                    conversation.PromptCount += 1;
                    if(intent != "unknown")
                    conversation.SelectedMenu =(int)((MainMenuEnum)Enum.Parse(typeof(MainMenuEnum), intent)); // <-- Added missing semicolon here
                    conversation.ModifiedAt = DateTime.UtcNow;
                }
                await _mongoDb.UpdateConversationAsync(conversation);
            }
            return ChatAccessEnum.Ok;
        }

        private async Task GetIntentAndAct(string userText, Conversations user, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //var intent = await Task.FromResult(_utilityService.DetectIntent(userText));
            //if (intent == "unknown")
            //    CreateInitialMenu(turnContext, cancellationToken).GetAwaiter().GetResult();
            switch (((MainMenuEnum)user.SelectedMenu).ToString())
            {
                case "application_installation":
                    operation= new ApplicationInstallationService(_provider,_config);
                    var operationResponse=await operation.ExecuteOperation(turnContext, cancellationToken);
                    break;
                //case "it_support":
                    //return "IT Support";
                //case "travel_expense":
                //    //return "Travel Expense";
                //case "hr_queries":
                //    return "HR Queries";
                //case "sales_proposal":
                //    return "Sales Proposal";
                //case "pitch_deck":
                //    return "Pitch Deck";
                //case "sales_content":
                //    return "Sales Content";
                //default:
                    //return "General Inquiry";
            }
        }
        private async Task CreateInitialMenu(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var menuCard = await _cardService.CreateMainMenuCard();
            var attachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = menuCard
            };
            var reply = MessageFactory.Attachment(attachment);
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task<string> GetCardResponseIntent(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if(!String.IsNullOrEmpty(turnContext.Activity.Text))
                return "unknown";
            bool isValid = Enum.TryParse<MainMenuEnum>(Convert.ToString(JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["Action"]), ignoreCase: true, out var result);
           return isValid ? result.ToString() : "unknown";
        }

    }
}