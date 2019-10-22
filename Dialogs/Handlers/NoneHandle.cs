using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Logger;
using Accenture.CIO.WPBot.Core.BotState;
using Accenture.CIO.WPBot.Core.Models;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Core.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Configuration;
using System.Net;
using Accenture.CIO.Bot.Common.Helpers;
using Newtonsoft.Json;
using Accenture.CIO.WPBot.Core.Services;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;

namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class NoneHandle : ComponentDialog
    {
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;
        LoggingMiddleware objLoggingMiddleware = new LoggingMiddleware();

        public NoneHandle(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(NoneHandle))
        {
            _accessors = accessors;
            _config = config;
            _sqlLoggerRepository = sqlLoggerRepository;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            UserQuery userQuery = await _accessors.UserQueryAccessor.GetAsync(innerDc.Context, () => new UserQuery());
            return await StartAsync(innerDc, userQuery, options);
        }

        private async Task<DialogTurnResult> StartAsync(DialogContext innerDc, UserQuery userQuery, object luisResult)
        {
            // Log None Logs
            LuisResult objLuisResult = (LuisResult)luisResult;
            //await objLoggingMiddleware.InsertNoneLuisLogs(innerDc.Context, objLuisResult, _config, userQuery.EnterpriseId);
            await _sqlLoggerRepository.InsertNoneLuisLogs(innerDc.Context, objLuisResult, userQuery.EnterpriseId);

            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
            replyToActivity.Text = Constants.NonehandleMessage;
            replyToActivity.Attachments = new List<Attachment>();
            ThumbnailCard tCard = new ThumbnailCard()
            {
                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = "View Schedule",
                        Type = ActionTypes.ImBack,
                        Value = $"View Schedule"
                    },
                    new CardAction()
                    {
                        Title = "Raise Adhoc",
                        Type = ActionTypes.ImBack,
                        Value = $"Raise Adhoc"
                    },
                    new CardAction()
                    {
                        Title = "Cancel Trip",
                        Type = ActionTypes.ImBack,
                        Value = $"Cancel Trip"
                    },
                    new CardAction()
                    {
                        Title = "View Route",
                        Type = ActionTypes.ImBack,
                        Value = $"View Route"
                    },
                    new CardAction()
                    {
                        Title = "View Adhoc Status",
                        Type = ActionTypes.ImBack,
                        Value = $"View Adhoc Status"
                    },
                    new CardAction()
                    {
                        Title = "View OTP",
                        Type = ActionTypes.ImBack,
                        Value = $"View OTP"
                    }
                }
            };

            string eid = userQuery.EnterpriseId;
            
            userQuery = new UserQuery();
            userQuery.EnterpriseId = eid; eid = null;
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);

            return await innerDc.EndDialogAsync();

            #region SuggestionAction
            //replyToActivity.SuggestedActions = new SuggestedActions()
            //{
            //    Actions =new List<CardAction>
            //    {
            //        new CardAction()
            //        {
            //            Title = "View Schedule",
            //            Type = ActionTypes.ImBack,
            //            Value = $"View Schedule"
            //        },
            //        new CardAction()
            //        {
            //            Title = "Raise Adhoc",
            //            Type = ActionTypes.ImBack,
            //            Value = $"Raise Adhoc"
            //        },
            //        new CardAction()
            //        {
            //            Title = "Cancel Trip",
            //            Type = ActionTypes.ImBack,
            //            Value = $"Cancel Trip"
            //        },
            //        new CardAction()
            //        {
            //            Title = "View Route",
            //            Type = ActionTypes.ImBack,
            //            Value = $"View Route"
            //        },
            //        new CardAction()
            //        {
            //            Title = "View Adhoc Status",
            //            Type = ActionTypes.ImBack,
            //            Value = $"View Adhoc Status"
            //        },
            //        new CardAction()
            //        {
            //            Title = "View OTP",
            //            Type = ActionTypes.ImBack,
            //            Value = $"View OTP"
            //        }
            //    }
            //};

            #endregion

        }
    }
}
