using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamsBot.Models;
using TeamsBot.Models.Enums;
using TeamsBot.Mongo;
using TeamsBot.Services.GraphService;
using TeamsBot.Services.Interfaces;
using abs=TeamsBot.Services.Abstraction;
using TeamsBot.Services.LLM;

namespace TeamsBot.Services.Operations
{
    public class ApplicationInstallationService : abs.Operation
    {
        private readonly IConfiguration _config;
        private readonly IServiceProvider _provider;
        private readonly ICardService _cardService;
        private readonly IServiceNowService _serviceNowService;
        private readonly IBlobService _blobService;
        private readonly MongoDb _mongoDb;
        private readonly GeminiService _geminiService;
        private readonly string botId;
        private readonly IIntuneService _intuneService;
        public ApplicationInstallationService(IServiceProvider provider, IConfiguration config): base(provider, config)
        {
            _provider = provider;
            _config = config;
            _mongoDb = new MongoDb();
            _blobService = _provider.GetRequiredService<IBlobService>();
            _cardService = _provider.GetRequiredService<ICardService>();
            _serviceNowService = _provider.GetRequiredService<IServiceNowService>();
            botId = _config["Config:AppConfig:BotId"];
            _intuneService = _provider.GetRequiredService<IIntuneService>();
            _geminiService = new GeminiService();
        }
        public override async Task<OperationResponse> ExecuteOperation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userText = turnContext.Activity.Text?.Trim();
            string userId = turnContext.Activity.From.AadObjectId;
            var user = await _mongoDb.FindConversationAsync(userId);
            string suiteId = string.Empty;
            
            var senderName = turnContext.Activity.From.Name;
            bool isFirst = true;
            if (turnContext.Activity.Value is not null && turnContext.Activity.Value.ToString().Contains("installSoftware"))
            {
                userText = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["name"].ToString();
                suiteId = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["objectId"].ToString();
                isFirst = false;
            }

            if (userText == null && Convert.ToString(JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["Action"])!= "application_installation") //it has to be deepak singh
            {
                var jObjectReq = JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["requestId"].ToString();
                var serviceRequest = await _mongoDb.FindServiceRequestAsync(jObjectReq);
                var endUser = await _mongoDb.FindConversationAsync(serviceRequest.TeamsUserId);
                if (turnContext.Activity.Value.ToString().Contains("approve"))
                {
                    suiteId = Convert.ToString(JObject.Parse(JsonConvert.SerializeObject(turnContext.Activity.Value))["objectId"]);
                    var card = await _cardService.GetCard("Approved your request Please click Ok when you are ready for the software to be installed.", senderName, jObjectReq, suiteId);

                    return new OperationResponse(null, card, true, endUser);

                }
                else if (turnContext.Activity.Value.ToString().Contains("reject"))
                {
                    var card =await _cardService.GetCard("Rejected your request Due to some Restrictions. please contact It Admin.", senderName, jObjectReq, suiteId);
                    return new OperationResponse(null, card, true, endUser);
                    

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
                return null;
            }
            else
            {
                var softwareSuites = _mongoDb.GetSoftwareSuiteCollection();
                if (isFirst)
                {
                    var isInstallation = userText.ToLower().Contains("install ") || userText.ToLower().Contains("notepad++ ");
                    var isList = await _geminiService.GetIsListGeminiResponseAsync(userText);
                    if (isList && !isInstallation)
                    {

                        foreach (var item in softwareSuites.Result.Children<JObject>().ToArray())
                        {
                            var card = await _cardService.BuildSoftwareSuiteCard(item.ToObject<SoftwareSuite>()); // your method
                            var attachment = new Microsoft.Bot.Schema.Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = card
                            };

                            var reply = MessageFactory.Attachment(attachment);
                            await turnContext.SendActivityAsync(reply, cancellationToken);
                        }
                        return null;
                    }
                    var llmRes = await _geminiService.GetGeminiResponseAsync(userText);

                    if (!isInstallation)
                    {
                        var llmReply = $"**Aries:** {llmRes.Candidates[0].Content.Parts[0].Text}";
                        return new OperationResponse(llmReply,null, false, user);
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


                // getting the It Admin Conversation from db
                if (systemAdmin is not null)// && userId != userB.Id)
                {
                    var approvalCard = await _cardService.BuildSoftwareApprovalCard(serviceRequest, userText, senderName, suiteId);
                    return new OperationResponse(null, approvalCard, true, systemAdmin);
                }
                return null;
            }
        }

        public override async  Task<string> WelcomeMessage()
        {
            return $"**Aries**: You are now in the Application Installation section. How can I assist you further?";
        }
    }
}
