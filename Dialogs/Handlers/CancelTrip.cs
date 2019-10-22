using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.Bot.Common.Helpers;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Core.BotState;
using Accenture.CIO.WPBot.Core.Models;
using Accenture.CIO.WPBot.Core.Services;
using Accenture.CIO.WPBot.Logger;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class CancelTrip : ComponentDialog
    {
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;
        List<RouteInfo> lstRouteDetails = new List<RouteInfo>();

        public CancelTrip(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(CancelTrip))
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
            CancelTripService objCancelTripService = new CancelTripService();
            string botResponseType = string.Empty;
            string dteRequested = string.Empty;
            string inputDatefomrat = string.Empty;
            string reply = string.Empty;
            var activity = innerDc.Context.Activity;
            var inputMsg = innerDc.Context.Activity;
            // Check and popup the response for normal cancel trip query
            if (CancelTripConstant.CancelTripList.Split(',').ToList().Any(t => activity.Text.ToLower().Contains(t)) && string.IsNullOrEmpty(userQuery.CancelTripDate) && !activity.Text.ToLower().Contains(Constants.Confirm))
            {
                if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.NextDayOfTravelEntity) ||
                   (BotHelper.IsDateContains(inputMsg.Text, out inputDatefomrat) || !string.IsNullOrEmpty(inputDatefomrat)) || !string.IsNullOrEmpty(userQuery.AdhocDateFormat))
                {
                    if (!string.IsNullOrEmpty(inputDatefomrat))
                    {
                        if (!string.IsNullOrEmpty(inputDatefomrat))
                        {
                            DateTime test;
                            if (!(DateTime.TryParseExact(inputDatefomrat, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                            {
                                userQuery.Context = Constants.InputDateFormatInCancelTrop;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                            }
                            else
                            {
                                dteRequested = inputDatefomrat;
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(userQuery.AdhocDateFormat))
                    {
                        if (!Constants.AdhocDayFormat.Split(',').ToList().Any(t => userQuery.AdhocDateFormat.ToLower().Contains(t)))
                        {
                            if (Constants.WeekDayFormats.Split(',').ToList().Any(t => userQuery.AdhocDateFormat.ToLower().Contains(t)))
                            {
                                if (!userQuery.AdhocDateFormat.ToLower().Contains("next"))
                                {
                                    if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(1).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(2).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(2).ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(3).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(3).ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(4).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(4).ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(5).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(5).ToString("MM/dd/yyyy");
                                    }
                                    else if (userQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(6).DayOfWeek.ToString().ToLower())
                                    {
                                        dteRequested = DateTime.Now.AddDays(6).ToString("MM/dd/yyyy");
                                    }
                                    else
                                    {
                                        dteRequested = DateTime.Now.AddDays(7).ToString("MM/dd/yyyy");

                                    }
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync($"Sorry! It seems that there is still some time left before your route gets created. You can cancel the route once it gets created.");
                                    await PostToUserForAnyQuery(innerDc, userQuery);
                                }
                            }
                            //To validate for tomorrow,today,dayaftertomorrowetc..
                            else if (userQuery.AdhocDateFormat.ToLower().Contains(Constants.TodayDayOfTravelEntity) || userQuery.AdhocDateFormat.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || (userQuery.AdhocDateFormat.ToLower().Contains(Constants.NextDayOfTravelEntity) && !Constants.WeekDayFormats.Split(',').ToList().Any(t => userQuery.AdhocDateFormat.ToLower().Contains(t))))
                            {
                                var inputmsgList = innerDc.Context.Activity.Text.ToLower().Replace(".", "").Split(new Char[] { ' ' });
                                int result;
                                userQuery.AdhocShiftDateFlag = "False";
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
                                                userQuery.AdhocDate = DateTime.Now.AddDays(convertIntvalue + adhocWeekIntValue);
                                                userQuery.AdhocShiftDateFlag = "True";
                                            }
                                            else if (inputMsg.Text.Trim().ToLower().Contains(Constants.TomorrowDayOfTravelEntity))
                                            {
                                                adhocWeekIntValue = 1;
                                                userQuery.AdhocDate = DateTime.Now.AddDays(convertIntvalue + adhocWeekIntValue);
                                                userQuery.AdhocShiftDateFlag = "True";
                                            }
                                            else if (inputMsg.Text.Trim().ToLower().Contains(Constants.WeekDayOfTravelEntity))
                                            {

                                                userQuery.AdhocShiftDateFlag = "False";
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
                                            if (userQuery.AdhocShiftDateFlag == "False")
                                            {

                                                if (inputMsg.Text.Trim().ToLower().Contains(Constants.TodayDayOfTravelEntity))
                                                {
                                                    userQuery.AdhocDate = DateTime.Now;
                                                    userQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.DayAfterTomorrowDayOfTravelEntity))
                                                {
                                                    userQuery.AdhocDate = DateTime.Now.AddDays(2);
                                                    userQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.TomorrowDayOfTravelEntity))
                                                {
                                                    userQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                    userQuery.AdhocShiftDateFlag = "True";
                                                }
                                                else if (inputMsg.Text.Trim().ToLower().Contains(Constants.NextDayOfTravelEntity))
                                                {
                                                    userQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                    userQuery.AdhocShiftDateFlag = "True";
                                                }

                                                else
                                                {

                                                }
                                            }

                                        }
                                    }
                                }

                                dteRequested = userQuery.AdhocDate.ToString("MM/dd/yyyy");
                            }
                            else
                            {
                                DateTime test;
                                if (!(DateTime.TryParse(userQuery.AdhocDateFormat, out test)))
                                {
                                    userQuery.Context = Constants.InputDateFormat;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                                }
                                else
                                {
                                    DateTime dt = Convert.ToDateTime(userQuery.AdhocDateFormat);
                                    dteRequested = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                                }
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync($"Sorry! It seems that there is still some time left before your route gets created. You can cancel the route once it gets created.");
                            await PostToUserForAnyQuery(innerDc, userQuery);
                        }
                    }

                    if (!string.IsNullOrEmpty(dteRequested))
                    {
                        DateTime date = Convert.ToDateTime(dteRequested);
                        if (date.Date < DateTime.Now.Date)
                        {
                            userQuery.Context = Constants.InputDateFormatInCancelTrop;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync($"I understand you have entered a past date! Please type in a current or future date in **MM/DD/YYYY** format to View Route details!.");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(userQuery.AdhocType))
                            {
                                lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(CancelTripConstant.AdhocTypeBoth, userQuery, _config);
                                lstRouteDetails = lstRouteDetails.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dteRequested.ToLower())).ToList();
                                //(Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dteRequested.ToLower())
                                userQuery.lstRouteDetails = new List<RouteInfo>();
                                userQuery.lstRouteDetails.AddRange(lstRouteDetails);

                                if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                                {
                                    await innerDc.Context.SendActivityAsync(CancelTripConstant.UpcomingTripsMsg);

                                    // if pick routes exists
                                    if (lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Pick)).ToList().Count > 0)
                                    {
                                        int a = 1;
                                        var lstPickDetails = lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Pick)).ToList();
                                        List<CardAction> lstCardAction = new List<CardAction>();
                                        foreach (var item in lstPickDetails)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{a}. {item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                            });
                                            a++;
                                        }

                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        ThumbnailCard tCard = new ThumbnailCard();
                                        replyToActivity.Text = "Pick Route";
                                        //tCard.Text = "Pick";
                                        tCard.Buttons = lstCardAction;
                                        userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                        lstPickDetails = null;
                                    }
                                    // if drop routes exists
                                    if (lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Drop)).ToList().Count > 0)
                                    {
                                        int a = 1;
                                        var lstDropDetails = lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Drop)).ToList();
                                        List<CardAction> lstCardAction = new List<CardAction>();
                                        foreach (var item in lstDropDetails)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = $"{a}. {item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                            });
                                            a++;
                                        }

                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        ThumbnailCard tCard = new ThumbnailCard();
                                        replyToActivity.Text = "Drop Route";
                                        //tCard.Text = "Drop";
                                        tCard.Buttons = lstCardAction;
                                        userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                        lstDropDetails = null;
                                    }
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                                    await PostToUserForAnyQuery(innerDc, userQuery);
                                }
                            }
                            else
                            {
                                lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(userQuery.AdhocType, userQuery, _config);
                                lstRouteDetails = lstRouteDetails.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dteRequested.ToLower())).ToList();
                                userQuery.lstRouteDetails = new List<RouteInfo>();
                                userQuery.lstRouteDetails.AddRange(lstRouteDetails);

                                if (userQuery.AdhocType == "pick")
                                    userQuery.AdhocType = "Pick";
                                if (userQuery.AdhocType == "drop")
                                    userQuery.AdhocType = "Drop";

                                if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                                {
                                    List<CardAction> lstCardAction = new List<CardAction>();
                                    foreach (var item in lstRouteDetails)
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                            Type = ActionTypes.ImBack,
                                            Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                        });
                                    }

                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    replyToActivity.Text = $"{userQuery.AdhocType} Route";
                                    tCard.Buttons = lstCardAction;
                                    userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                                    await PostToUserForAnyQuery(innerDc, userQuery);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // No AdhocType mentioned in cancellation query
                    if (!(CancelTripConstant.PickTypeAdhoc.Split(',').ToList().Any(t => activity.Text.ToLower().Contains(t)) || CancelTripConstant.DropTypeAdhoc.Split(',').ToList().Any(t => activity.Text.ToLower().Contains(t))))
                    {
                        lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(CancelTripConstant.AdhocTypeBoth, userQuery, _config);
                        userQuery.lstRouteDetails = new List<RouteInfo>();
                        userQuery.lstRouteDetails.AddRange(lstRouteDetails);

                        if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                        {
                            await innerDc.Context.SendActivityAsync(CancelTripConstant.UpcomingTripsMsg);

                            // if pick routes exists
                            if (lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Pick)).ToList().Count > 0)
                            {
                                int a = 1;
                                var lstPickDetails = lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Pick)).ToList();
                                List<CardAction> lstCardAction = new List<CardAction>();
                                foreach (var item in lstPickDetails)
                                {
                                    lstCardAction.Add(new CardAction()
                                    {
                                        Title = $"{a}. {item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                        Type = ActionTypes.ImBack,
                                        Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                    });
                                    a++;
                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                replyToActivity.Text = "Pick Route";
                                //tCard.Text = "Pick";
                                tCard.Buttons = lstCardAction;
                                userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                lstPickDetails = null;
                            }
                            // if drop routes exists
                            if (lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Drop)).ToList().Count > 0)
                            {
                                int a = 1;
                                var lstDropDetails = lstRouteDetails.Where(t => t.Action.ToLower().Equals(CancelTripConstant.Drop)).ToList();
                                List<CardAction> lstCardAction = new List<CardAction>();
                                foreach (var item in lstDropDetails)
                                {
                                    lstCardAction.Add(new CardAction()
                                    {
                                        Title = $"{a}. {item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                        Type = ActionTypes.ImBack,
                                        Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                    });
                                    a++;
                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                replyToActivity.Text = "Drop Route";
                                //tCard.Text = "Drop";
                                tCard.Buttons = lstCardAction;
                                userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                lstDropDetails = null;
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                            await PostToUserForAnyQuery(innerDc, userQuery);
                        }
                    }
                    // AdhocType mentioned in cancellation query
                    else
                    {
                        var tripType = CancelTripConstant.PickTypeAdhoc.Split(',').ToList().Any(t => activity.Text.ToLower().Contains(t)) ? CancelTripConstant.Pick : CancelTripConstant.Drop;
                        lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(tripType, userQuery, _config);
                        userQuery.lstRouteDetails = new List<RouteInfo>();
                        userQuery.lstRouteDetails.AddRange(lstRouteDetails);

                        if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                        {
                            List<CardAction> lstCardAction = new List<CardAction>();
                            foreach (var item in lstRouteDetails)
                            {
                                lstCardAction.Add(new CardAction()
                                {
                                    Title = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                    Type = ActionTypes.ImBack,
                                    Value = $"{item.Shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}"
                                });
                            }

                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            replyToActivity.Text = tripType.Equals(CancelTripConstant.Pick) ? "Pick Route" : "Drop Route";
                            tCard.Buttons = lstCardAction;
                            userQuery.Context = CancelTripConstant.UpcomingTripsMsg;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                            await PostToUserForAnyQuery(innerDc, userQuery);
                        }
                    }
                }

            }
            // If route has been selected by user
            else if (!string.IsNullOrEmpty(userQuery.CancelTripDate))
            {
                if (userQuery.lstRouteDetails != null && userQuery.lstRouteDetails.Count > 0)
                    lstRouteDetails.AddRange(userQuery.lstRouteDetails);
                else
                    lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(CancelTripConstant.AdhocTypeBoth, userQuery, _config);
                if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                {
                    var inputText = userQuery.CancelTripDate.ToArray();
                    inputText[userQuery.CancelTripDate.IndexOf(" ")] = ':';
                    userQuery.CancelTripDate = String.Concat(inputText).Insert(5, ",");
                    string shift = userQuery.CancelTripDate.Substring(0, userQuery.CancelTripDate.IndexOf(',')).Replace(" ", "").Replace(":", "");
                    string shiftDate = userQuery.CancelTripDate.Substring((userQuery.CancelTripDate.IndexOf(',') + 1)).TrimStart();
                    string routeId = lstRouteDetails.Where(t => t.Shift == shift && Convert.ToDateTime(t.ShiftDate).ToString("dd MMMM yyyy").ToLower() == shiftDate.ToLower()).Select(r => r.RouteId).FirstOrDefault();
                    string isValidRoute = await objCancelTripService.IsValidRouteIdToBeCacncelled(routeId, userQuery, _config);
                    if (!string.IsNullOrEmpty(isValidRoute))
                    {
                        // Invalid Route Deteced
                        if (isValidRoute.Equals("-2"))
                        {
                            string resultMessage = string.Empty;
                            resultMessage += $"{CancelTripConstant.TripShitPrinted}\n\n";
                            resultMessage += $"{Constants.ContactTransportDesk}";

                            await innerDc.Context.SendActivityAsync(resultMessage);
                            userQuery.lstRouteDetails = null;
                            userQuery.CancelTripDate = null;
                            await PostToUserForAnyQuery(innerDc, userQuery);
                        }
                        // If valid route to be cancelled
                        else if (isValidRoute.Equals("1"))
                        {
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.Text = CancelTripConstant.ConfirmCancelRoute;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                            {
                                new CardAction() {Title=Constants.Yes,Type = ActionTypes.ImBack,Value = Constants.Yes },
                                new CardAction() {Title=Constants.No,Type = ActionTypes.ImBack,Value = Constants.No }
                            }
                            };
                            userQuery.Context = $"{Constants.ConfirmCancelRoute}:{routeId}";
                            userQuery.CancelTripDate = null;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync($"Sorry Error in Route Validation. Please Restart the conversation");
                        await PostToUserForAnyQuery(innerDc, userQuery);
                    }
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                    await PostToUserForAnyQuery(innerDc, userQuery);
                }
            }
            else if ((activity.Text.ToLower().Contains(Constants.Confirm) || activity.Text.ToLower().Contains(Constants.NotConfirm)) && activity.Text.ToLower().Contains("delete"))
            {
                if (activity.Text.ToLower().Contains(Constants.Confirm) && !activity.Text.ToLower().Contains(Constants.NotConfirm))
                {
                    string routeId = activity.Text.Substring((activity.Text.IndexOf(':') + 1)).TrimStart().TrimEnd();
                    if (userQuery.lstRouteDetails != null && userQuery.lstRouteDetails.Count > 0)
                        lstRouteDetails.AddRange(userQuery.lstRouteDetails);
                    else
                        lstRouteDetails = await objCancelTripService.GetEmployeeRouteDataForCancel(CancelTripConstant.AdhocTypeBoth, userQuery, _config);
                    if (lstRouteDetails != null && lstRouteDetails.Count > 0)
                    {
                        string shift = lstRouteDetails.Where(t => t.RouteId == routeId).Select(t => t.Shift).FirstOrDefault().Insert(2, ":");
                        string tripType = lstRouteDetails.Where(t => t.RouteId == routeId).Select(t => t.Action).FirstOrDefault();
                        DateTime? shiftDate = lstRouteDetails.Where(t => t.RouteId == routeId).Select(t => t.ShiftDate).FirstOrDefault();

                        string cancelResult = await objCancelTripService.CancelEmployeeTrip(routeId, userQuery, _config);
                        if (!string.IsNullOrEmpty(cancelResult))
                        {
                            if (cancelResult.Equals("1"))
                            {
                                await innerDc.Context.SendActivityAsync($"The {tripType} for {shift}, {Convert.ToDateTime(shiftDate).ToString("dd MMMM yyyy")} has been cancelled");
                                shift = tripType = null;
                                shiftDate = null;
                                userQuery.lstRouteDetails = null;
                                await PostToUserForAnyQuery(innerDc, userQuery);
                            }
                            else if (cancelResult.Equals("-2"))
                            {
                                string resultMessage = string.Empty;
                                resultMessage += $"{CancelTripConstant.TripShitPrinted}\n\n";
                                resultMessage += $"{Constants.ContactTransportDesk}";

                                await innerDc.Context.SendActivityAsync(resultMessage);
                                userQuery.CancelTripDate = null;
                                userQuery.lstRouteDetails = null;
                                await PostToUserForAnyQuery(innerDc, userQuery);
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync($"Sorry Error in Route Validation. Please Restart the conversation");
                            await PostToUserForAnyQuery(innerDc, userQuery);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(CancelTripConstant.NoRouteInCancelTrip);
                        await PostToUserForAnyQuery(innerDc, userQuery);
                    }
                }
                else
                    await PostToUserForAnyQuery(innerDc, userQuery);
            }
            else if ((activity.Text.ToLower().Contains(Constants.Confirm) || activity.Text.ToLower().Contains(Constants.NotConfirm)) && !activity.Text.ToLower().Contains("delete"))
            {
                if (activity.Text.ToLower().Contains(Constants.NotConfirm))
                {
                    lstRouteDetails = null;
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
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} in cancel trip";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
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
    }
}

