using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Dialogs.Handlers;
using Accenture.CIO.WPBot.Logger;
using System;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Core.BotState;
using System.Linq;
using Accenture.CIO.Bot.Common.Helpers;
using System.Diagnostics;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using Accenture.CIO.WPBot.Core.Services;

namespace Accenture.CIO.WPBot
{
    public class LuisHandlerDialog : ComponentDialog
    {
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private DialogSet _dialogs;
        private readonly StateBotAccessors _accessors;
        UserQuery userQuery = null;
        LoggingMiddleware objLoggingMiddleware = new LoggingMiddleware();

        public LuisHandlerDialog(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
            : base(nameof(LuisHandlerDialog))
        {
            _accessors = accessors;
            _config = config;
            _sqlLoggerRepository = sqlLoggerRepository;
            AddDialog(new GetAdhocRequestDetails(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new ViewRoute(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new ViewOTP(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new ViewSchedule(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new CancelTrip(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new AdhocStatus(_sqlLoggerRepository, _accessors, _config));
            AddDialog(new NoneHandle(_sqlLoggerRepository, _accessors, _config));
        }
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await StartAsync(innerDc);
        }

        private async Task<DialogTurnResult> StartAsync(DialogContext innerDc)
        {
            string response = string.Empty;
            string botResponseType = string.Empty;
            string activityText = string.Empty;
            string message = innerDc.Context.Activity.Text;
            bool isIntentFound = false;
            message = message.ToLower().Replace("bang", "bdc");
            userQuery = await _accessors.UserQueryAccessor.GetAsync(innerDc.Context, () => new UserQuery());
            ConversationData conversationData =
            await _accessors.ConversationDataAccessor.GetAsync(innerDc.Context, () => new ConversationData());

            // Get the LuisModel
            LuisResult luisResult = await LuisHelper.GetLuisResult(message, _config);

            // Making userquery to be null If user moves from one Module to another
            if (string.IsNullOrEmpty(userQuery.LuisIntent))
            {
                userQuery.LuisIntent = luisResult.TopScoringIntent.Intent;
            }
            if (!(luisResult.TopScoringIntent.Intent == userQuery.LuisIntent))
            {
                string eid = userQuery.EnterpriseId;
                userQuery = new UserQuery();
                userQuery.EnterpriseId = eid; eid = null;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                luisResult = await LuisHelper.GetLuisResult(message, _config);
            }

            
            // Get the Entities and its values.
            GetUserData(luisResult, userQuery);
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

            // Log LUIS Logs
            await _sqlLoggerRepository.InsertLuisLogs(innerDc.Context, luisResult, userQuery.EnterpriseId);

            //await innerDc.Context.GetUserQuery
            if ((luisResult.TopScoringIntent.Score > Convert.ToDouble(_config["Luis:Threshold"]))
                && (luisResult.TopScoringIntent.Intent != null)
                && (luisResult.TopScoringIntent.Intent != LuisConstants.NoneIntent))
            {
                
                //_ApiInput = new ApiInputDetails() { Input = Utilities.GetCorpusInput(innerDc.Context, luisResult), ApiIdentifier = "Luis" };
                //var res = LuisHelper.GetApiResponseAsync(_config, _ApiInput);
                switch (luisResult.TopScoringIntent.Intent)
                {
                    case "GetAdhocRequestDetails":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(GetAdhocRequestDetails), luisResult);
                        break;
                    case "ViewRoute":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(ViewRoute), luisResult);
                        break;
                    case "GetScheduleDetails":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(ViewSchedule), luisResult);
                        break;
                    case "ViewOTP":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(ViewOTP), luisResult);
                        break;
                    case "CancelTrip":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(CancelTrip), luisResult);
                        break;
                    case "GetAdhocStatus":
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(AdhocStatus), luisResult);
                        break;
                    default:
                        isIntentFound = true;
                        await innerDc.BeginDialogAsync(nameof(NoneHandle), luisResult);
                        break;
                }
            }
            else
            {
                isIntentFound = true;
                await innerDc.BeginDialogAsync(nameof(NoneHandle), luisResult);
            }
            //await _sqlLoggerRepository.InsertBotLogAsync(innerDc.Context.Activity, taskResultNonLuis);

