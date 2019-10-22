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
    public class AdhocStatus : ComponentDialog
    {
        List<AdhocRequestStatus> lstAdhocRequestStatus = new List<AdhocRequestStatus>();
        static string PickStatus = "";
        static string DropStatus = "";
        static string InterofficeStatus = "";
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;

        public AdhocStatus(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(AdhocStatus))
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

            AdhocStatusService objAdhocStatusService = new AdhocStatusService();
            //searchQuery.Context = null;
            var resultMessage = innerDc.Context.Activity;
            var inputMsg = innerDc.Context.Activity;
            //Activity response = innerDc.Context.Activity.CreateReply();
            //var inputMsg = innerDc.Context.Activity;
            string botResponseType = string.Empty;
            string reply = string.Empty;
            bool isResponded = false;
            string inputDate = string.Empty;

            #region AdhocDay Is not null
            if (!string.IsNullOrEmpty(searchQuery.AdhocDay))
            {
                string dtDaySelect;
                string shift = "";
                string resultFormat = string.Empty;
                if (searchQuery.AdhocDay.ToLower().Contains(Constants.day1.ToLower()))
                {
                    dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");

                    string tripType = string.Empty;
                    if (inputMsg.Text.Trim().ToLower().Contains("pick"))
                    {
                        tripType = "Pick";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("drop"))
                    {
                        tripType = "Drop";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("interoffice"))
                    {
                        tripType = "InterOffice";
                        shift = searchQuery.AdhocDay.Substring(16);
                    }

                    if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                        lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                    else
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                    if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(shift))
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower()) && t.TripType == tripType && t.shift == shift).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                        else
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                    }

                                }

                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else if (searchQuery.AdhocDay.ToLower().Contains(Constants.day2.ToLower()))
                {
                    dtDaySelect = Convert.ToDateTime(DateTime.Today.AddDays(1).ToShortDateString()).ToString("MM/dd/yyyy");
                    string tripType = string.Empty;
                    if (inputMsg.Text.Trim().ToLower().Contains("pick"))
                    {
                        tripType = "Pick";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("drop"))
                    {
                        tripType = "Drop";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("interoffice"))
                    {
                        tripType = "InterOffice";
                        shift = searchQuery.AdhocDay.Substring(16);
                    }

                    if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                        lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                    else
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                    if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(shift))
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower()) && t.TripType == tripType && t.shift == shift).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                        else
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }

                                    }
                                }

                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }

                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else if (searchQuery.AdhocDay.ToLower().Contains(Constants.day3.ToLower()))
                {
                    dtDaySelect = Convert.ToDateTime(DateTime.Today.AddDays(2).ToShortDateString()).ToString("MM/dd/yyyy");
                    string tripType = string.Empty;
                    if (inputMsg.Text.Trim().ToLower().Contains("pick"))
                    {
                        tripType = "Pick";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("drop"))
                    {
                        tripType = "Drop";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("interoffice"))
                    {
                        tripType = "InterOffice";
                        shift = searchQuery.AdhocDay.Substring(16);
                    }

                    if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                        lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                    else
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                    if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(shift))
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower()) && t.TripType == tripType && t.shift == shift).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                        else
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                    }

                                }

                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else if (searchQuery.AdhocDay.ToLower().Contains(Constants.day4.ToLower()))
                {
                    dtDaySelect = Convert.ToDateTime(DateTime.Today.AddDays(3).ToShortDateString()).ToString("MM/dd/yyyy");
                    string tripType = string.Empty;
                    if (inputMsg.Text.Trim().ToLower().Contains("pick"))
                    {
                        tripType = "Pick";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("drop"))
                    {
                        tripType = "Drop";
                        shift = searchQuery.AdhocDay.Substring(9);
                    }
                    else if (inputMsg.Text.Trim().ToLower().Contains("interoffice"))
                    {
                        tripType = "InterOffice";
                        shift = searchQuery.AdhocDay.Substring(16);
                    }

                    if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                        lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                    else
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                    if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(shift))
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower()) && t.TripType == tripType && t.shift == shift).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                        else
                                        {
                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                        }
                                    }

                                }

                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }

                }
            }
            #endregion
            #region End Block
            else if (inputMsg.Text.Trim().ToLower().Equals("confirm " + Constants.AdhocStatus))
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
            else if (inputMsg.Text.Trim().ToLower().Equals("notconfirm " + Constants.AdhocStatus))
            {
                lstAdhocRequestStatus = null;
                string eid = searchQuery.EnterpriseId;
                searchQuery = new UserQuery();
                searchQuery.EnterpriseId = eid; eid = null;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);

            }
            #endregion
            else if (Constants.AdhocStatusFormat.Split(',').ToList().Any(t => innerDc.Context.Activity.Text.ToLower().Contains(t)) && string.IsNullOrEmpty(searchQuery.StatusDate))
            {
                string dtDaySelect = "";
                string resultFormat = string.Empty;
                string resultday = string.Empty;
                if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.TomorrowDayOfTravelEntity))
                {
                    resultday = innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) ? Constants.TodayDayOfTravelEntity : Constants.TomorrowDayOfTravelEntity;
                    dtDaySelect = (resultday.Equals(Constants.TodayDayOfTravelEntity)) ? DateTime.Now.Date.ToString("MM/dd/yyyy")
                                                : DateTime.Now.AddDays(1).Date.ToString("MM/dd/yyyy");
                    if (inputMsg.Text.Trim().ToLower().Contains("pick") || inputMsg.Text.Trim().ToLower().Contains("drop") || inputMsg.Text.Trim().ToLower().Contains("interoffice") || inputMsg.Text.Trim().ToLower().Contains("inter office"))
                    {
                        string tripType = string.Empty;
                        if (inputMsg.Text.Trim().ToLower().Contains("pick"))
                        {
                            tripType = "Pick";
                        }
                        else if (inputMsg.Text.Trim().ToLower().Contains("drop"))
                        {
                            tripType = "Drop";
                        }
                        else
                        {
                            tripType = "InterOffice";
                        }

                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                        searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                        searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);
                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower()) && t.TripType == tripType).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                    }

                                }
                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);

                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);

                        }
                    }
                    else
                    {
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                        searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                        searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);
                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                        {
                            var filteredRouteData = lstAdhocRequestStatus.Where(t => (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                            {
                                //await innerDc.Context.SendActivityAsync($"The status of Adhoc request for {dtDaySelect} is as below.");
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(filteredRouteData);
                                foreach (var obj in filteredRouteData)
                                {
                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                    if (obj.AdhocStatus.Equals("Approved"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Rejected"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                    }
                                    if (obj.AdhocStatus.Equals("Pending"))
                                    {
                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                    }

                                }

                                await innerDc.Context.SendActivityAsync(resultFormat);
                                searchQuery.AdhocDay = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                await PostToUserForAnyQuery(innerDc, searchQuery);

                            }
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);

                        }
                    }

                }
                else if (!string.IsNullOrEmpty(searchQuery.AdhocType))
                {
                    if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice.ToLower()))
                    {
                        if (BotHelper.IsDateContains(inputMsg.Text, out inputDate))
                        {
                            if (!string.IsNullOrEmpty(inputDate))
                            {
                                string dateRequired = string.Empty;

                                DateTime test;
                                DateTime testdate;
                                if (DateTime.TryParse(inputDate, out testdate))
                                {
                                    DateTime dateInput = Convert.ToDateTime(inputDate);
                                    CultureInfo invC = CultureInfo.InvariantCulture;
                                    var ConvertedDate = dateInput.ToString("d", invC);
                                    if (!(DateTime.TryParseExact(ConvertedDate, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                                    {
                                        searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                                    }
                                    else
                                    {
                                        if (!Validate4daywindow(dateInput))
                                        {
                                            searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            await innerDc.Context.SendActivityAsync(Constants.futureDateMessageInViewSchedule);
                                        }
                                        else
                                        {
                                            DateTime dt = Convert.ToDateTime(dateInput);
                                            dtDaySelect = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                                            if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                                            {

                                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                                else
                                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                                {
                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                                    for (int i = 0; i < 5; i++)
                                                    {
                                                        lstPickDetails.Remove(lstPickDetails[0]);
                                                    }
                                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                                    searchQuery.cardTitle = "True";

                                                    int s = searchQuery.AdhocStatusFlag;
                                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                                    foreach (var item in lstPickDetails)
                                                    {

                                                        if (lstNextCardAction.Count < 5)
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                            });
                                                            PickStatus = "";
                                                        }
                                                        else
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More Pick Details"
                                                            });
                                                            PickStatus = "Pick";
                                                            break;
                                                        }

                                                        s++;

                                                    }
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Pick";
                                                    //replyToActivity.Text = "Pick";
                                                    tCard.Buttons = lstNextCardAction;
                                                    //searchQuery.cardTitle = cardTitle.Pick;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                                            {

                                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                                else
                                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                                {
                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                                    for (int i = 0; i < 5; i++)
                                                    {
                                                        lstPickDetails.Remove(lstPickDetails[0]);
                                                    }
                                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                                    searchQuery.cardTitle = "True";

                                                    int s = searchQuery.AdhocStatusFlag;
                                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                                    foreach (var item in lstPickDetails)
                                                    {

                                                        if (lstNextCardAction.Count < 5)
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                            });
                                                            DropStatus = "";
                                                        }
                                                        else
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More Drop Details"
                                                            });
                                                            DropStatus = "Drop";
                                                            break;
                                                        }

                                                        s++;

                                                    }
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Drop";
                                                    replyToActivity.Text = "Drop";
                                                    tCard.Buttons = lstNextCardAction;
                                                    //searchQuery.cardTitle = cardTitle.Drop;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                                }
                                            }
                                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                                            {

                                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                                else
                                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                                {
                                                    var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                                    for (int i = 0; i < 5; i++)
                                                    {
                                                        lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                                                    }
                                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                                    searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                                    searchQuery.cardTitle = "True";

                                                    int s = searchQuery.AdhocStatusFlag;
                                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                                    foreach (var item in lstInterOfficeDetails)
                                                    {

                                                        if (lstNextCardAction.Count < 5)
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                            });
                                                            InterofficeStatus = "";
                                                        }
                                                        else
                                                        {
                                                            lstNextCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More InterOffice Details"
                                                            });
                                                            InterofficeStatus = "InterOffice";
                                                            break;
                                                        }

                                                        s++;

                                                    }
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Inter-Office";
                                                    replyToActivity.Text = "Inter-Office";
                                                    tCard.Buttons = lstNextCardAction;
                                                    //searchQuery.cardTitle = cardTitle.InterOffice;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                            }
                                            else
                                            {
                                                await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                                                lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                                searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);
                                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                                                {
                                                    string tripType = string.Empty;
                                                    if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()))
                                                    {
                                                        tripType = "Pick";
                                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                        {
                                                            //Output for Pick
                                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                                            {
                                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                                {
                                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                                    {
                                                                        foreach (var obj in filteredRouteData)
                                                                        {
                                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                                            {
                                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                                else
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                            }

                                                                        }
                                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                                        searchQuery.AdhocDay = null;
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                                    {
                                                                        int a = 1;
                                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                                        foreach (var item in lstPickDetails)
                                                                        {
                                                                            if (a > 5)
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = "More",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = "More Pick Details"
                                                                                });
                                                                                PickStatus = "Pick";
                                                                                break;
                                                                            }
                                                                            else
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                                                });
                                                                                a++;
                                                                            }
                                                                        }

                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                        replyToActivity.Attachments = new List<Attachment>();
                                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                                        //tCard.Text = "Pick";
                                                                        replyToActivity.Text = "Pick";
                                                                        tCard.Buttons = lstCardAction;
                                                                        //searchQuery.cardTitle = cardTitle.Pick;
                                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }

                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                                        }
                                                    }
                                                    else if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()))
                                                    {
                                                        tripType = "Drop";
                                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                        {
                                                            // Output for Drop
                                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                                            {
                                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                                {
                                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                                    {
                                                                        foreach (var obj in filteredRouteData)
                                                                        {
                                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                                            {
                                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                                else
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                            }

                                                                        }
                                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                                        searchQuery.AdhocDay = null;
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                                    {
                                                                        int a = 1;
                                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                                        foreach (var item in lstPickDetails)
                                                                        {
                                                                            if (a > 5)
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = "More",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = "More Drop Details"
                                                                                });
                                                                                DropStatus = "Drop";
                                                                                break;
                                                                            }
                                                                            else
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                                                });
                                                                                a++;
                                                                            }
                                                                        }

                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                        replyToActivity.Attachments = new List<Attachment>();
                                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                                        //tCard.Text = "Drop";
                                                                        replyToActivity.Text = "Drop";
                                                                        tCard.Buttons = lstCardAction;
                                                                        //searchQuery.cardTitle = cardTitle.Drop;
                                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                                        }
                                                    }
                                                    else
                                                    {
                                                        tripType = "InterOffice";
                                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                        {
                                                            // Output for InterOffice
                                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                                            {
                                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                                {
                                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                                    {
                                                                        foreach (var obj in filteredRouteData)
                                                                        {
                                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                                            {
                                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                            }
                                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                                            {
                                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                                else
                                                                                {
                                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                                }
                                                                            }

                                                                        }
                                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                                        searchQuery.AdhocDay = null;
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                                    {
                                                                        int a = 1;
                                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                                        foreach (var item in lstPickDetails)
                                                                        {
                                                                            if (a > 5)
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = "More",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = "More Interoffice Details"
                                                                                });
                                                                                InterofficeStatus = "InterOffice";
                                                                                break;
                                                                            }
                                                                            else
                                                                            {
                                                                                lstCardAction.Add(new CardAction()
                                                                                {
                                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                                    Type = ActionTypes.ImBack,
                                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                                                });
                                                                                a++;
                                                                            }
                                                                        }

                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                        replyToActivity.Attachments = new List<Attachment>();
                                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                                        //tCard.Text = "Inter-Office";
                                                                        replyToActivity.Text = "Inter-Office";
                                                                        tCard.Buttons = lstCardAction;
                                                                        //searchQuery.cardTitle = cardTitle.InterOffice;
                                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                    }
                                                                    else
                                                                    {
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                                    }
                                                                }


                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                                        }
                                                    }

                                                }
                                                else
                                                {
                                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                                }
                                            }

                                        }

                                    }
                                }
                                else
                                {
                                    searchQuery.Context = Constants.InputDateMsgInAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                        {
                            DateTime testAdhocdate;
                            //To display the message for invalid formats
                            if (!DateTime.TryParse(searchQuery.AdhocDateFormat, out testAdhocdate))
                            {
                                searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                            }
                            else
                            {
                                DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                if (!Validate4daywindow(dt))
                                {
                                    searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(Constants.futureDateMessageInViewSchedule);
                                }
                                else
                                {
                                    dtDaySelect = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");

                                    if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                        {
                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstPickDetails.Remove(lstPickDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {

                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                    });
                                                    PickStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Pick Details"
                                                    });
                                                    PickStatus = "Pick";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Pick";
                                            //replyToActivity.Text = "Pick";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.Pick;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                        {
                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstPickDetails.Remove(lstPickDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {

                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                    });
                                                    DropStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Drop Details"
                                                    });
                                                    DropStatus = "Drop";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Drop";
                                            replyToActivity.Text = "Drop";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.Drop;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);

                                        }
                                    }
                                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                        {
                                            var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstInterOfficeDetails)
                                            {
                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                    });
                                                    InterofficeStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More InterOffice Details"
                                                    });
                                                    InterofficeStatus = "InterOffice";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Inter-Office";
                                            replyToActivity.Text = "Inter-Office";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.InterOffice;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                    else
                                    {
                                        await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                        searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);
                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                                        {
                                            string tripType = string.Empty;
                                            if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()))
                                            {
                                                tripType = "Pick";
                                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                {
                                                    //Output for Pick
                                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                                    {
                                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                        {
                                                            var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                            {
                                                                foreach (var obj in filteredRouteData)
                                                                {
                                                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    if (obj.AdhocStatus.Equals("Approved"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Rejected"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Pending"))
                                                                    {
                                                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                        else
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                    }

                                                                }
                                                                await innerDc.Context.SendActivityAsync(resultFormat);
                                                                searchQuery.AdhocDay = null;
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                            {
                                                                int a = 1;
                                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                                foreach (var item in lstPickDetails)
                                                                {
                                                                    if (a > 5)
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = "More",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = "More Pick Details"
                                                                        });
                                                                        PickStatus = "Pick";
                                                                        break;
                                                                    }
                                                                    else
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                                        });
                                                                        a++;
                                                                    }
                                                                }

                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                replyToActivity.Attachments = new List<Attachment>();
                                                                ThumbnailCard tCard = new ThumbnailCard();
                                                                //tCard.Text = "Pick";
                                                                replyToActivity.Text = "Pick";
                                                                tCard.Buttons = lstCardAction;
                                                                //searchQuery.cardTitle = cardTitle.Pick;
                                                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);

                                                    }
                                                }
                                                else
                                                {
                                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);

                                                }
                                            }
                                            else if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()))
                                            {
                                                tripType = "Drop";
                                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                {
                                                    // Output for Drop
                                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                                    {
                                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                        {
                                                            var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                            {
                                                                foreach (var obj in filteredRouteData)
                                                                {
                                                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    if (obj.AdhocStatus.Equals("Approved"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Rejected"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Pending"))
                                                                    {
                                                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                        else
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                    }

                                                                }
                                                                await innerDc.Context.SendActivityAsync(resultFormat);
                                                                searchQuery.AdhocDay = null;
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                            {
                                                                int a = 1;
                                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                                foreach (var item in lstPickDetails)
                                                                {
                                                                    if (a > 5)
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = "More",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = "More Drop Details"
                                                                        });
                                                                        DropStatus = "Drop";
                                                                        break;
                                                                    }
                                                                    else
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                                        });
                                                                        a++;
                                                                    }
                                                                }

                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                replyToActivity.Attachments = new List<Attachment>();
                                                                ThumbnailCard tCard = new ThumbnailCard();
                                                                //tCard.Text = "Drop";
                                                                replyToActivity.Text = "Drop";
                                                                tCard.Buttons = lstCardAction;
                                                                //searchQuery.cardTitle = cardTitle.Drop;
                                                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);

                                                    }
                                                }
                                                else
                                                {
                                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);

                                                }
                                            }
                                            else
                                            {
                                                tripType = "InterOffice";
                                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                                {
                                                    // Output for InterOffice
                                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                                    {
                                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList().Count == 1)
                                                        {
                                                            var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                            {
                                                                foreach (var obj in filteredRouteData)
                                                                {
                                                                    obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                                    if (obj.AdhocStatus.Equals("Approved"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Rejected"))
                                                                    {
                                                                        resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                                    }
                                                                    if (obj.AdhocStatus.Equals("Pending"))
                                                                    {
                                                                        if (obj.AdhocRequestRequestedBy == "Approver")
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                        else
                                                                        {
                                                                            resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                        }
                                                                    }

                                                                }
                                                                await innerDc.Context.SendActivityAsync(resultFormat);
                                                                searchQuery.AdhocDay = null;
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                            if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                            {
                                                                int a = 1;
                                                                List<CardAction> lstCardAction = new List<CardAction>();
                                                                foreach (var item in lstPickDetails)
                                                                {
                                                                    if (a > 5)
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = "More",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = "More Interoffice Details"
                                                                        });
                                                                        InterofficeStatus = "InterOffice";
                                                                        break;
                                                                    }
                                                                    else
                                                                    {
                                                                        lstCardAction.Add(new CardAction()
                                                                        {
                                                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                                        });
                                                                        a++;
                                                                    }
                                                                }

                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                replyToActivity.Attachments = new List<Attachment>();
                                                                ThumbnailCard tCard = new ThumbnailCard();
                                                                //tCard.Text = "Inter-Office";
                                                                replyToActivity.Text = "Inter-Office";
                                                                tCard.Buttons = lstCardAction;
                                                                //searchQuery.cardTitle = cardTitle.InterOffice;
                                                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                            }
                                                            else
                                                            {
                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                await PostToUserForAnyQuery(innerDc, searchQuery);
                                                            }
                                                        }


                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);

                                                    }
                                                }
                                                else
                                                {
                                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);

                                                }
                                            }

                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                        }
                                    }

                                }

                            }
                        }
                        else
                        {
                            if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                            {
                                dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                {
                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstPickDetails.Remove(lstPickDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstPickDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                            });
                                            PickStatus = "";
                                        }
                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More Pick Details"
                                            });
                                            PickStatus = "Pick";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Pick";
                                    //replyToActivity.Text = "Pick";
                                    tCard.Buttons = lstNextCardAction;
                                    //searchQuery.cardTitle = cardTitle.Pick;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                            }
                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                            {
                                dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                {
                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstPickDetails.Remove(lstPickDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstPickDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                            });
                                            DropStatus = "";
                                        }

                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More Drop Details"
                                            });
                                            DropStatus = "Drop";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Drop";
                                    replyToActivity.Text = "Drop";
                                    tCard.Buttons = lstNextCardAction;
                                    ////searchQuery.cardTitle = cardTitle.Drop;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                }
                            }
                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                            {
                                dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                {
                                    var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstInterOfficeDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {

                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                            });
                                            InterofficeStatus = "";
                                        }
                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More InterOffice Details"
                                            });
                                            InterofficeStatus = "InterOffice";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Inter-Office";
                                    replyToActivity.Text = "Inter-Office";
                                    tCard.Buttons = lstNextCardAction;
                                    //searchQuery.cardTitle = cardTitle.InterOffice;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                            }
                            else
                            {
                                dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");

                                await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                                lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);
                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0)
                                {
                                    string tripType = string.Empty;
                                    if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()))
                                    {
                                        tripType = "Pick";
                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                        {
                                            //Output for Pick
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                            {
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count == 1)
                                                {
                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType).ToList();
                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                    {
                                                        foreach (var obj in filteredRouteData)
                                                        {
                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                            {
                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                                else
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                            }

                                                        }
                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                        searchQuery.AdhocDay = null;
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                                else
                                                {
                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                    {
                                                        int a = 1;
                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                        foreach (var item in lstPickDetails)
                                                        {
                                                            if (a > 5)
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = "More",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = "More Pick Details"
                                                                });
                                                                PickStatus = "Pick";
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                                });
                                                                a++;
                                                            }
                                                        }

                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                        replyToActivity.Attachments = new List<Attachment>();
                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                        //tCard.Text = "Pick";
                                                        replyToActivity.Text = "Pick";
                                                        tCard.Buttons = lstCardAction;
                                                        //searchQuery.cardTitle = cardTitle.Pick;
                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }

                                            }
                                            else
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                            }
                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                        }
                                    }
                                    else if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()))
                                    {
                                        tripType = "Drop";
                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                        {
                                            // Output for Drop
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                            {
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count == 1)
                                                {
                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType).ToList();
                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                    {
                                                        foreach (var obj in filteredRouteData)
                                                        {
                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                            {
                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                                else
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                            }

                                                        }
                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                        searchQuery.AdhocDay = null;
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                                else
                                                {
                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                    {
                                                        int a = 1;
                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                        foreach (var item in lstPickDetails)
                                                        {
                                                            if (a > 5)
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = "More",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = "More Drop Details"
                                                                });
                                                                DropStatus = "Drop";
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                                });
                                                                a++;
                                                            }
                                                        }

                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                        replyToActivity.Attachments = new List<Attachment>();
                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                        //tCard.Text = "Drop";
                                                        replyToActivity.Text = "Drop";
                                                        tCard.Buttons = lstCardAction;
                                                        //searchQuery.cardTitle = cardTitle.Drop;
                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                            }
                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                        }
                                    }
                                    else
                                    {
                                        tripType = "InterOffice";
                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                        {
                                            // Output for InterOffice
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                            {
                                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count == 1)
                                                {
                                                    var filteredRouteData = lstAdhocRequestStatus.Where(t => t.TripType == tripType).ToList();
                                                    if (filteredRouteData != null && filteredRouteData.Count > 0)
                                                    {
                                                        foreach (var obj in filteredRouteData)
                                                        {
                                                            obj.AdhocRequestRequestedBy = string.IsNullOrEmpty(obj.AdhocRequestRequestedBy) ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            obj.AdhocRequestRequestedBy = (obj.AdhocRequestRequestedBy == "To Be Approved") ? "Approver" : obj.AdhocRequestRequestedBy;
                                                            if (obj.AdhocStatus.Equals("Approved"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Rejected"))
                                                            {
                                                                resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}** has been **{obj.AdhocStatus}** by **{obj.AdhocRequestRequestedBy}**\n\n ";
                                                            }
                                                            if (obj.AdhocStatus.Equals("Pending"))
                                                            {
                                                                if (obj.AdhocRequestRequestedBy == "Approver")
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact your **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                                else
                                                                {
                                                                    resultFormat += $"Your **{obj.TripType}** Adhoc request for **{obj.shift.Insert(2, ":")}, {Convert.ToDateTime(obj.ShiftDate).ToString("dd MMMM yyyy")}** with **AdhocID: {obj.AdhocID}**  is in **{obj.AdhocStatus}** state. Please contact with **{obj.AdhocRequestRequestedBy}** for Adhoc approval.\n\n ";
                                                                }
                                                            }

                                                        }
                                                        await innerDc.Context.SendActivityAsync(resultFormat);
                                                        searchQuery.AdhocDay = null;
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                                else
                                                {
                                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                                    if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                    {
                                                        int a = 1;
                                                        List<CardAction> lstCardAction = new List<CardAction>();
                                                        foreach (var item in lstPickDetails)
                                                        {
                                                            if (a > 5)
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = "More",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = "More Interoffice Details"
                                                                });
                                                                InterofficeStatus = "InterOffice";
                                                                break;
                                                            }
                                                            else
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                                });
                                                                a++;
                                                            }
                                                        }

                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                        replyToActivity.Attachments = new List<Attachment>();
                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                        //tCard.Text = "Inter-Office";
                                                        replyToActivity.Text = "Inter-Office";
                                                        tCard.Buttons = lstCardAction;
                                                        //searchQuery.cardTitle = cardTitle.InterOffice;
                                                        searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    }
                                                    else
                                                    {
                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                        replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }


                                            }
                                            else
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                            }
                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                        }
                                    }

                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }
                            }
                        }

                    }

                }
                else if (BotHelper.IsDateContains(inputMsg.Text, out inputDate))
                {
                    if (!string.IsNullOrEmpty(inputDate))
                    {
                        string dateRequired = string.Empty;

                        DateTime test;
                        DateTime testdate;
                        if (DateTime.TryParse(inputDate, out testdate))
                        {
                            DateTime dateInput = Convert.ToDateTime(inputDate);
                            CultureInfo invC = CultureInfo.InvariantCulture;
                            var ConvertedDate = dateInput.ToString("d", invC);
                            if (!(DateTime.TryParseExact(ConvertedDate, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                            {
                                searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                            }
                            else
                            {
                                DateTime dt = Convert.ToDateTime(inputDate);
                                dtDaySelect = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                                if (!Validate4daywindow(dt))
                                {
                                    searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(Constants.futureDateMessageInViewSchedule);
                                }
                                else
                                {
                                    if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                        {
                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstPickDetails.Remove(lstPickDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {

                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                    });
                                                    PickStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Pick Details"
                                                    });
                                                    PickStatus = "Pick";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Pick";
                                            //replyToActivity.Text = "Pick";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.Pick;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                        {
                                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstPickDetails.Remove(lstPickDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {

                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                    });
                                                    DropStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Drop Details"
                                                    });
                                                    DropStatus = "Drop";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Drop";
                                            replyToActivity.Text = "Drop";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.Drop;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);

                                        }
                                    }
                                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                                    {

                                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                        else
                                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                        {
                                            var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                            for (int i = 0; i < 5; i++)
                                            {
                                                lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                                            }
                                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                            searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                            searchQuery.cardTitle = "True";

                                            int s = searchQuery.AdhocStatusFlag;
                                            List<CardAction> lstNextCardAction = new List<CardAction>();
                                            foreach (var item in lstInterOfficeDetails)
                                            {

                                                if (lstNextCardAction.Count < 5)
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                    });
                                                    InterofficeStatus = "";
                                                }
                                                else
                                                {
                                                    lstNextCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More InterOffice Details"
                                                    });
                                                    InterofficeStatus = "InterOffice";
                                                    break;
                                                }

                                                s++;

                                            }
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Inter-Office";
                                            replyToActivity.Text = "Inter-Office";
                                            tCard.Buttons = lstNextCardAction;
                                            //searchQuery.cardTitle = cardTitle.InterOffice;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                    }
                                    else
                                    {
                                        await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                        searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                        searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);

                                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                        {
                                            DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                                            DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                                            //Output for Pick
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                            {
                                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                {
                                                    int a = 1;
                                                    List<CardAction> lstCardAction = new List<CardAction>();
                                                    foreach (var item in lstPickDetails)
                                                    {
                                                        if (a > 5)
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More Pick Details"
                                                            });
                                                            PickStatus = "Pick";
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                            });
                                                            a++;
                                                        }

                                                    }

                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Pick";
                                                    replyToActivity.Text = "Pick";
                                                    tCard.Buttons = lstCardAction;
                                                    //searchQuery.cardTitle = cardTitle.Pick;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                                else
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                                }
                                            }
                                            // Output for Drop
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                            {
                                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                {
                                                    int a = 1;
                                                    List<CardAction> lstCardAction = new List<CardAction>();
                                                    foreach (var item in lstPickDetails)
                                                    {
                                                        if (a > 5)
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More Drop Details"
                                                            });
                                                            DropStatus = "Drop";
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                            });
                                                            a++;
                                                        }

                                                    }

                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Drop";
                                                    replyToActivity.Text = "Drop";
                                                    tCard.Buttons = lstCardAction;
                                                    //searchQuery.cardTitle = cardTitle.Drop;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                                else
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                                }

                                            }
                                            // Output for InterOffice
                                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                            {
                                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                                if (lstPickDetails != null && lstPickDetails.Count > 0)
                                                {
                                                    int a = 1;
                                                    List<CardAction> lstCardAction = new List<CardAction>();
                                                    foreach (var item in lstPickDetails)
                                                    {
                                                        if (a > 5)
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = "More",
                                                                Type = ActionTypes.ImBack,
                                                                Value = "More Interoffice Details"
                                                            });
                                                            InterofficeStatus = "InterOffice";
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            lstCardAction.Add(new CardAction()
                                                            {
                                                                Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                                Type = ActionTypes.ImBack,
                                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                            });
                                                            a++;
                                                        }

                                                    }

                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    //tCard.Text = "Inter-Office";
                                                    replyToActivity.Text = "Inter-Office";
                                                    tCard.Buttons = lstCardAction;
                                                    //searchQuery.cardTitle = cardTitle.InterOffice;
                                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }
                                                else
                                                {
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                                }

                                            }

                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);

                                        }
                                    }

                                }

                            }
                        }
                        else
                        {
                            searchQuery.Context = Constants.InputDateMsgInAdhocStatus;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                        }
                    }

                }
                else if (!string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                {
                    DateTime testAdhocdate;
                    //To display the message for invalid formats
                    if (!DateTime.TryParse(searchQuery.AdhocDateFormat, out testAdhocdate))
                    {
                        searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        await innerDc.Context.SendActivityAsync(Constants.InputDateMsgInAdhocStatus);
                    }
                    else
                    {
                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                        dtDaySelect = Convert.ToDateTime(dt.ToShortDateString()).ToString("MM/dd/yyyy");
                        if (!Validate4daywindow(dt))
                        {
                            searchQuery.Context = Constants.InputDateFormatInAdhocStatus;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(Constants.futureDateMessageInViewSchedule);
                        }
                        else
                        {
                            if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                            {

                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                {
                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstPickDetails.Remove(lstPickDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstPickDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                            });
                                            PickStatus = "";
                                        }
                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More Pick Details"
                                            });
                                            PickStatus = "Pick";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Pick";
                                    //replyToActivity.Text = "Pick";
                                    tCard.Buttons = lstNextCardAction;
                                    //searchQuery.cardTitle = cardTitle.Pick;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                            }
                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                            {

                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                {
                                    var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstPickDetails.Remove(lstPickDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstPickDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                            });
                                            DropStatus = "";
                                        }
                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More Drop Details"
                                            });
                                            DropStatus = "Drop";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Drop";
                                    replyToActivity.Text = "Drop";
                                    tCard.Buttons = lstNextCardAction;
                                    //searchQuery.cardTitle = cardTitle.Drop;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                }
                            }
                            else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                            {

                                if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                                    lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                                else
                                    lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                {
                                    var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                    for (int i = 0; i < 5; i++)
                                    {
                                        lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                                    }
                                    searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                    searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                                    searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                                    searchQuery.cardTitle = "True";

                                    int s = searchQuery.AdhocStatusFlag;
                                    List<CardAction> lstNextCardAction = new List<CardAction>();
                                    foreach (var item in lstInterOfficeDetails)
                                    {

                                        if (lstNextCardAction.Count < 5)
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                Type = ActionTypes.ImBack,
                                                Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                            });
                                            InterofficeStatus = "";
                                        }
                                        else
                                        {
                                            lstNextCardAction.Add(new CardAction()
                                            {
                                                Title = "More",
                                                Type = ActionTypes.ImBack,
                                                Value = "More InterOffice Details"
                                            });
                                            InterofficeStatus = "InterOffice";
                                            break;
                                        }

                                        s++;

                                    }
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    //tCard.Text = "Inter-Office";
                                    replyToActivity.Text = "Inter-Office";
                                    tCard.Buttons = lstNextCardAction;
                                    //searchQuery.cardTitle = cardTitle.InterOffice;
                                    searchQuery.Context = Constants.SelectDateAdhocStatus;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                                lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                                searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                                searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);

                                if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                                {
                                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(dt);
                                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                                    //Output for Pick
                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                                    {
                                        var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                        if (lstPickDetails != null && lstPickDetails.Count > 0)
                                        {
                                            int a = 1;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {
                                                if (a > 5)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Pick Details"
                                                    });
                                                    PickStatus = "Pick";
                                                    break;
                                                }
                                                else
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                                    });
                                                    a++;
                                                }

                                            }

                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Pick";
                                            replyToActivity.Text = "Pick";
                                            tCard.Buttons = lstCardAction;
                                            //searchQuery.cardTitle = cardTitle.Pick;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                        else
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypePickEntity);
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                        }

                                    }
                                    // Output for Drop
                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                                    {
                                        var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                        if (lstPickDetails != null && lstPickDetails.Count > 0)
                                        {
                                            int a = 1;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {
                                                if (a > 5)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Drop Details"
                                                    });
                                                    DropStatus = "Drop";
                                                    break;
                                                }
                                                else
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                                    });
                                                    a++;
                                                }

                                            }

                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Drop";
                                            replyToActivity.Text = "Drop";
                                            tCard.Buttons = lstCardAction;
                                            //searchQuery.cardTitle = cardTitle.Drop;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                        else
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeDropEntity);
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                        }

                                    }
                                    // Output for InterOffice
                                    if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                                    {
                                        var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity) && (Convert.ToDateTime(t.ShiftDate).ToString("MM/dd/yyyy").ToLower() == dtDaySelect.ToLower())).ToList();
                                        if (lstPickDetails != null && lstPickDetails.Count > 0)
                                        {
                                            int a = 1;
                                            List<CardAction> lstCardAction = new List<CardAction>();
                                            foreach (var item in lstPickDetails)
                                            {
                                                if (a > 5)
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = "More",
                                                        Type = ActionTypes.ImBack,
                                                        Value = "More Interoffice Details"
                                                    });
                                                    InterofficeStatus = "InterOffice";
                                                    break;
                                                }
                                                else
                                                {
                                                    lstCardAction.Add(new CardAction()
                                                    {
                                                        Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                                        Type = ActionTypes.ImBack,
                                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                                    });
                                                    a++;
                                                }

                                            }

                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            //tCard.Text = "Inter-Office";
                                            replyToActivity.Text = "Inter-Office";
                                            tCard.Buttons = lstCardAction;
                                            //searchQuery.cardTitle = cardTitle.InterOffice;
                                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                        }
                                        else
                                        {
                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.Text = string.Format("Sorry.There are no {0} adhoc request created for you.", Constants.TripTypeInterOfficeEntity);
                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                        }

                                    }

                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                                    await PostToUserForAnyQuery(innerDc, searchQuery);

                                }
                            }

                        }

                    }
                }
                else
                {
                    if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for pick" && !string.IsNullOrEmpty(PickStatus))
                    {
                        dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                        else
                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                        {
                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                            for (int i = 0; i < 5; i++)
                            {
                                lstPickDetails.Remove(lstPickDetails[0]);
                            }
                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                            searchQuery.cardTitle = "True";
                            
                            int s = searchQuery.AdhocStatusFlag;
                            List<CardAction> lstNextCardAction = new List<CardAction>();
                            foreach (var item in lstPickDetails)
                            {

                                if (lstNextCardAction.Count < 5)
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                        Type = ActionTypes.ImBack,
                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                    });
                                    PickStatus = "";
                                }
                                else
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = "More",
                                        Type = ActionTypes.ImBack,
                                        Value = "More Pick Details"
                                    });
                                    PickStatus = "Pick";
                                    break;
                                }

                                s++;

                            }
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            //tCard.Text = "Pick";
                            //replyToActivity.Text = "Pick";
                            tCard.Buttons = lstNextCardAction;
                            //searchQuery.cardTitle = cardTitle.Pick;
                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for drop" && !string.IsNullOrEmpty(DropStatus))
                    {
                        dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                        else
                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                        {
                            var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                            for (int i = 0; i < 5; i++)
                            {
                                lstPickDetails.Remove(lstPickDetails[0]);
                            }
                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                            searchQuery.lstAdhocRequestStatus.AddRange(lstPickDetails);
                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                            searchQuery.cardTitle = "True";

                            int s = searchQuery.AdhocStatusFlag;
                            List<CardAction> lstNextCardAction = new List<CardAction>();
                            foreach (var item in lstPickDetails)
                            {

                                if (lstNextCardAction.Count < 5)
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                        Type = ActionTypes.ImBack,
                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                    });
                                    DropStatus = "";
                                }
                                else
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = "More",
                                        Type = ActionTypes.ImBack,
                                        Value = "More Drop Details"
                                    });
                                    DropStatus = "Drop";
                                    break;
                                }

                                s++;

                            }
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            //tCard.Text = "Drop";
                            replyToActivity.Text = "Drop";
                            tCard.Buttons = lstNextCardAction;
                            //searchQuery.cardTitle = cardTitle.Drop;
                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            await innerDc.Context.SendActivityAsync(replyToActivity);

                        }
                    }
                    else if (innerDc.Context.Activity.Text.ToLower() == "view adhoc status for interoffice" && !string.IsNullOrEmpty(InterofficeStatus))
                    {
                        dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");
                        if (searchQuery.lstAdhocRequestStatus != null && searchQuery.lstAdhocRequestStatus.Count > 0)
                            lstAdhocRequestStatus.AddRange(searchQuery.lstAdhocRequestStatus);
                        else
                            lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);

                        if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                        {
                            var lstInterOfficeDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                            for (int i = 0; i < 5; i++)
                            {
                                lstInterOfficeDetails.Remove(lstInterOfficeDetails[0]);
                            }
                            searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                            searchQuery.lstAdhocRequestStatus.AddRange(lstInterOfficeDetails);
                            searchQuery.AdhocStatusFlag = string.IsNullOrEmpty(searchQuery.cardTitle) ? 6 : searchQuery.AdhocStatusFlag + 5;
                            searchQuery.cardTitle = "True";

                            int s = searchQuery.AdhocStatusFlag;
                            List<CardAction> lstNextCardAction = new List<CardAction>();
                            foreach (var item in lstInterOfficeDetails)
                            {

                                if (lstNextCardAction.Count < 5)
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = $"{s}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                        Type = ActionTypes.ImBack,
                                        Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                    });
                                    InterofficeStatus = "";
                                }
                                else
                                {
                                    lstNextCardAction.Add(new CardAction()
                                    {
                                        Title = "More",
                                        Type = ActionTypes.ImBack,
                                        Value = "More InterOffice Details"
                                    });
                                    InterofficeStatus = "InterOffice";
                                    break;
                                }

                                s++;

                            }
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            //tCard.Text = "Inter-Office";
                            replyToActivity.Text = "Inter-Office";
                            tCard.Buttons = lstNextCardAction;
                            //searchQuery.cardTitle = cardTitle.InterOffice;
                            searchQuery.Context = Constants.SelectDateAdhocStatus;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        dtDaySelect = Convert.ToDateTime(DateTime.Today.ToShortDateString()).ToString("MM/dd/yyyy");

                        await innerDc.Context.SendActivityAsync($"Sit back and take a sip of your coffee while I fetch the status of your adhoc requests.");
                        lstAdhocRequestStatus = await objAdhocStatusService.GetAdhocStatusData(dtDaySelect, searchQuery, _config);
                        searchQuery.lstAdhocRequestStatus = new List<AdhocRequestStatus>();
                        searchQuery.lstAdhocRequestStatus.AddRange(lstAdhocRequestStatus);

                        if (lstAdhocRequestStatus != null && lstAdhocRequestStatus.Count > 0 && lstAdhocRequestStatus[0].AdhocStatus != "Expired")
                        {
                            DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                            DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                            //Output for Pick
                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList().Count > 0)
                            {
                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypePickEntity)).ToList();
                                int a = 1;
                                List<CardAction> lstCardAction = new List<CardAction>();
                                List<CardAction> lstNextCardAction = new List<CardAction>();
                                foreach (var item in lstPickDetails)
                                {
                                    if (a > 5)
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = "More",
                                            Type = ActionTypes.ImBack,
                                            Value = "More Pick Details"
                                        });
                                        PickStatus = "Pick";
                                        break;
                                    }
                                    else
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                            Type = ActionTypes.ImBack,
                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} pick"
                                        });
                                        a++;
                                    }

                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                //tCard.Text = "Pick";
                                replyToActivity.Text = "Pick";
                                tCard.Buttons = lstCardAction;
                                //searchQuery.cardTitle = cardTitle.Pick;
                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);

                            }
                            // Output for Drop
                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList().Count > 0)
                            {
                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeDropEntity)).ToList();
                                int a = 1;
                                List<CardAction> lstCardAction = new List<CardAction>();
                                foreach (var item in lstPickDetails)
                                {
                                    if (a > 5)
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = "More",
                                            Type = ActionTypes.ImBack,
                                            Value = "More Drop Details"
                                        });
                                        DropStatus = "Drop";
                                        break;
                                    }
                                    else
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                            Type = ActionTypes.ImBack,
                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} drop"
                                        });
                                        a++;
                                    }

                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                //tCard.Text = "Drop";
                                replyToActivity.Text = "Drop";
                                tCard.Buttons = lstCardAction;
                                //searchQuery.cardTitle = cardTitle.Drop;
                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);

                            }
                            // Output for InterOffice
                            if (lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList().Count > 0)
                            {
                                var lstPickDetails = lstAdhocRequestStatus.Where(t => t.TripType.Equals(Constants.TripTypeInterOfficeEntity)).ToList();
                                int a = 1;
                                List<CardAction> lstCardAction = new List<CardAction>();
                                foreach (var item in lstPickDetails)
                                {
                                    if (a > 5)
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = "More",
                                            Type = ActionTypes.ImBack,
                                            Value = "More Interoffice Details"
                                        });
                                        InterofficeStatus = "Interoffice";
                                        break;
                                    }
                                    else
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = $"{a}. {item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")}",
                                            Type = ActionTypes.ImBack,
                                            Value = $"{item.shift.Insert(2, ":")}, {Convert.ToDateTime(item.ShiftDate).ToString("dd MMMM yyyy")} interoffice"
                                        });
                                        a++;
                                    }

                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                //tCard.Text = "Inter-Office";
                                replyToActivity.Text = "Inter-Office";
                                tCard.Buttons = lstCardAction;
                                //searchQuery.cardTitle = cardTitle.InterOffice;
                                searchQuery.Context = Constants.SelectDateAdhocStatus;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);

                            }

                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoAdhocCreated);
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }

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
            replyToActivity.Text = "Is that all ? Or do you want my assistance in the any of the other transport queries?";
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
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} adhoc status";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
        }
    }
}
