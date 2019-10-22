using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Logger;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Core.BotState;

namespace Accenture.CIO.WPBot
{
    public class WPBotFlowDialog : ComponentDialog
    {
        //private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private readonly StateBotAccessors _accessors;
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        public WPBotFlowDialog(StateBotAccessors accessors, ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, IConfiguration configuration)
            : base(nameof(WPBotFlowDialog))

        {
            _accessors = accessors;
            _sqlLoggerRepository = sqlLoggerRepository;

            WaterfallStep[] waterfallSteps = new WaterfallStep[]
           {
                SalutationHandlerAsync,
                LuisHandlerAsync,
                QnAHandler,
                GetUserConfirmation,
                ProcessUserChoice,
           };

            AddDialog(new WaterfallDialog(nameof(WPBotFlowDialog), waterfallSteps));
            AddDialog(new SalutationHandlerDialog(_sqlLoggerRepository));
            AddDialog(new LuisHandlerDialog(_sqlLoggerRepository, _accessors, configuration));
            AddDialog(new QnAHandlerDialog(sqlLoggerRepository, _accessors, configuration));
            AddDialog(new TextPrompt(ResponseTemplate.UserConfirmCard));
        }

        private async Task<DialogTurnResult> SalutationHandlerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(SalutationHandlerDialog));
        }
        private async Task<DialogTurnResult> LuisHandlerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
                return await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(LuisHandlerDialog)); ;
        }

        private async Task<DialogTurnResult> QnAHandler(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
                return await stepContext.EndDialogAsync();
            return await stepContext.BeginDialogAsync(nameof(QnAHandlerDialog));
        }

        private async Task<DialogTurnResult> GetUserConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
                return await stepContext.EndDialogAsync();
                stepContext.Values["PrevQuery"] = stepContext.Context.Activity.Text;
            await stepContext.Context.SendActivityAsync(Utilities.GetResourceMessage(Constants.SearchInitializationMsg));
            return await stepContext.PromptAsync(ResponseTemplate.UserConfirmCard, new PromptOptions()
            {
                Prompt = await new ResponseTemplate().RenderTemplate(stepContext.Context, "en", ResponseTemplate.UserConfirmCard),
            }, cancellationToken);
        }

        public async Task<DialogTurnResult> ProcessUserChoice(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string choice = stepContext.Context.Activity.Text.ToUpperInvariant();
            string response = string.Empty;
            switch (choice)
            {
                case "YES":
                    // await stepContext.Context.SendActivityAsync("LUIS");
                    await GetSearchResult(stepContext);
                    return await stepContext.EndDialogAsync();
                case "NO":
                    response = "Thank you.Hope to see you around";
                    TaskResult taskResult = new TaskResult()
                    {
                        Category = CategoryType.Search,
                        ModelName = "Search API",
                        Query = stepContext.Values["PrevQuery"].ToString(),
                        Intent = string.Empty,
                        Entity = string.Empty,
                        Response = response,
                        ResponseType = BotResponseType.ValidResponse,
                        Score = 1,
                        Source = CategoryType.Search
                    };
                    await stepContext.Context.SendActivityAsync(response);
                    await _sqlLoggerRepository.InsertBotLogAsync(stepContext.Context.Activity, taskResult);
                    return await stepContext.EndDialogAsync();
                default:
                    return await stepContext.BeginDialogAsync(nameof(WPBotFlowDialog));
            }

        }
        private async Task GetSearchResult(WaterfallStepContext stepContext)
        {
            string response = string.Empty;
            string query = stepContext.Values["PrevQuery"].ToString();

            Activity replyToActivity = stepContext.Context.Activity.CreateReply();
            SearchModel searchResult = AccentureSearchAPI.getSearchResult(query);

            replyToActivity.Attachments = new List<Attachment>();
            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel; int i = 0;


            foreach (var item in searchResult.hits.hits)
            {
                AdaptiveCard card = new AdaptiveCard("0.5");

                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = $"[{item.fields.cleantitle[0]}]({item.fields.cleanurl[0]})",
                    Size = AdaptiveTextSize.Small,
                    Weight = AdaptiveTextWeight.Lighter,
                    Wrap = true,
                });

                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = $"{item.fields.cleanurl[0]}",
                    Size = AdaptiveTextSize.Small,
                    Weight = AdaptiveTextWeight.Lighter,
                    Wrap = true,
                });

                if (item.fields.cleandescription != null)
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = item.fields.cleandescription[0].Length > 252 ? $"{item.fields.cleandescription[0].Substring(0, 250)}..." : item.fields.cleandescription[0],
                        Size = AdaptiveTextSize.Small,
                        Weight = AdaptiveTextWeight.Lighter,
                        Wrap = true,
                    });
                }
                replyToActivity.Attachments.Add(new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                });
                ++i;
                if (i == 4)
                {
                    string more = "More...";
                    string sQuery = Uri.EscapeDataString(query);
                    string searchUrl = $"https://search.accenture.com/?aid=all&k={sQuery}&page=1";
                    AdaptiveCard card5 = new AdaptiveCard("0.5");

                    card5.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = $"[{more}]({searchUrl})",
                        Size = AdaptiveTextSize.Small,
                        Weight = AdaptiveTextWeight.Lighter,
                        Wrap = true,
                    });


                    replyToActivity.Attachments.Add(new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card5
                    });
                    break;
                }

                response += item.fields.cleanurl[0] + " /n ";
                replyToActivity.TextFormat = TextFormatTypes.Markdown;
            }
            await stepContext.Context.SendActivityAsync(replyToActivity);
            await stepContext.Context.AskUserFeedbackAsync(_prevActivityAccessor);
            TaskResult taskResult = new TaskResult()
            {
                Category = CategoryType.Search,
                ModelName = "Search API",
                Query = query,
                Intent = string.Empty,
                Entity = string.Empty,
                Response = response,
                ResponseType = BotResponseType.ValidResponse,
                Score = 1,
                Source = string.IsNullOrEmpty(response) ? CategoryType.Search : CategoryType.BotResponse
            };
            await _sqlLoggerRepository.InsertBotLogAsync(stepContext.Context.Activity, taskResult);

        }
    }
}