            return await innerDc.EndDialogAsync(result: isIntentFound);
        }

        private void GetUserData(LuisResult luisResult, UserQuery userQuery)
        {
            string Entity = Utilities.GetEntities(luisResult);
            Dictionary<string, string> entityDictionary = new Dictionary<string, string>();
            var Listentity = Entity.Split(',').Select(part => part.Split(':')).Where(part => part.Length == 2).ToList();
            foreach (var list in Listentity)
            {
                var items = list;
                if (!entityDictionary.ContainsKey(items[0]))
                {
                    entityDictionary.Add(items[0].Trim(), items[1].Trim());
                }
            }

            var dictionary = entityDictionary;

            //var dictionary = Entity.Split(',').Select(part => part.Split(':')).Where(part => part.Length == 2).ToDictionary(sp => sp[0].Trim(), sp => sp[1].Trim());

            string adhocType = string.Empty;
            if (dictionary.TryGetValue("AdhocType", out adhocType))
            {
                userQuery.AdhocType = adhocType;
            }

            string adhocFacility = string.Empty;
            if (dictionary.TryGetValue("AdhocFacility", out adhocFacility))
            {
                userQuery.AdhocFacility = adhocFacility;
            }

            string adhocDestinationFacility = string.Empty;
            if (dictionary.TryGetValue("AdhocDestinationFacility", out adhocDestinationFacility))
            {
                userQuery.AdhocDestinationFacility = adhocDestinationFacility;
            }

            string adhocShift = string.Empty;
            if (dictionary.TryGetValue("AdhocShift", out adhocShift))
            {
                userQuery.AdhocShift = adhocShift.Replace(" . ", "");
                if (userQuery.AdhocShift.Length == 3)
                {
                    userQuery.AdhocShift = userQuery.AdhocShift.Insert(0, "0");
                }
                else
                    userQuery.AdhocShift = userQuery.AdhocShift;
            }

            string viewRoute = string.Empty;
            if (dictionary.TryGetValue("ViewRoute", out viewRoute))
            {
                userQuery.ViewRoute = viewRoute;
            }

            string adhocDay = string.Empty;
            if (dictionary.TryGetValue("AdhocDay", out adhocDay))
                userQuery.AdhocDay = adhocDay;

            string cancelTripDate = string.Empty;
            if (dictionary.TryGetValue("CancelTripDate", out cancelTripDate))
                userQuery.CancelTripDate = cancelTripDate;

            string viewSchedule = string.Empty;
            if (dictionary.TryGetValue("ViewSchedule", out viewSchedule))
                userQuery.ViewSchedule = viewSchedule;

            string adhocReason = string.Empty;
            if (dictionary.TryGetValue("AdhocReason", out adhocReason))
                userQuery.AdhocReason = adhocReason;

            string adhocChargeCode = string.Empty;
            if (dictionary.TryGetValue("AdhocChargeCode", out adhocChargeCode))
                userQuery.AdhocChargeCode = adhocChargeCode;

            string confirmation = string.Empty;
            if (dictionary.TryGetValue("Confirmation", out confirmation))
            {
                if (!string.IsNullOrEmpty(userQuery.Context))
                {
                    if (userQuery.Context.ToLower().Equals(Constants.EarlyDropInAdhocRequest.ToLower()))
                    {
                        userQuery.AdhocEarlyDrop = confirmation;
                    }
                    else if (userQuery.Context.ToLower().Equals(Constants.ReviewAdhocRequestDetail.ToLower()))
                    {
                        userQuery.ConfirmRaiseAdhoc = confirmation;
                    }
                }
            }

            string adhocDateFormat = string.Empty;
            if (dictionary.TryGetValue("AdhocDateFormat", out adhocDateFormat))
            {

                adhocDateFormat = adhocDateFormat.ToLower().Replace("st", "");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("nd", "");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("rd", "");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("th", "");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("augu", "august");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("moay", "monday");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("ursday", "thursday");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("satuay", "saturday");
                adhocDateFormat = adhocDateFormat.ToLower().Replace("suay", "sunday");

                userQuery.AdhocDateFormat = adhocDateFormat;
            }

        }
    }
}
