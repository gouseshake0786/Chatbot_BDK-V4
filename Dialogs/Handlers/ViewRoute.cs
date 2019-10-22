using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using CognitiveService = Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Core.BotState;
using Microsoft.Bot.Schema;
using Accenture.CIO.WPBot.Core.Models;
using System.Text.RegularExpressions;
using Accenture.CIO.Bot.Common.Helpers;
using Bot.Builder.Community.Dialogs.FormFlow;
using System.Net.Http;
using System.Net.Http.Headers;
using Accenture.CIO.WPBot.Core.Services;
using Newtonsoft.Json;
using System.Net;
using System.Globalization;
//using Accenture.CIO.Bot.Common.Models;

namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class ViewRoute : ComponentDialog
    {
        List<RouteDetails> lstRteDtls = new List<RouteDetails>();

        string dteRequested = string.Empty;

        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;

        public ViewRoute(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(ViewRoute))
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
            ViewRouteService objViewRouteService = new ViewRouteService();

            string botResponseType = string.Empty;
            //bool isResponded = false;
            string reply = string.Empty;
            string inputDatefomrat = string.Empty;
            string replyMessage = string.Empty;
            var inputMsg = innerDc.Context.Activity;
            //Activity response = innerDc.Context.Activity.CreateReply();
            if (Constants.ViewrouteList.Split(',').ToList().Any(t => inputMsg.Text.Trim().ToLower().Contains(t)) &&
                    !(Constants.NotInViewrouteList.Split(',').ToList().Any(t => inputMsg.Text.Trim().ToLower().Contains(t))) && string.IsNullOrEmpty(searchQuery.CancelTripDate))
            {
                replyMessage = string.Empty;

                //To validate Date& Day Formats in raising the adhoc
                if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.NextDayOfTravelEntity) ||
                   (BotHelper.IsDateContains(inputMsg.Text, out inputDatefomrat) || !string.IsNullOrEmpty(inputDatefomrat)) || !string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                {


                    if (!string.IsNullOrEmpty(inputDatefomrat))
                    {
                        if (!string.IsNullOrEmpty(inputDatefomrat))
                        {
                            DateTime test;
                            if (!(DateTime.TryParseExact(inputDatefomrat, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                            {
                                searchQuery.Context = Constants.InputDateFormat;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                            }
                            else
                            {
                                dteRequested = inputDatefomrat;
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                    {
                        if (!Constants.AdhocDayFormat.Split(',').ToList().Any(t => searchQuery.AdhocDateFormat.ToLower().Contains(t)))
                        {
                            if (Constants.WeekDayFormats.Split(',').ToList().Any(t => searchQuery.AdhocDateFormat.ToLower().Contains(t)))
                            {
                                if (!searchQuery.AdhocDateFormat.ToLower().Contains("next"))
                                {
                                    if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(1).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(2).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(2).ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(3).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(3).ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(4).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(4).ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(5).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(5).ToString("MM/dd/yyyy");
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(6).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(6).ToString("MM/dd/yyyy");
                                    }
                                    else if(searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(6).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(7).ToString("MM/dd/yyyy");

                                    }
                                    else
                                    {
                                        DateTime test;
                                        if (!(DateTime.TryParse(searchQuery.AdhocDateFormat, out test)))
                                        {
                                            searchQuery.Context = Constants.InputDateFormat;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                            await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                                        }
                                        else
                                        {
                                            DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                            dteRequested = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                                        }
                                    }
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync($"Oh oh! It seems that there is still some time left before your route gets created.");
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }
                            }
                            //To validate for tomorrow,today,dayaftertomorrowetc..
                            else if (searchQuery.AdhocDateFormat.ToLower().Contains(Constants.TodayDayOfTravelEntity) || searchQuery.AdhocDateFormat.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || (searchQuery.AdhocDateFormat.ToLower().Contains(Constants.NextDayOfTravelEntity) && !Constants.WeekDayFormats.Split(',').ToList().Any(t => searchQuery.AdhocDateFormat.ToLower().Contains(t))))
                            {
                                var inputmsgList = innerDc.Context.Activity.Text.ToLower().Replace(".", "").Split(new Char[] { ' ' });
                                int result;
                                searchQuery.AdhocShiftDateFlag = "False";
                                //Split the inpout message to get the date from 2 days from today etc kind of scenarios
                                foreach (var x in inputmsgList)
                                {
                                    var msgValue = int.TryParse(x, out result);
                                    // To validate 2 days from tomorrow,2 days from today etc 
                                    if (msgValue == true)
                                    {
                                        if (inputMsg.Text.Trim().ToLower().Contains("from") || inputMsg.Text.Trim().ToLower().Contains("after"))
                                        {
                                            int convertIntvalue;
                                            int adhocWeekIntValue;
                                            convertIntvalue = Convert.ToInt32(x);
                                            if (inputMsg.Text.Trim().ToLower().Contains(Constants.TodayDayOfTravelEntity))
                                            {
                                                adhocWeekIntValue = 0;
                                                searchQuery.AdhocDate = DateTime.Now.AddDays(convertIntvalue + adhocWeekIntValue);
                                                searchQuery.AdhocShiftDateFlag = "True";
                                            }
                                            else if (inputMsg.Text.Trim().ToLower().Contains(Constants.TomorrowDayOfTravelEntity))
                                            {
                                                adhocWeekIntValue = 1;
                                                searchQuery.AdhocDate = DateTime.Now.AddDays(convertIntvalue + adhocWeekIntValue);
                                                searchQuery.AdhocShiftDateFlag = "True";
                                            }
                                            else if (inputMsg.Text.Trim().ToLower().Contains(Constants.WeekDayOfTravelEntity))
                                            {

                                                searchQuery.AdhocShiftDateFlag = "False";
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                    else
                                    {
                                        //To validate only today,tomorrow,next etc
                                        if (x == Constants.TodayDayOfTravelEntity || x == Constants.TomorrowDayOfTravelEntity || x == Constants.NextDayOfTravelEntity)
                                        {
                                            if (searchQuery.AdhocShiftDateFlag == "False")
                                            {

                                                if (inputMsg.Text.Trim().ToLower().Contains(Constants.TodayDayOfTravelEntity))
                                                {
                                                    searchQuery.AdhocDate = DateTime.Now;
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.DayAfterTomorrowDayOfTravelEntity))
                                                {
                                                    searchQuery.AdhocDate = DateTime.Now.AddDays(2);
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.TomorrowDayOfTravelEntity))
                                                {
                                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.NextDayOfTravelEntity))
                                                {
                                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                }

                                                else
                                                {

                                                }
                                            }

                                        }
                                    }
                                }

                                dteRequested = searchQuery.AdhocDate.ToString("MM/dd/yyyy");
                            }
                            else
                            {
                                DateTime test;
                                if (!(DateTime.TryParse(searchQuery.AdhocDateFormat, out test)))
                                {
                                    searchQuery.Context = Constants.InputDateFormat;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                                }
                                else
                                {
                                    DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                    dteRequested = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                                }
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync($"Oh oh! It seems that there is still some time left before your route gets created.");
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else
                    {

                    }
                    if (!string.IsNullOrEmpty(dteRequested))
                    {
                        DateTime date = Convert.ToDateTime(dteRequested);
                        if (!Validate4daywindow(date))
                        {
                            if (date.Date < DateTime.Now.Date)
                            {
                                searchQuery.Context = Constants.InputDateFormat;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync($"I understand you have entered a past date! Please type in a current or future date in **MM/DD/YYYY** format to View Route details!.");
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync($"Oh oh! It seems that there is still some time left before your route gets created.");
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }
                        else
                        {
                            lstRteDtls = await objViewRouteService.GetRouteDetails(dteRequested, searchQuery, _config);
                            searchQuery.lstRteDtls = new List<RouteDetails>();
                            searchQuery.lstRteDtls.AddRange(lstRteDtls);
                            if (lstRteDtls != null && lstRteDtls.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(lstRteDtls[0].FacilityId))
                                {
                                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                    //await innerDc.Context.SendActivityAsync("Please click on the date to view the route.");
                                    await innerDc.Context.SendActivityAsync("Unsure about your route? Click on a date to know the route details!");
                                    // Filter the data with cutoff time
                                    if (ViewRouteConstant.BdcCdcFacility.Split(',').ToList().Any(t => lstRteDtls[0].FacilityId.ToLower().StartsWith(t)))
                                    {
                                        if (string.IsNullOrEmpty(searchQuery.AdhocType))
                                        {
                                            // Output for Pick
                                            if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = date.ToString("dd MMMM yyyy") + " pick"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = "Pick Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                            // Output for Drop
                                            if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = date.ToString("dd MMMM yyyy") + " drop"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = "Drop Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (searchQuery.AdhocType.ToLower() == "pick"|| searchQuery.AdhocType.ToLower()=="pickup"|| searchQuery.AdhocType.ToLower()=="pick up")
                                                searchQuery.AdhocType = "Pick";
                                            if (searchQuery.AdhocType.ToLower() == "drop")
                                                searchQuery.AdhocType = "Drop";
                                            if (lstRteDtls.Where(t => t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{date.ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = $"{searchQuery.AdhocType} Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (string.IsNullOrEmpty(searchQuery.AdhocType))
                                        {
                                            // Output for Pick
                                            if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = date.ToString("dd MMMM yyyy") + " pick"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = "Pick Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                            // Output for Drop
                                            if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = date.ToString("dd MMMM yyyy") + " drop"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = "Drop Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (searchQuery.AdhocType.ToLower() == "pick"|| searchQuery.AdhocType.ToLower() == "pickup" || searchQuery.AdhocType.ToLower() == "pick up")
                                                searchQuery.AdhocType = "Pick";
                                            if (searchQuery.AdhocType.ToLower() == "drop")
                                                searchQuery.AdhocType = "Drop";

                                            if (lstRteDtls.Where(t => t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                int a = 1, i;
                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == date.Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {date.ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{date.ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                                    });
                                                    a++;
                                                }

                                                if (lstCardAction.Count > 0)
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    replyToActivity.Text = $"{searchQuery.AdhocType} Route";
                                                    tCard.Buttons = lstCardAction;
                                                    searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(ViewRouteConstant.NoRouteFound);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }
                    }


                }
                else
                {
                    lstRteDtls = await objViewRouteService.GetRouteDetails(DateTime.Today.ToString("MM/dd/yyyy"), searchQuery, _config);
                    searchQuery.lstRteDtls = new List<RouteDetails>();
                    searchQuery.lstRteDtls.AddRange(lstRteDtls);
                    if (lstRteDtls != null && lstRteDtls.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(lstRteDtls[0].FacilityId))
                        {
                            DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                            DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                            //await innerDc.Context.SendActivityAsync("Please click on the date to view the route.");
                            await innerDc.Context.SendActivityAsync("Unsure about your route? Click on a date to know the route details!");
                            // Filter the data with cutoff time
                            if (ViewRouteConstant.BdcCdcFacility.Split(',').ToList().Any(t => lstRteDtls[0].FacilityId.ToLower().StartsWith(t)))
                            {
                                if (Convert.ToInt32(istShiftDate.ToString("HHmm")) <= 0600)
                                {
                                    if (string.IsNullOrEmpty(searchQuery.AdhocType))
                                    {
                                        // Output for Pick
                                        if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                        {
                                            int a = 1, i;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.ToString("dd MMMM yyyy") + " pick"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " pick"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = "Pick Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                        // Output for Drop
                                        if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                        {
                                            int a = 1, i;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.ToString("dd MMMM yyyy") + " drop"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " drop"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = "Drop Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (searchQuery.AdhocType.ToLower() == "pick" || searchQuery.AdhocType.ToLower() == "pickup" || searchQuery.AdhocType.ToLower() == "pick up")
                                            searchQuery.AdhocType = "Pick";
                                        if (searchQuery.AdhocType.ToLower() == "drop")
                                            searchQuery.AdhocType = "Drop";
                                        if (lstRteDtls.Where(t => t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                        {
                                            int a = 1, i;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = $"{istShiftDate.ToString("dd MMMM yyyy")} + {searchQuery.AdhocType.ToLower()}"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = $"{istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}+ {searchQuery.AdhocType.ToLower()}"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = $"{searchQuery.AdhocType} Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(searchQuery.AdhocType))
                                    {
                                        // Output for Pick
                                        if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                        {
                                            int a = 1, i, j;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.ToString("dd MMMM yyyy") + " pick"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " pick"
                                                });
                                                i++;
                                            }
                                            j = i;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(2).Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{j}. {istShiftDate.AddDays(2).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(2).ToString("dd MMMM yyyy") + " pick"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = "Pick Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                        // Output for Drop
                                        if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                        {
                                            int a = 1, i, j;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.ToString("dd MMMM yyyy") + " drop"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " drop"
                                                });
                                                i++;
                                            }
                                            j = i;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(2).Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{j}. {istShiftDate.AddDays(2).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = istShiftDate.AddDays(2).ToString("dd MMMM yyyy") + " drop"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = "Drop Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (searchQuery.AdhocType.ToLower() == "pick" || searchQuery.AdhocType.ToLower() == "pickup" || searchQuery.AdhocType.ToLower() == "pick up")
                                            searchQuery.AdhocType = "Pick";
                                        if (searchQuery.AdhocType.ToLower() == "drop")
                                            searchQuery.AdhocType = "Drop";
                                        if (lstRteDtls.Where(t => t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                        {
                                            int a = 1, i, j;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = $"{istShiftDate.ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                                });
                                                a++;
                                            }
                                            i = a;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = $"{istShiftDate.AddDays(1).ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                                });
                                                i++;
                                            }
                                            j = i;
                                            if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(2).Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = $"{j}. {istShiftDate.AddDays(2).ToString("dd MMMM yyyy")}",
                                                    Type = ActionTypes.ImBack,
                                                    Value = $"{istShiftDate.AddDays(2).ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                                });
                                            }

                                            if (lstCardAction.Count > 0)
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                ThumbnailCard tCard = new ThumbnailCard();
                                                replyToActivity.Text = $"{searchQuery.AdhocType} Route";
                                                tCard.Buttons = lstCardAction;
                                                searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(searchQuery.AdhocType))
                                {
                                    // Output for Pick
                                    if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                    {
                                        int a = 1, i;
                                        List<CardAction> lstCardAction = new List<CardAction>();
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = istShiftDate.ToString("dd MMMM yyyy") + " pick"
                                            });
                                            a++;
                                        }
                                        i = a;
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypePickEntity)).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " pick"
                                            });
                                        }

                                        if (lstCardAction.Count > 0)
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            replyToActivity.Text = "Pick Route";
                                            tCard.Buttons = lstCardAction;
                                            searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                    // Output for Drop
                                    if (lstRteDtls.Where(t => t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                    {
                                        int a = 1, i;
                                        List<CardAction> lstCardAction = new List<CardAction>();
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = istShiftDate.ToString("dd MMMM yyyy") + " drop"
                                            });
                                            a++;
                                        }
                                        i = a;
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(Constants.AdhocTypeDropEntity)).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = istShiftDate.AddDays(1).ToString("dd MMMM yyyy") + " drop"
                                            });
                                        }

                                        if (lstCardAction.Count > 0)
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            replyToActivity.Text = "Drop Route";
                                            tCard.Buttons = lstCardAction;
                                            searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                }
                                else
                                {
                                    if (lstRteDtls.Where(t => t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                    {
                                        int a = 1, i;
                                        List<CardAction> lstCardAction = new List<CardAction>();
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{a}. {istShiftDate.ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{istShiftDate.ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                            });
                                            a++;
                                        }
                                        i = a;
                                        if (lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).Date == istShiftDate.AddDays(1).Date) && t.Type.Equals(searchQuery.AdhocType.ToLower())).ToList().Count > 0)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{i}. {istShiftDate.AddDays(1).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{istShiftDate.AddDays(1).ToString("dd MMMM yyyy")} {searchQuery.AdhocType.ToLower()}"
                                            });
                                        }

                                        if (lstCardAction.Count > 0)
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            replyToActivity.Text = $"{searchQuery.AdhocType} Route";
                                            tCard.Buttons = lstCardAction;
                                            searchQuery.Context = ViewRouteConstant.DateSelectionPrompt;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(ViewRouteConstant.InCompleteEmployeeData);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                            //searchQuery = new UserQuery();
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(ViewRouteConstant.NoRouteFound);

                        await PostToUserForAnyQuery(innerDc, searchQuery);

                    }
                }
            }
            else if (!string.IsNullOrEmpty(searchQuery.CancelTripDate))
            {
                if (searchQuery.lstRteDtls != null && searchQuery.lstRteDtls.Count > 0)
                    lstRteDtls.AddRange(searchQuery.lstRteDtls);
                else
                    lstRteDtls = await objViewRouteService.GetRouteDetails(DateTime.Today.ToString("MM/dd/yyyy"), searchQuery, _config);
                if (lstRteDtls != null && lstRteDtls.Count > 0)
                {
                    string tripType = inputMsg.Text.Trim().ToLower().Contains(Constants.AdhocTypePickEntity) ? Constants.AdhocTypePickEntity : Constants.AdhocTypeDropEntity;
                    var filteredRouteData = lstRteDtls.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("dd MMMM yyyy").ToLower() == searchQuery.CancelTripDate.Trim().ToLower()) && t.Type == tripType).ToList();

                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                    {
                        searchQuery.lstRteDtls = new List<RouteDetails>();
                        searchQuery.lstRteDtls.AddRange(filteredRouteData);
                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();

                        replyToActivity.Attachments = new List<Attachment>();

                        Activity replyToActivityMessage = innerDc.Context.Activity.CreateReply();
                        replyToActivityMessage.Attachments = new List<Attachment>();
                        replyToActivityMessage.Text += $"Your route for **{tripType}** on **{Convert.ToDateTime(searchQuery.CancelTripDate).ToString("dd MMMM yyyy")}** \n\n";
                        foreach (var item in filteredRouteData)
                        {
                            replyToActivityMessage.Text += $"**Route Id**:{item.Routeid} \n\n";
                            replyToActivityMessage.Text += $"**Shift**:{item.Shift} \n\n";
                            replyToActivityMessage.Text += $"**TripType**:{item.Type} \n\n";
                            replyToActivityMessage.Text += $"**Vehicle Platform**:{item.PlatForm} \n\n";
                            replyToActivityMessage.Text += $"\n\n";
                            
                            //await innerDc.Context.SendActivityAsync($"**Route No**:Route{item.RouteSeq}\n **Route Id**:{item.Routeid}\n **Shift**:{item.Shift}\n **TripType**:{item.Type}\n **Platform**:{item.PlatForm}");
                        }
                        await innerDc.Context.SendActivityAsync(replyToActivityMessage);

                        await PostToUserForAnyQuery(innerDc, searchQuery);
                        // Aks for co-passengers
                        //replyToActivity.Text = ViewRouteConstant.PromptForCoPassenger;
                        //ThumbnailCard tCard = new ThumbnailCard()
                        //{
                        //    Buttons = new List<CardAction>()
                        //            {
                        //                new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                        //                new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                        //            }
                        //};

                        //searchQuery.Context = ViewRouteConstant.PromptForCoPassenger;
                        //searchQuery.CancelTripDate = null;
                        //await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        //await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        //replyToActivity.Attachments.Add(tCard.ToAttachment());

                        //await innerDc.Context.SendActivityAsync(replyToActivity);
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(ViewRouteConstant.NoRouteFound);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(ViewRouteConstant.NoRouteFound);

                    await PostToUserForAnyQuery(innerDc, searchQuery);

                }
            }
            else if (new List<string>() { ViewRouteConstant.AnythingElseInViewRoute, Constants.Yes, Constants.No }.Any(t => inputMsg.Text.Trim().ToLower().Contains(t)))
            {
                if (inputMsg.Text.Trim().ToLower().Contains(Constants.Yes.ToLower()))
                {
                    //await innerDc.Context.SendActivityAsync(Constants.ResultMessageConfirm);
                    searchQuery.Context = null;
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
                else
                {
                    lstRteDtls = null;
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);
                }

                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
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

        public bool Validate4daywindow(DateTime shiftdate)
        {
            bool valid = false;
            DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
            DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
            if ((shiftdate.Date >= istShiftDate.Date) && (shiftdate.Date <= istShiftDate.AddDays(3).Date))
            {
                valid = true;
                return valid;
            }
            else
            {
                return valid;
            }
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
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} in view route";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
        }
    }
}
