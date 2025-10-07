using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta.Models;
using TeamsBot.Models;
using TeamsBot.Mongo;
using TeamsBot.Services.GraphService;

namespace TeamsBot.Services.Abstraction
{
    public abstract class Operation
    {
        protected User userB;
        protected Conversations systemAdmin;
        public abstract Task<string> WelcomeMessage();
        public abstract Task<OperationResponse> ExecuteOperation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken);
        protected IConfiguration _config { get; set; }
        protected IServiceProvider _provider { get; set; }
        protected Operation(IServiceProvider provider, IConfiguration config)
        {
            _config = config;
            _provider = provider;
            InitializeDefaults().GetAwaiter();

        }
        protected async Task InitializeDefaults()
        {
            var graphClient = GraphHelper.GetGraphClient(_config["Config:AppConfig:BotId"], _config["Config:AppConfig:TenantId"], _config["Config:AppConfig:AppPassword"]);
            var proactiveMessageSender = new ProactiveMessageSender(graphClient);
            userB = await proactiveMessageSender.SendMessageToUserAsync(_config["Config:AppConfig:ItAdminEmail"], _config["Config:AppConfig:BotId"]);
            var _mongoDb = new MongoDb();
            systemAdmin = await _mongoDb.FindConversationAsync(userB.Id);
        }
    }
}
