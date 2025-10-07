using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using TeamsBot.Models;
using TeamsBot.Services.Abstraction;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services.Operations
{
    public class ITSupportService : Operation
    {
        public ITSupportService(IServiceProvider provider, IConfiguration config) : base(provider, config)
        {
        }

        public override async Task<OperationResponse> ExecuteOperation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public override async Task<string> WelcomeMessage()
        {
            return $"**Aries**: You are now in the IT Support section. How can I assist you further?";
        }
    }
}
