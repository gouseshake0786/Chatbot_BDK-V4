using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Accenture.CIO.WPBot
{
    /// <summary>
    ///  Handles user salutation messages 
    /// </summary>
    public class SalutationHandlerDialog : ComponentDialog
    {
        private ILoggerRepository<SqlLoggerRepository> _loggerRepository;
        private static List<SalutationModel> lstIntentUtterance = new List<SalutationModel>();
        private static List<SalutationResponseModel> lstSalutationResponse = new List<SalutationResponseModel>();
        public SalutationHandlerDialog(ILoggerRepository<SqlLoggerRepository> loggerRepository)
            : base(nameof(SalutationHandlerDialog))
        {
            if (!lstIntentUtterance.Any())
                lstIntentUtterance = Utilities.ReadCSVFile<SalutationModel>(Directory.GetCurrentDirectory() + "\\wwwroot\\Repositories\\SalutationQnA.csv");

            if (!lstSalutationResponse.Any())
                lstSalutationResponse = Utilities.ReadCSVFile<SalutationResponseModel>(Directory.GetCurrentDirectory() + "\\wwwroot\\Repositories\\SalutationResponse.csv");

            _loggerRepository = loggerRepository ?? throw new ArgumentNullException(nameof(loggerRepository));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await StartAsync(innerDc);
        }

        /// <summary>
        /// identifies intent
        /// </summary>
        /// <param name="innerDc"></param>
        /// <returns></returns>
        private async Task<DialogTurnResult> StartAsync(DialogContext innerDc)
        {
            BotAssert.ContextNotNull(innerDc.Context);
            string response = string.Empty;
            var message = innerDc.Context.Activity;

            string intent = await Utilities.PredictSalutationIntent(lstIntentUtterance, message.Text);
            if (!string.IsNullOrEmpty(intent))
            {
                switch (intent)
                {
                    case Constants.Greetings:
                        GreetingHandler(out response, message);
                        break;
                    case Constants.IntentWelcome:
                    case Constants.IntentClosurePositive:
                    case Constants.IntentClosureNegative:
                        GetWelcomeClosureResponse(out response, intent);
                        break;
                }

                await innerDc.Context.SendActivityAsync(response.Replace("<FirstName>", innerDc.Context.Activity.From.Name));

                // sql logging
                //TaskResult taskResult = new TaskResult()
                //{
                //    Category = CategoryType.Salutation,
                //    ModelName = "SalutationQnA.csv",
                //    Intent = intent,
                //    Entity = string.Empty,
                //    Response = response,
                //    ResponseType = BotResponseType.ValidResponse,
                //    Score = 1,
                //    Source = string.IsNullOrEmpty(response) ? CategoryType.Salutation : CategoryType.BotResponse
                //};
                //await _loggerRepository.InsertBotLogAsync(innerDc.Context.Activity, taskResult);
                return await innerDc.EndDialogAsync(result: true);
            }
            return await innerDc.EndDialogAsync(result: false);
        }

        /// <summary>
        /// Responds to welcome closure intent.
        /// </summary>
        /// <param name="response">response for intent.</param>
        /// <param name="intent">intent.</param>
        private void GetWelcomeClosureResponse(out string response, string intent)
        {
            var lstResponses = lstSalutationResponse.Where(x => x.Intent.Equals(intent, StringComparison.InvariantCultureIgnoreCase)).ToList();
            response = lstResponses[new Random().Next(0, lstResponses.Count())].Response;
        }


        /// <summary>
        /// Geeting Utterance will be handled here.
        /// </summary>
        /// <param name="userResponse">userResponse.</param>
        private void GreetingHandler(out string userResponse, Activity activity)
        {
            int currentHour = Convert.ToInt32(activity.GetChannelDataValue("usertime")); ;
            userResponse = string.Empty;
            if (currentHour < 12)
            {
                userResponse = Utilities.GetResourceMessage(Constants.MorningSalutation);
            }
            else if (currentHour >= 12 && currentHour < 16)
            {
                userResponse = Utilities.GetResourceMessage(Constants.AfternoonSalutation);
            }
            else if (currentHour >= 16 && currentHour < 24)
            {
                userResponse = Utilities.GetResourceMessage(Constants.EveningSalutation);
            }
        }

    }
}
