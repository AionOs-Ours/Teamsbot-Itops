// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using TeamsBot.Services.Interfaces;
using TeamsBot.Services.LLM;

namespace TeamsBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly string botId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
        private readonly GeminiService _geminiService = new GeminiService();
        private readonly IBotService _botService;
        private readonly ICardService _cardService;
        public EchoBot(IServiceProvider serviceProvider)
        {
            _botService = serviceProvider.GetRequiredService<IBotService>();
            _cardService = serviceProvider.GetRequiredService<ICardService>();
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
         {
            try
            {

                await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);
                await _botService.ProcessBotMessage(turnContext, cancellationToken);
            }
            catch (System.Exception ex)
            {

                throw;
            }
        }
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome! I'm **Aries** Your AIonOS AI Assist";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
