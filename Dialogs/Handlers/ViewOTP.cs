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

namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class ViewOTP : ComponentDialog
    {
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;
        //RouteDetails objRouteDetails = new RouteDetails();
        List<RouteDetails> objRouteDetails = new List<RouteDetails>();

        public ViewOTP(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(ViewOTP))
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
            ViewOTPService objViewOTPService = new ViewOTPService();
            string botResponseType = string.Empty;
            string reply = string.Empty;
            var activity = innerDc.Context.Activity;

            if (ViewOtpConstant.ViewOtpList.Split(',').ToList().Any(t => activity.Text.ToLower().Contains(t)) && !activity.Text.ToLower().Contains(Constants.Confirm))
            {
                string outputMsg = string.Empty;
                objRouteDetails = await objViewOTPService.GetOTPDetails(userQuery, _config);
                if (objRouteDetails != null && objRouteDetails.Count > 0)
                {
                    {
                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToActivity.Attachments = new List<Attachment>();
                        replyToActivity.Text = ViewOtpConstant.OTPMessage + "\n\n";

                        foreach (var item in objRouteDetails)
                        {
                            string timestamp = item.Shift.ToString().Replace(":", "");
                            string hrFormat = Convert.ToInt32(timestamp) < 1200 ? "AM" : "PM";
                            string shiftdate = Convert.ToString(item.ShiftDate).Substring(0, 10);
                            string tripType = item.Action.First().ToString().ToUpper() + item.Action.Substring(1);

                            if (item.Action == "pick" && (!string.IsNullOrEmpty(item.BoardingOTP) || !string.IsNullOrEmpty(item.DeboardingOTP)))
                            {
                                replyToActivity.Text += $"{item.RouteSeq}.** {tripType}** || {shiftdate} || {item.Shift + hrFormat} || Route ID:{item.Routeid}\n\n";
                                replyToActivity.Text += "Boarding OTP : " + item.BoardingOTP + "  \n\n";
                            }
                            else if (!string.IsNullOrEmpty(item.BoardingOTP) || !string.IsNullOrEmpty(item.DeboardingOTP))
                            {
                                replyToActivity.Text += $"{item.RouteSeq}.** {tripType}** || {shiftdate} || {item.Shift + hrFormat} || Route ID:{item.Routeid}\n\n";
                                if (item.DeboardingOTP == null)
                                {

                                    replyToActivity.Text += "Boarding OTP : " + item.BoardingOTP + "  \n\n";

                                }
                                else
                                {
                                    replyToActivity.Text += "Boarding OTP : " + item.BoardingOTP + "  \n\n";
                                    replyToActivity.Text += ($"Deboarding OTP: {item.DeboardingOTP} \n\n");
                                }
                            }
                        }
                        await innerDc.Context.SendActivityAsync(replyToActivity);
                        await PostToUserForAnyQuery(innerDc, userQuery);
                    }
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(ViewOtpConstant.NoOtpToBeShown);
                    await PostToUserForAnyQuery(innerDc, userQuery);
                }
            }
            else if ((activity.Text.ToLower().Contains(Constants.Confirm) || activity.Text.ToLower().Contains(Constants.NotConfirm)))
            {
                if (activity.Text.ToLower().Contains(Constants.NotConfirm))
                {
                    string eid = userQuery.EnterpriseId;
                    userQuery = new UserQuery();
                    userQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);
                }
                else
                {
                    //await innerDc.Context.SendActivityAsync(Constants.ResultMessageConfirm);
                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.Text = Constants.ResultMessageConfirm;
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

                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
            }
            else
            {
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

                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                replyToActivity.Attachments.Add(tCard.ToAttachment());

                await innerDc.Context.SendActivityAsync(replyToActivity);
            }
            return await innerDc.EndDialogAsync();
        }

        private async Task PostToUserForAnyQuery(DialogContext innerDc, UserQuery userQuery)
        {
            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
            replyToActivity.Text = "Is that all ? Or do you want my assistance in any of the other transport queries?";
            replyToActivity.Attachments = new List<Attachment>();
            ThumbnailCard tCard = new ThumbnailCard()
            {
                Buttons = new List<CardAction>()
                {
                    new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                    new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                }
            };
            string eid = userQuery.EnterpriseId;
            userQuery = new UserQuery();
            userQuery.EnterpriseId = eid; eid = null;
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} in view otp";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
        }
    }
}
