using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Accenture.CIO.WPBot.Core;
using System.Collections.Generic;

namespace Accenture.CIO.WPBot
{
    public class ResponseTemplate : TemplateManager
    {
        /// <summary>
        /// UserConfirmCard
        /// </summary>
        public const string UserConfirmCard = "UserConfirmCard";

        /// <summary>
        /// Initializes a new instance of the <see cref="EBResponse"/> class.
        /// </summary>
        public ResponseTemplate()
        {
            this.Register(new DictionaryRenderer(_responseTemplates));
        }
        /// <summary>
        /// The response templates.
        /// </summary>
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
               { UserConfirmCard, (context, data) => UserConfirmationCard(context, data) },
            },
        };

        private static IMessageActivity UserConfirmationCard(ITurnContext context, dynamic data)
        {
            var reply = context.Activity.CreateReply();
            var card = new HeroCard
            {
                Title = "Do you want to continue ?",
                Buttons = new List<CardAction>()
                {
                    new CardAction() { Type = ActionTypes.ImBack, Title = Constants.Yes, Value = Constants.Yes },
                    new CardAction() { Type = ActionTypes.ImBack, Title = Constants.No, Value = Constants.No },
                },
            };

            reply.Attachments.Add(card.ToAttachment());
            return reply;
        }
    }
}
