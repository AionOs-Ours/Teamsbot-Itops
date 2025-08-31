// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Supabase.Gotrue;
using TeamsBot.Mongo;
using TeamsBot.Services;
using TeamsBot.Services.GraphService;
using TeamsBot.Services.Interfaces;
using TeamsBot.Services.LLM;
using TeamsBot.Services.ManageEngine;
using static Supabase.Gotrue.Constants;
using Attachment = Microsoft.Bot.Schema.Attachment;

namespace TeamsBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly string botId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
        private readonly GeminiService _geminiService = new GeminiService();
        private readonly IBotService _botService;
        public EchoBot(IServiceProvider serviceProvider)
        {
            _botService = serviceProvider.GetRequiredService<IBotService>();
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
            var welcomeText = "Hello and welcome! I'm Aries Your ITOPS-Bot";
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
