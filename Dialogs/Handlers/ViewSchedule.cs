using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Core.BotState;
using Accenture.CIO.WPBot.Logger;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Core.Models;
using Accenture.CIO.WPBot.Core.Services;
using Accenture.CIO.Bot.Common.Helpers;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.Globalization;

namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class ViewSchedule : ComponentDialog
    {
        string dteRequested = string.Empty;
        List<ScheduleDetails> sch = new List<ScheduleDetails>();
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;

        public ViewSchedule(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(ViewSchedule))
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
        private async Task<DialogTurnResult> StartAsync(DialogContext innerDc, UserQuery searchQuery, object luisResult)
        {

            ViewScheduleService objViewScheduleService = new ViewScheduleService();
            RouteInfo route = new RouteInfo();
            string[] stringArray = new string[15];
            int j = 0;
            string inputDatefomrat = string.Empty;
            //Activity response = innerDc.Context.Activity.CreateReply();
            var inputMsg = innerDc.Context.Activity;
            string botResponseType = string.Empty;
            string reply = string.Empty;
            //bool isResponded = false;

            if (!string.IsNullOrEmpty(searchQuery.ViewSchedule) && (searchQuery.ViewSchedule.ToLower().Equals(Constants.ViewScheduleEntity) || searchQuery.ViewSchedule.ToLower().Equals(Constants.ScheduleInTms)) && string.IsNullOrEmpty(searchQuery.AdhocDay) && string.IsNullOrEmpty(searchQuery.Confirmation) && !inputMsg.Text.Trim().ToLower().Contains("confirm")
                && !inputMsg.Text.Trim().ToLower().Contains("notconfirm") && !inputMsg.Text.Trim().ToLower().Contains("show details in view schedule") && !inputMsg.Text.Trim().ToLower().Contains("dont show details in view schedule"))
            {
                string dtViewSchedule = "";
                if (BotHelper.IsDateContains(inputMsg.Text, out inputDatefomrat))
                {
                    if (!string.IsNullOrEmpty(inputDatefomrat))
                    {
                        string dateRequired = string.Empty;

                        DateTime test;
                        DateTime testdate;
                        if (DateTime.TryParse(inputDatefomrat, out testdate))
                        {
                            DateTime dateInput = Convert.ToDateTime(inputDatefomrat);
                            CultureInfo invC = CultureInfo.InvariantCulture;
                            var ConvertedDate = dateInput.ToString("d", invC);
                            if (!(DateTime.TryParseExact(ConvertedDate, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                            {
                                searchQuery.Context = Constants.InputDateFormatInViewSchedule;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync(Constants.InputDateInViewSchdule);
                            }
                            else
                            {
                                DateTime dt = Convert.ToDateTime(inputDatefomrat);
                                dtViewSchedule = dt.ToString("MM/dd/yyyy");
                                //Display the message If input date is less that current date
                                if (dt.Date < DateTime.Now.Date)
                                {
                                    searchQuery.Context = Constants.InputDateFormatInViewSchedule;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(Constants.pastDateMessageInViewSchedule);
                                }
                                else
                                {
                                    sch = await objViewScheduleService.DisplayViewScheduleDetails(dtViewSchedule, searchQuery, _config);
                                    searchQuery.lstViewSchedule = new List<ScheduleDetails>();
                                    searchQuery.lstViewSchedule.AddRange(sch);
                                    string resultMessage = string.Empty;
                                    string resultCardMessage = string.Empty;
                                    resultMessage += $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n\n\n";
                                    var UName = string.Empty;
                                    if (sch != null && sch.Count > 0)
                                    {

                                        for (int i = 0; i < sch.Count; i++)
                                        {
                                            var SchDay0 = sch[i].Sch;
                                            SchDay0 = SchDay0.Replace("<br />", " ||   Drop : ");
                                            SchDay0 = SchDay0.Replace("NULL", "NA");
                                            var Day0 = sch[i].StartDate;
                                            UName = sch[0].UserName;
                                            //Display the schedule only for the current date
                                            if (Day0 == dt)
                                            {
                                                if (i >= 7)
                                                {
                                                    resultCardMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                                                }
                                                else
                                                {
                                                    resultMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                                                }

                                            }

                                        }
                                        string spocName = string.Empty;

                                        //spocName += $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n";
                                        //await innerDc.Context.SendActivityAsync(spocName);
                                        await innerDc.Context.SendActivityAsync(resultMessage);

                                        if (!string.IsNullOrEmpty(resultCardMessage))
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.Text = Constants.ShowMoreDetails;
                                            ThumbnailCard tCard = new ThumbnailCard()
                                            {
                                                Buttons = new List<CardAction>()
                                    {
                                        new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                                        new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                                     }
                                            };
                                            searchQuery.Context = Constants.ShowMoreDetails;
                                            searchQuery.CancelTripDate = null;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                        else
                                        {
                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                        }

                                    }
                                    else
                                    {
                                        await innerDc.Context.SendActivityAsync(Constants.NoScheduleInViewSchdule);
                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                    }
                                }

                            }
                        }
                        else
                        {
                            searchQuery.Context = Constants.InputDateFormatInViewSchedule;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(Constants.InputDateInViewSchdule);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                {

                    DateTime testAdhocdate;
                    //To display the message for invalid formats
                    if (!DateTime.TryParse(searchQuery.AdhocDateFormat, out testAdhocdate))
                    {
                        searchQuery.Context = Constants.InputDateFormatInViewSchedule;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        await innerDc.Context.SendActivityAsync(Constants.InputDateInViewSchdule);
                    }
                    else
                    {
                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                        dtViewSchedule = dt.ToString("MM/dd/yyyy");
                        //Display the message If input date is less that current date
                        if (dt.Date < DateTime.Now.Date)
                        {
                            searchQuery.Context = Constants.InputDateFormatInViewSchedule;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(Constants.pastDateMessageInViewSchedule);
                        }
                        else
                        {
                            sch = await objViewScheduleService.DisplayViewScheduleDetails(dtViewSchedule, searchQuery, _config);
                            searchQuery.lstViewSchedule = new List<ScheduleDetails>();
                            searchQuery.lstViewSchedule.AddRange(sch);
                            string resultMessage = string.Empty;
                            string resultCardMessage = string.Empty;
                            resultMessage += $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n\n\n";
                            var UName = string.Empty;
                            if (sch != null && sch.Count > 0)
                            {
                                for (int i = 0; i < sch.Count; i++)
                                {
                                    var SchDay0 = sch[i].Sch;
                                    SchDay0 = SchDay0.Replace("<br />", " ||   Drop : ");
                                    SchDay0 = SchDay0.Replace("NULL", "NA");
                                    var Day0 = sch[i].StartDate;
                                    UName = sch[0].UserName;
                                    //Display the schedule only for the current date
                                    if (Day0 == dt)
                                    {
                                        if (i >= 7)
                                        {
                                            resultCardMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                                        }
                                        else
                                        {
                                            resultMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                                        }

                                    }

                                }
                                string spocName = string.Empty;

                                //spocName += $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n";
                                //await innerDc.Context.SendActivityAsync(spocName);
                                await innerDc.Context.SendActivityAsync(resultMessage);

                                if (!string.IsNullOrEmpty(resultCardMessage))
                                {
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.Text = Constants.ShowMoreDetails;
                                    ThumbnailCard tCard = new ThumbnailCard()
                                    {
                                        Buttons = new List<CardAction>()
                                    {
                                        new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                                        new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                                     }
                                    };
                                    searchQuery.Context = Constants.ShowMoreDetails;
                                    searchQuery.CancelTripDate = null;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                                else
                                {
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }

                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoScheduleInViewSchdule);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }

                    }
                }
                else
                {
                    dtViewSchedule = DateTime.Now.ToString("MM/dd/yyyy");

                    sch = await objViewScheduleService.DisplayViewScheduleDetails(dtViewSchedule, searchQuery, _config);
                    searchQuery.lstViewSchedule = new List<ScheduleDetails>();
                    searchQuery.lstViewSchedule.AddRange(sch);
                    string resultMessage = string.Empty;
                    string resultCardMessage = string.Empty;
                    resultMessage+= $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n\n\n";
                    var UName = string.Empty ;
                    if (sch != null && sch.Count > 0)
                    {
                        for (int i = 0; i < sch.Count; i++)
                        {
                            var SchDay0 = sch[i].Sch;
                            SchDay0 = SchDay0.Replace("<br />", " ||   Drop : ");
                            SchDay0 = SchDay0.Replace("NULL", "NA");
                            var Day0 = sch[i].StartDate;
                            UName = sch[0].UserName;

                            if (i >= 7)
                            {
                                resultCardMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                            }
                            else
                            {
                                resultMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                            }

                        }
                        string spocName = string.Empty;

                        //spocName += $"Hey {innerDc.Context.Activity.From.Name}! Your Commute has been taken care of! Take a look at the schedule created for you.\n\n";
                        //await innerDc.Context.SendActivityAsync(spocName);
                        await innerDc.Context.SendActivityAsync(resultMessage);

                        if (!string.IsNullOrEmpty(resultCardMessage))
                        {
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.Text = Constants.ShowMoreDetails;
                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                                    {
                                        new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                                        new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                                     }
                            };
                            searchQuery.Context = Constants.ShowMoreDetails;
                            searchQuery.CancelTripDate = null;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                        else
                        {
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }

                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoScheduleInViewSchdule);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
               
            }
            else if (inputMsg.Text.Trim().ToLower().Equals("confirm " + Constants.ViewScehedule))
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
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                replyToActivity.Attachments.Add(tCard.ToAttachment());

                await innerDc.Context.SendActivityAsync(replyToActivity);


            }
            else if (inputMsg.Text.Trim().ToLower().Equals("notconfirm " + Constants.ViewScehedule))
            {
                sch = null;
                string eid = searchQuery.EnterpriseId;
                searchQuery = new UserQuery();
                searchQuery.EnterpriseId = eid; eid = null;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);

            }
            else if (inputMsg.Text.Trim().ToLower().Equals("show details in " + Constants.ViewScehedule))
            {
                string resultMessage = string.Empty;
                string resultCardMessage = string.Empty;
                string dtViewSchedule = DateTime.Now.ToString("MM/dd/yyyy");
                if (searchQuery.lstViewSchedule != null && searchQuery.lstViewSchedule.Count > 0)
                    sch.AddRange(searchQuery.lstViewSchedule);
                else
                    sch = await objViewScheduleService.DisplayViewScheduleDetails(dtViewSchedule, searchQuery, _config);
                if (sch != null && sch.Count > 0)
                {
                    for (int i = 0; i < sch.Count; i++)
                    {
                        var SchDay0 = sch[i].Sch;
                        SchDay0 = SchDay0.Replace("<br />", " ||   Drop : ");
                        SchDay0 = SchDay0.Replace("NULL", "NA");
                        var Day0 = sch[i].StartDate;

                        if (i >= 7)
                        {
                            resultCardMessage += $"{i + 1}. {Convert.ToDateTime(Day0).ToString("dd MMMM yyyy")}  ||   Pick : {SchDay0} \n\n";
                        }
                        

                    }
                    await innerDc.Context.SendActivityAsync(resultCardMessage);
                    
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                  
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(Constants.NoScheduleInViewSchdule);
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }
            }
            else if (inputMsg.Text.Trim().ToLower().Equals("dont show details in " + Constants.ViewScehedule))
            {
                await PostToUserForAnyQuery(innerDc, searchQuery);
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

                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
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
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} in view schedule";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
        }
    }
}
