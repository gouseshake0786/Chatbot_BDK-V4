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
using Bot.Builder.Community.Dialogs.FormFlow;
using Accenture.CIO.WPBot.Core.Models;
using Accenture.CIO.WPBot.Core.Services;
using Accenture.CIO.Bot.Common.Helpers;
using System.Globalization;
namespace Accenture.CIO.WPBot.Dialogs.Handlers
{
    public class GetAdhocRequestDetails : ComponentDialog
    {
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _config;
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private ApiInputDetails _ApiInput;
        private readonly StateBotAccessors _accessors;

        public GetAdhocRequestDetails(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors, IConfiguration config)
           : base(nameof(GetAdhocRequestDetails))
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
            RaiseAdhocData objRaiseAdhocData = new RaiseAdhocData();
            RaiseAdhocService objRaiseAdhocService = new RaiseAdhocService();
            string botResponseType = string.Empty;
            string reply = string.Empty;
            string destinationFacility = string.Empty;
            var inputMsg = innerDc.Context.Activity;
            string inputDatefomrat = string.Empty;
            string replyMessage = string.Empty;
            #region When AdhocType Isn't Null And AdhocType Is Interoffice
            if (!string.IsNullOrEmpty(searchQuery.AdhocType) && (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office)))
            {
                //When Adhoctype is interoffice and AdhocFacility is not null then assign facility value to source facility and make Facility as Null
                if ((searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))
                       && !string.IsNullOrEmpty(searchQuery.AdhocFacility))
                {
                    if (searchQuery.Context.Equals(Constants.SelectDestinationFacility))
                        searchQuery.AdhocDestinationFacility = searchQuery.AdhocFacility;
                    else if (searchQuery.Context.Equals(Constants.SelectSourceFaility))
                        searchQuery.AdhocSourceFacility = searchQuery.AdhocFacility;

                    searchQuery.AdhocFacility = string.Empty;
                }

            }
            #endregion
            #region End Dialog function
            if (innerDc.Context.Activity.Text.ToLower().Contains("dialog in raise ad hoc request"))
            {
                if (!innerDc.Context.Activity.Text.ToLower().Contains("dont"))
                {
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
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
                else
                {
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                }
            }
            #endregion
            #region When All Entities are Null and message to choose Trip Type is displayed
            else if (string.IsNullOrEmpty(searchQuery.AdhocType) && string.IsNullOrEmpty(searchQuery.Confirmation) && string.IsNullOrEmpty(searchQuery.DayOfTravel) && string.IsNullOrEmpty(searchQuery.AdhocDay)
                   && string.IsNullOrEmpty(searchQuery.AdhocFacility) && string.IsNullOrEmpty(searchQuery.AdhocShift) && string.IsNullOrEmpty(searchQuery.AdhocSourceFacility) && string.IsNullOrEmpty(searchQuery.AdhocDestinationFacility)
                   && string.IsNullOrEmpty(searchQuery.AdhocReason) && string.IsNullOrEmpty(searchQuery.TripEarlyDropReason) && string.IsNullOrEmpty(searchQuery.AdhocEarlyDrop) && string.IsNullOrEmpty(searchQuery.ConfirmRaiseAdhoc) && string.IsNullOrEmpty(searchQuery.helpNeeded) && string.IsNullOrEmpty(searchQuery.AdhocChargeCode) /*&& string.IsNullOrEmpty(searchQuery.Context)*/)
            {
                //To validate Date& Day Formats in raising the adhoc
                if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.NextDayOfTravelEntity) ||
                   (BotHelper.IsDateContains(inputMsg.Text, out inputDatefomrat) || !string.IsNullOrEmpty(inputDatefomrat)) || !string.IsNullOrEmpty(searchQuery.AdhocDateFormat))
                {
                    //To validate date in MM/DD/YYYY format
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
                                searchQuery.Context = Constants.InputDateInRaiseAdhoc;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                            }
                            else
                            {
                                dateRequired = inputDatefomrat;
                                DateTime Date = Convert.ToDateTime(dateRequired);
                                // 4 day window validation to raise request for pick/drop in raise adhoc
                                if (Validate4daywindow(Date))
                                {
                                    searchQuery.AdhocShiftDateFlag = "True";
                                    searchQuery.AdhocDate = Convert.ToDateTime(inputDatefomrat);
                                    searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    replyToActivity.Text = Constants.WelcomeMessagecard;
                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                                        new CardAction()
                                        {
                                            Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                }
                                else
                                {
                                    // await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    replyToActivity.Text = Constants.validationMessageinAdhoc;
                                    ThumbnailCard tCard = new ThumbnailCard();

                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                        },
                                        new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                        },
                                         new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                            }
                        }
                        else
                        {
                            searchQuery.Context = Constants.InputDateInRaiseAdhoc;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                            await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);
                        }

                    }
                    ///To validate 26th January kind of Date Format
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
                                        searchQuery.AdhocDateFormat = DateTime.Now.ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;

                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(1).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(2).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(2).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(3).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(3).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(4).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(4).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(5).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(5).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(6).DayOfWeek.ToString().ToLower())
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(6).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }
                                    else
                                    {
                                        searchQuery.AdhocDateFormat = DateTime.Now.AddDays(7).ToString("MM/dd/yyyy");
                                        DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                        searchQuery.AdhocDate = dt.Date;
                                    }

                                    //4 day window validation to raise request for pick/drop in raise adhoc
                                    if (Validate4daywindow(searchQuery.AdhocDate))
                                    {
                                        searchQuery.AdhocShiftDateFlag = "True";
                                        searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;
                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        ThumbnailCard tCard = new ThumbnailCard();
                                        replyToActivity.Text = Constants.WelcomeMessagecard;
                                        tCard.Buttons = new List<CardAction>()
                                        {
                                            new CardAction()
                                            {
                                                Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                                            new CardAction()
                                            {
                                                Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                                            },
                                            new CardAction()
                                            {
                                                Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                                            }
                                        };
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                        await innerDc.Context.SendActivityAsync(replyToActivity);

                                    }
                                    else
                                    {
                                        await ValidateWeekDay(innerDc, searchQuery);
                                    }
                                }
                                else
                                {
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    replyToActivity.Text = Constants.validationMessageinAdhoc;
                                    //replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                                    ThumbnailCard tCard = new ThumbnailCard()
                                    {
                                        Buttons = new List<CardAction>()
                                        {
                                            new CardAction()
                                            {
                                                Title = "Today" ,
                                                Type = ActionTypes.ImBack,
                                                Value = "Today"
                                            },
                                            new CardAction()
                                            {
                                                Title = "Tomorrow" ,
                                                Type = ActionTypes.ImBack,
                                                Value = "Tomorrow"
                                            },
                                            new CardAction()
                                            {
                                                Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                                Type = ActionTypes.ImBack,
                                                Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                            },
                                            new CardAction()
                                            {
                                                Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                                Type = ActionTypes.ImBack,
                                                Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                            }

                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(replyToActivity);
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
                                //4 day window validation to raise request for pick/drop in raise adhoc
                                if (Validate4daywindow(searchQuery.AdhocDate))
                                {
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    ThumbnailCard tCard = new ThumbnailCard();
                                    replyToActivity.Text = Constants.WelcomeMessagecard;
                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                                        new CardAction()
                                        {
                                            Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                                else
                                {
                                    //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    replyToActivity.Text = Constants.validationMessageinAdhoc;
                                    ThumbnailCard tCard = new ThumbnailCard();

                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                        },
                                        new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                        },
                                         new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }

                            }
                            else
                            {
                                DateTime testAdhocdate;
                                //To display the message for invalid formats
                                if (!DateTime.TryParse(searchQuery.AdhocDateFormat, out testAdhocdate))
                                {
                                    //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    replyToActivity.Text = Constants.validationMessageInvalid;
                                    ThumbnailCard tCard = new ThumbnailCard();

                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                        },
                                        new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                        },
                                         new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }
                                else
                                {
                                    DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                    searchQuery.AdhocDate = dt.Date;
                                    //4 day window validation to raise request for pick/drop in raise adhoc
                                    if (Validate4daywindow(searchQuery.AdhocDate))
                                    {
                                        searchQuery.AdhocShiftDateFlag = "True";
                                        searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;
                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        ThumbnailCard tCard = new ThumbnailCard();
                                        replyToActivity.Text = Constants.WelcomeMessagecard;
                                        tCard.Buttons = new List<CardAction>()
                                        {
                                            new CardAction()
                                            {
                                                Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                                            new CardAction()
                                            {
                                                Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                                            },
                                            new CardAction()
                                            {
                                                Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                                            }
                                        };
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                        await innerDc.Context.SendActivityAsync(replyToActivity);

                                    }
                                    else
                                    {
                                        // await innerDc.Context.SendActivityAsync($"Sorry !. You can't raise the Adhoc request for choosen date");
                                        //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        replyToActivity.Text = Constants.validationMessageinAdhoc;
                                        //replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                                        ThumbnailCard tCard = new ThumbnailCard()
                                        {
                                            Buttons = new List<CardAction>()
                                            {
                                                new CardAction()
                                                {
                                                    Title = "Today" ,
                                                    Type = ActionTypes.ImBack,
                                                    Value = "Today"

                                                },
                                                new CardAction()
                                                {
                                                    Title = "Tomorrow" ,
                                                    Type = ActionTypes.ImBack,
                                                    Value = "Tomorrow"
                                                },
                                                new CardAction()
                                                {
                                                    Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                                    Type = ActionTypes.ImBack,
                                                    Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                },
                                                 new CardAction()
                                                {
                                                    Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                                    Type = ActionTypes.ImBack,
                                                    Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                },

                                            }
                                        };
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        await innerDc.Context.SendActivityAsync(replyToActivity);


                                    }
                                }
                            }
                        }
                        else
                        {
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            replyToActivity.Text = Constants.validationMessageinAdhoc;
                            // replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                                {
                                    new CardAction()
                                    {
                                        Title = "Today" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Today"

                                    },
                                    new CardAction()
                                    {
                                        Title = "Tomorrow" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Tomorrow"
                                    },
                                    new CardAction()
                                    {
                                        Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                        Type = ActionTypes.ImBack,
                                        Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                    },
                                    new CardAction()
                                    {
                                        Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                        Type = ActionTypes.ImBack,
                                        Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                    },
                                }
                            };
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToActivity.Attachments = new List<Attachment>();
                        replyToActivity.Text = Constants.validationMessageInvalid;
                        ThumbnailCard tCard = new ThumbnailCard();

                        tCard.Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                            },
                            new CardAction()
                            {
                                Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                            }
                        };
                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                        searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                        await innerDc.Context.SendActivityAsync(replyToActivity);
                    }

                }
                else
                {
                    searchQuery.AdhocDate = searchQuery.AdhocShiftDateFlag != null ? searchQuery.AdhocDate : DateTime.Now;

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.WelcomeMessagecard;
                    tCard.Buttons = new List<CardAction>()
                    {
                        new CardAction()
                        {
                            Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                        new CardAction()
                        {
                            Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                        },
                        new CardAction()
                        {
                            Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                        }
                    };
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(replyToActivity);

                }
            }
            #endregion           
            #region When SrcFacility Or DestFacility Choosen For Edit
            else if (innerDc.Context.Activity.Text.ToLower().Equals("source facility choosen for edit in raise ad hoc") || innerDc.Context.Activity.Text.ToLower().Equals("destination facility choosen for edit in raise ad hoc"))
            {
                if (innerDc.Context.Activity.Text.ToLower().Equals("destination facility choosen for edit in raise ad hoc"))
                {
                    await innerDc.Context.SendActivityAsync(Constants.WhenDestInEditMode);
                    searchQuery.AdhocDestinationFacility = null;
                }

                searchQuery.AdhocSourceFacility = null;
                searchQuery.AdhocDestinationFacility = null;
                searchQuery.AdhocShift = null;
                searchQuery.AdhocDay = null;
                List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                lstRaiseAdhoc = await objRaiseAdhocService.GetAllFacility(searchQuery, _config);

                if (lstRaiseAdhoc != null && lstRaiseAdhoc.Count > 0)
                {
                    searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                    searchQuery.lstShiftCallData.AddRange(lstRaiseAdhoc);
                    List<CardAction> lstCardAction = new List<CardAction>();

                    foreach (var item in lstRaiseAdhoc)
                    {
                        lstCardAction.Add(new CardAction()
                        {
                            Title = item.FacilityId,
                            Type = ActionTypes.ImBack,
                            Value = item.FacilityId
                        });
                    }

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.SelectSourceFaility;
                    tCard.Buttons = lstCardAction;
                    searchQuery.Context = Constants.SelectSourceFaility;
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
                else
                {
                    await innerDc.Context.SendActivityAsync($"{Constants.NoFacilityFound} {Constants.AssignYourSelfFacility}");

                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    return await innerDc.EndDialogAsync();
                }
            }
            #endregion
            #region When SourceFacility Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocSourceFacility) && string.IsNullOrEmpty(searchQuery.AdhocDestinationFacility))
            {
                //SsearchQuery.AdhocDay = null;
                List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                if (searchQuery.AdhocSourceFacility.Contains("for facility"))
                    searchQuery.AdhocSourceFacility = searchQuery.AdhocSourceFacility.Replace(" for facility", "");
                else
                    searchQuery.AdhocSourceFacility = searchQuery.AdhocSourceFacility;

                if (searchQuery.lstShiftCallData != null && searchQuery.lstShiftCallData.Count > 0)
                    lstRaiseAdhoc.AddRange(searchQuery.lstShiftCallData);
                else
                    lstRaiseAdhoc = await objRaiseAdhocService.GetAllFacility(searchQuery, _config);
                for (int i = 0; i < lstRaiseAdhoc.Count; i++)
                {
                    if (searchQuery.AdhocSourceFacility.ToLower() == lstRaiseAdhoc[i].FacilityId.ToLower())
                        lstRaiseAdhoc.Remove(lstRaiseAdhoc[i]);
                }
                if (lstRaiseAdhoc != null && lstRaiseAdhoc.Count > 0)
                {
                    List<CardAction> lstCardAction = new List<CardAction>();
                    foreach (var item in lstRaiseAdhoc)
                    {
                        lstCardAction.Add(new CardAction()
                        {
                            Title = item.FacilityId,
                            Type = ActionTypes.ImBack,
                            Value = item.FacilityId
                        });
                    }

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.SelectDestinationFacility;
                    tCard.Buttons = lstCardAction;
                    searchQuery.Context = Constants.SelectDestinationFacility;
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(Constants.NoFacilityFound);
                    await innerDc.Context.SendActivityAsync(Constants.AssignYourSelfFacility);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    return await innerDc.EndDialogAsync();
                }
            }
            #endregion
            #region When DestinationFacility Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocDestinationFacility) && string.IsNullOrEmpty(searchQuery.AdhocShift))
            {
                if (searchQuery.AdhocDestinationFacility.Contains("for facility"))
                    searchQuery.AdhocDestinationFacility = searchQuery.AdhocDestinationFacility.Replace(" for facility", "");
                else
                    searchQuery.AdhocDestinationFacility = searchQuery.AdhocDestinationFacility;
                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                searchQuery.AdhocDate = searchQuery.AdhocShiftDateFlag != null ? searchQuery.AdhocDate : DateTime.Now;
                DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToActivity.Attachments = new List<Attachment>();
                replyToActivity.Text = Constants.SelectShiftTime;
                List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                if (searchQuery.lstShiftByFacility != null && searchQuery.lstShiftByFacility.Count > 0)
                    lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                else
                {
                    if (searchQuery.EmployeeAssignFacility == null)
                    {
                        if (objRaiseAdhocData.objRaiseAdhoc != null)
                        {
                            searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                            searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                            searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                            searchQuery.AdhocFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                            searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                        }
                    }
                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                    //lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                }
                if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                {
                    if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                    {
                        istShiftDate = istShiftDate.AddHours(2);
                        int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                        lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                    }
                    else
                    {
                        lstShiftByFacility = lstShiftByFacility.ToList();
                    }

                    if (lstShiftByFacility.Count > 0)
                    {

                        List<CardAction> lstCardAction = new List<CardAction>();

                        foreach (var item in lstShiftByFacility)
                        {
                            lstCardAction.Add(new CardAction()
                            {
                                Title = item.Shift.Insert(2, ":"),
                                Type = ActionTypes.ImBack,
                                Value = item.Shift.Insert(2, ":")
                            });
                        }

                        ThumbnailCard tCard = new ThumbnailCard();
                        tCard.Buttons = lstCardAction;
                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        await innerDc.Context.SendActivityAsync(replyToActivity);

                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoShiftForFacility);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else
                {
                    await innerDc.Context.SendActivityAsync(Constants.NoShift);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }

            }
            #endregion
            #region Shift Has Been Choosen In Edit Mode
            else if (((!string.IsNullOrEmpty(searchQuery.AdhocShift) && (!string.IsNullOrEmpty(searchQuery.AdhocType) && (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office)))
                && !innerDc.Context.Activity.Text.ToLower().Contains("edit") && string.IsNullOrEmpty(searchQuery.ConfirmRaiseAdhoc)) && string.IsNullOrEmpty(searchQuery.AdhocChargeCode)))
            {
                // Set GatingExclusion Parameter
                List<RaiseAdhoc> lstShiftCallData = new List<RaiseAdhoc>();
                List<RaiseAdhoc> lstAdhocReason = new List<RaiseAdhoc>();

                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))
                {
                    lstShiftCallData = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                    if (lstShiftCallData != null && lstShiftCallData.Count > 0)
                    {
                        string shiftReasonGating = lstShiftCallData.Where(t => t.Shift.ToLower().Trim().Equals(searchQuery.AdhocShift.Replace(" ", "").Replace(":", ""))).Select(t => t.ChargeCodeGating).First();

                        if ((lstShiftCallData[0].Gender == "Female" && (shiftReasonGating.Split('-')[0].ToString() == "N")))
                            searchQuery.gatingExclusion = "Y";
                        else
                            searchQuery.gatingExclusion = "N";
                        shiftReasonGating = null;
                        lstShiftCallData = null;
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync($"{Constants.NoShift}. Please restart the conversation");
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
                else
                {
                    if (searchQuery.lstShiftCallData != null && searchQuery.lstShiftCallData.Count > 0 && searchQuery.lstShiftCallData[0].Gender != null)
                        lstShiftCallData.AddRange(searchQuery.lstShiftCallData);
                    else
                    {
                        var lst = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                        lstShiftCallData = lst.lstRaiseAdhoc;
                        lstShiftCallData[0].Gender = lst.objRaiseAdhoc.Gender;
                        lst = null;
                    }

                    if (searchQuery.lstAdhcoReason != null && searchQuery.lstAdhcoReason.Count > 0)
                        lstAdhocReason.AddRange(searchQuery.lstAdhcoReason);
                    else
                        lstAdhocReason = await objRaiseAdhocService.GetEmployeeAdhocReason(searchQuery, _config);

                    string adhocReasonGating = lstAdhocReason.Where(t => t.AdhocReason.ToLower().Trim().Equals(searchQuery.AdhocReason.ToLower().Trim())).Select(t => t.ChargeCodeGating).First();
                    string shiftReasonGating = lstShiftCallData.Where(t => t.Shift.ToLower().Trim().Equals(searchQuery.AdhocShift.Replace(" ", "").Replace(":", ""))).Select(t => t.ChargeCodeGating).First();

                    if ((adhocReasonGating.Split('-')[0].ToString().Trim() == "N") || (lstShiftCallData[0].Gender == "Female" && (shiftReasonGating.Split('-')[0].ToString() == "N")))
                        searchQuery.gatingExclusion = "Y";
                    searchQuery.gatingExclusion = "N";
                    adhocReasonGating = shiftReasonGating = null;
                    lstShiftCallData = lstAdhocReason = null;
                }
                // Set GatingExclusion Parameter ends here

                // Check for WBSE
                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))
                {
                    var lstWbse = await objRaiseAdhocService.GetEmployeeWBSE(searchQuery, _config);
                    if (lstWbse != null && lstWbse.Count > 0 && !string.IsNullOrEmpty(lstWbse[0].WBSElement))
                    {
                        if (lstWbse.Count.Equals(1))
                        {
                            searchQuery.AdhocChargeCode = lstWbse[0].WBSElement;

                            //Check for WBSE validation when Auto filled
                            if (searchQuery.AdhocChargeCode.ToLower().Contains(Constants.ChargeCode.ToLower()))
                                searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode.Replace("charge code ", "");
                            else
                                searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode;

                            if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender) || string.IsNullOrEmpty(searchQuery.AdhocFacility))
                            {
                                var lstRaiseAdhoc = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                                searchQuery.Entity = lstRaiseAdhoc.objRaiseAdhoc.Entity;
                                searchQuery.Deal = lstRaiseAdhoc.objRaiseAdhoc.Deal;
                                searchQuery.Gender = lstRaiseAdhoc.objRaiseAdhoc.Gender;
                                searchQuery.AdhocFacility = lstRaiseAdhoc.objRaiseAdhoc.FacilityId;
                                lstRaiseAdhoc = null;

                                // When Entity,Deal,Gender is null
                                if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender))
                                {
                                    string messageValidationOutput = "Your Entity";
                                    messageValidationOutput = string.IsNullOrEmpty(searchQuery.Entity) ? "Your Entity" : string.Empty;
                                    messageValidationOutput += string.IsNullOrEmpty(searchQuery.Deal) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Deal" : ", Deal" : messageValidationOutput = string.Empty;
                                    messageValidationOutput += string.IsNullOrEmpty(searchQuery.Gender) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Gender" : ", Gender" : messageValidationOutput = string.Empty;
                                    messageValidationOutput += Constants.EntityDealGenderNotUpdated;

                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(messageValidationOutput);
                                    return await innerDc.EndDialogAsync();
                                }
                            }

                            var objValidChargeCode = await objRaiseAdhocService.ValidateChargeCode(searchQuery, _config);
                            // If WBSE is valid
                            if (objValidChargeCode.ChargeCodeValidation == true && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                            {

                                // Populate Summary
                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                                replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                                replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                                replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                                if (searchQuery.AdhocReason != null)
                                {
                                    replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                                }
                                if (searchQuery.AdhocSourceFacility != null)
                                {
                                    replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                                    searchQuery.AdhocFacility = null;
                                }
                                if (searchQuery.AdhocFacility != null)
                                {
                                    replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                                }
                                if (searchQuery.AdhocDestinationFacility != null)
                                {
                                    replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                                }

                                replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";

                                ThumbnailCard tCard = new ThumbnailCard()
                                {
                                    Buttons = new List<CardAction>()
                                {
                                    new CardAction()
                                    {
                                        Title = "Submit" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Submit"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Cancel" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Cancel"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Edit My Choices" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Edit My Choices"
                                    }
                                }
                                };
                                searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);

                            }
                            else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                            {
                                await innerDc.Context.SendActivityAsync(Constants.WbseValidationMessage);
                                searchQuery.Context = Constants.EnterCorrectChargeCode;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                //await innerDc.Context.SendActivityAsync(Constants.EnterCorrectChargeCode);
                            }
                            else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == false)
                            {
                                await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                                string eid = searchQuery.EnterpriseId;
                                searchQuery = new UserQuery();
                                searchQuery.EnterpriseId = eid; eid = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);

                                return await innerDc.EndDialogAsync();
                            }
                        }
                        else
                        {
                            // Populate all the wbse and asked to choose
                            List<CardAction> lstCardAction = new List<CardAction>();
                            foreach (var item in lstWbse)
                            {
                                lstCardAction.Add(new CardAction()
                                {
                                    Title = item.WBSElement,
                                    Type = ActionTypes.ImBack,
                                    Value = item.WBSElement
                                });
                            }

                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            replyToActivity.Text = Constants.EnterOrInputCorrectChargeCode;
                            tCard.Buttons = lstCardAction;
                            searchQuery.Context = Constants.EnterOrInputCorrectChargeCode;
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        searchQuery.Context = Constants.ProvideValidChargeCode;
                        await innerDc.Context.SendActivityAsync(Constants.ProvideValidChargeCode);
                    }
                }
                else
                {
                    searchQuery.Context = Constants.ProvideValidChargeCodeInEditMode;
                    await innerDc.Context.SendActivityAsync(Constants.ProvideValidChargeCodeInEditMode);
                }

                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            }
            #endregion
            #region When AdhocDay Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocDay) && innerDc.Context.Activity.Text.ToLower().Contains("day"))
            {

                if (!string.IsNullOrEmpty(searchQuery.AdhocType))
                {
                    objRaiseAdhocData = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType.Trim());
                    List<CardAction> lstCa = new List<CardAction>();
                    List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    replyToActivity.Text = Constants.SelectShiftTime;
                    // Fill Entity, Deal, EmpFacility & Gender

                    if (searchQuery.AdhocDay.ToLower().Equals(Constants.day1.ToLower()))
                    {
                        searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.ToShortDateString());
                        DateTime utcCurrentDate = TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(DateTime.Now));
                        DateTime istCurrentDate = TimeZoneInfo.ConvertTimeFromUtc(utcCurrentDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                        if (searchQuery.EmployeeAssignFacility == null)
                        {
                            if (objRaiseAdhocData.objRaiseAdhoc != null)
                            {
                                searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                            }
                        }
                        if (searchQuery.lstShiftByFacility != null && searchQuery.lstShiftByFacility.Count > 0)
                            lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                        else
                            lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                        if (lstShiftByFacility.Count == 0)
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoShiftForFacility);
                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                        else
                        {
                            if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                            {
                                istCurrentDate = istCurrentDate.AddHours(4);
                                int currentShift = Convert.ToInt32(istCurrentDate.ToString("HHmm"));
                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop") || searchQuery.AdhocType.Trim().ToLower().Equals("inter office"))
                            {
                                istCurrentDate = istCurrentDate.AddHours(2);
                                int currentShift = Convert.ToInt32(istCurrentDate.ToString("HHmm"));
                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }

                            if (lstShiftByFacility.Count > 0)
                            {
                                List<CardAction> lstCardAction = new List<CardAction>();

                                foreach (var item in lstShiftByFacility)
                                {
                                    lstCardAction.Add(new CardAction()
                                    {
                                        Title = item.Shift.Insert(2, ":"),
                                        Type = ActionTypes.ImBack,
                                        Value = item.Shift.Insert(2, ":")
                                    });
                                }

                                ThumbnailCard tCard = new ThumbnailCard();
                                tCard.Buttons = lstCardAction;
                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                await innerDc.Context.SendActivityAsync(replyToActivity);
                            }
                            else
                            {
                                //await innerDc.Context.SendActivityAsync(Constants.NoShift);

                                //Display the message if the date is current date
                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                {
                                    //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                    string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                    searchQuery.AdhocShiftDateFlag = "True";
                                    List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                    lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                    if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                    {
                                        List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                        foreach (var item in lstShiftByFacilityNextDay)
                                        {
                                            lstCardActionNextDay.Add(new CardAction()
                                            {
                                                Title = item.Shift.Insert(2, ":"),
                                                Type = ActionTypes.ImBack,
                                                Value = item.Shift.Insert(2, ":")
                                            });
                                        }

                                        ThumbnailCard tCard = new ThumbnailCard();

                                        tCard.Buttons = lstCardActionNextDay;
                                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                                        replyToActivity.Text = message;
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                    }

                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoShiftForFacility);
                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day2.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(1).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day3.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(2).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day4.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(3).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day5.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(4).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day6.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(5).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if ((searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office)))
                        {
                            if (!string.IsNullOrEmpty(searchQuery.AdhocSourceFacility))
                            {
                                if (searchQuery.lstShiftByFacility != null && searchQuery.lstShiftByFacility.Count > 0)
                                    lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                                else
                                {
                                    if (searchQuery.EmployeeAssignFacility == null)
                                    {
                                        if (objRaiseAdhocData.objRaiseAdhoc != null)
                                        {
                                            searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                            searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                            searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                            searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                            searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                        }
                                    }
                                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                    //lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                                }


                                if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                {

                                    List<CardAction> lstCardAction = new List<CardAction>();

                                    foreach (var item in lstShiftByFacility)
                                    {
                                        lstCardAction.Add(new CardAction()
                                        {
                                            Title = item.Shift.Insert(2, ":"),
                                            Type = ActionTypes.ImBack,
                                            Value = item.Shift.Insert(2, ":")
                                        });
                                    }

                                    ThumbnailCard tCard = new ThumbnailCard();
                                    tCard.Buttons = lstCardAction;
                                    searchQuery.Context = Constants.SelectShiftForAdhoc;
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoShiftForFacility);
                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }

                            }

                        }
                        else
                        {
                            if (searchQuery.lstShiftByFacility != null && searchQuery.lstShiftByFacility.Count > 0)
                                lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                            else
                            {
                                if (searchQuery.EmployeeAssignFacility == null)
                                {
                                    if (objRaiseAdhocData.objRaiseAdhoc != null)
                                    {
                                        searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                        searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                        searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                        searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                        searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                    }
                                }
                                lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                //lstShiftByFacility.AddRange(searchQuery.lstShiftByFacility);
                            }


                            if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                            {

                                List<CardAction> lstCardAction = new List<CardAction>();

                                foreach (var item in lstShiftByFacility)
                                {
                                    lstCardAction.Add(new CardAction()
                                    {
                                        Title = item.Shift.Insert(2, ":"),
                                        Type = ActionTypes.ImBack,
                                        Value = item.Shift.Insert(2, ":")
                                    });
                                }

                                ThumbnailCard tCard = new ThumbnailCard();
                                tCard.Buttons = lstCardAction;
                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync(replyToActivity);

                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoShiftForFacility);
                                string eid = searchQuery.EnterpriseId;
                                searchQuery = new UserQuery();
                                searchQuery.EnterpriseId = eid; eid = null;
                                await PostToUserForAnyQuery(innerDc, searchQuery);
                            }

                        }


                    }
                    searchQuery.lstShiftByFacility = null;
                }
                else
                {
                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.WelcomeMessagecard;
                    tCard.Buttons = new List<CardAction>()
                    {
                        new CardAction()
                        {
                            Title = "To Office", Type = ActionTypes.ImBack, Value = "To Office"},
                        new CardAction()
                        {
                            Title = "To Home", Type = ActionTypes.ImBack, Value = "To Home"
                        },
                        new CardAction()
                        {
                            Title = "Inter Office", Type = ActionTypes.ImBack, Value = "Inter Office"
                        }
                    };
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    searchQuery.Context = Constants.SelectTripTypeForAdhoc;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
            }
            #endregion
            #region When AdhocFacility Isn't Null & Contains Facility Keyword
            else if (!string.IsNullOrEmpty(searchQuery.AdhocFacility) && searchQuery.AdhocFacility.ToLower().Contains("facility"))
            {
                // filter the facility id from input
                if (searchQuery.AdhocFacility.ToLower().Contains("for facility"))
                    searchQuery.AdhocFacility = searchQuery.AdhocFacility.Replace(" for facility", "");
                else
                    searchQuery.AdhocFacility = searchQuery.AdhocFacility;

                if (!string.IsNullOrEmpty(searchQuery.AdhocType))
                {
                    var listShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                    searchQuery.lstShiftByFacility = new List<RaiseAdhoc>();
                    searchQuery.lstShiftByFacility.AddRange(listShiftByFacility);

                    if (listShiftByFacility != null && listShiftByFacility.Count > 0)
                    {
                        DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                        DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                        if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity))
                        {
                            searchQuery.Context = Constants.SelectShiftForAdhoc;
                            if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                            {
                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                {
                                    istShiftDate = istShiftDate.AddHours(4);
                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                    listShiftByFacility = listShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                }
                                else
                                {
                                    listShiftByFacility = listShiftByFacility.ToList();
                                }
                            }
                            else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                            {
                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                {
                                    istShiftDate = istShiftDate.AddHours(2);
                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                    listShiftByFacility = listShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                }
                                else
                                {
                                    listShiftByFacility = listShiftByFacility.ToList();
                                }
                            }

                            if (listShiftByFacility.Count > 0)
                            {
                                List<CardAction> lstCardAction = new List<CardAction>();

                                foreach (var item in listShiftByFacility)
                                {
                                    lstCardAction.Add(new CardAction()
                                    {
                                        Title = item.Shift.Insert(2, ":"),
                                        Type = ActionTypes.ImBack,
                                        Value = item.Shift.Insert(2, ":")
                                    });
                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                ThumbnailCard tCard = new ThumbnailCard();
                                replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                tCard.Buttons = lstCardAction;
                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                await innerDc.Context.SendActivityAsync(replyToActivity);
                            }
                            else
                            {
                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                {
                                    //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                    string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                    searchQuery.AdhocShiftDateFlag = "True";
                                    List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                    lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                    if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                    {
                                        List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                        foreach (var item in lstShiftByFacilityNextDay)
                                        {
                                            lstCardActionNextDay.Add(new CardAction()
                                            {
                                                Title = item.Shift.Insert(2, ":"),
                                                Type = ActionTypes.ImBack,
                                                Value = item.Shift.Insert(2, ":")
                                            });
                                        }

                                        ThumbnailCard tCard = new ThumbnailCard();
                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        tCard.Buttons = lstCardActionNextDay;
                                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                                        replyToActivity.Text = message;
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                    }

                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }
                            }
                        }
                        else
                        {
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;

                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                                {
                                    new CardAction()
                                    {
                                        Title = "Today",
                                        Type = ActionTypes.ImBack,
                                        Value = "Today"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Tomorrow",
                                        Type = ActionTypes.ImBack,
                                        Value = "Tomorrow"
                                    },
                                    new CardAction()
                                    {
                                        Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                        Type = ActionTypes.ImBack,
                                        Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                    },
                                    new CardAction()
                                    {
                                        Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                        Type = ActionTypes.ImBack,
                                        Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                    }
                                }
                            };
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
            }
            #endregion
            #region When Facility Choosen For Editing
            else if (innerDc.Context.Activity.Text.ToLower().Equals("facility choosen for edit in raise ad hoc"))
            {
                searchQuery.AdhocShift = string.Empty;
                List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                lstRaiseAdhoc = await objRaiseAdhocService.GetAllFacility(searchQuery, _config);

                if (lstRaiseAdhoc != null && lstRaiseAdhoc.Count > 0)
                {
                    searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                    searchQuery.lstShiftCallData.AddRange(lstRaiseAdhoc);
                    List<CardAction> lstCardAction = new List<CardAction>();

                    foreach (var item in lstRaiseAdhoc)
                    {
                        lstCardAction.Add(new CardAction()
                        {
                            Title = item.FacilityId,
                            Type = ActionTypes.ImBack,
                            Value = item.FacilityId
                        });
                    }

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.SelectFacilityForRaiseAdhoc;
                    tCard.Buttons = lstCardAction;
                    searchQuery.Context = Constants.SelectFacilityForRaiseAdhoc;
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
                else
                {
                    await innerDc.Context.SendActivityAsync($"{Constants.NoFacilityAssigned} {Constants.AssignYourSelfFacility}");
                    
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    return await innerDc.EndDialogAsync();
                }
            }
            #endregion
            #region When Wbse Is Edited
            else if (!string.IsNullOrEmpty(searchQuery.AdhocChargeCode) && innerDc.Context.Activity.Text.ToLower().Contains("wbse edit"))
            {
                if (searchQuery.AdhocChargeCode.ToLower().Contains(Constants.ChargeCode.ToLower()))
                    searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode.Replace("charge code ", "");
                else
                    searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode;

                if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender) || string.IsNullOrEmpty(searchQuery.AdhocFacility))
                {
                    var lstRaiseAdhoc = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                    searchQuery.Entity = lstRaiseAdhoc.objRaiseAdhoc.Entity;
                    searchQuery.Deal = lstRaiseAdhoc.objRaiseAdhoc.Deal;
                    searchQuery.Gender = lstRaiseAdhoc.objRaiseAdhoc.Gender;
                    //searchQuery.AdhocFacility = lstRaiseAdhoc.objRaiseAdhoc.FacilityId;
                    lstRaiseAdhoc = null;

                    // When Entity,Deal,Gender is null
                    if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender))
                    {
                        string messageValidationOutput = "Your Entity";
                        messageValidationOutput = string.IsNullOrEmpty(searchQuery.Entity) ? "Your Entity" : string.Empty;
                        messageValidationOutput += string.IsNullOrEmpty(searchQuery.Deal) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Deal" : ", Deal" : messageValidationOutput = string.Empty;
                        messageValidationOutput += string.IsNullOrEmpty(searchQuery.Gender) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Gender" : ", Gender" : messageValidationOutput = string.Empty;
                        messageValidationOutput += Constants.EntityDealGenderNotUpdated;

                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        await innerDc.Context.SendActivityAsync(messageValidationOutput);
                        return await innerDc.EndDialogAsync();
                    }
                }

                var objValidChargeCode = await objRaiseAdhocService.ValidateChargeCode(searchQuery, _config);
                if (objValidChargeCode.ChargeCodeValidation == true && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                {
                    // Populate Summary
                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                    replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                    replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                    replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                    if (searchQuery.AdhocReason != null)
                    {
                        replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                    }
                    if (searchQuery.AdhocSourceFacility != null)
                    {
                        replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                        searchQuery.AdhocFacility = null;
                    }
                    if (searchQuery.AdhocFacility != null)
                    {
                        replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                    }
                    if (searchQuery.AdhocDestinationFacility != null)
                    {
                        replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                    }

                    replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";


                    ThumbnailCard tCard = new ThumbnailCard()
                    {
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Submit" ,
                                Type = ActionTypes.ImBack,
                                Value = "Submit"
                            },
                            new CardAction()
                            {
                                Title = "Cancel" ,
                                Type = ActionTypes.ImBack,
                                Value = "Cancel"
                            },
                            new CardAction()
                            {
                                Title = "Edit My Choices" ,
                                Type = ActionTypes.ImBack,
                                Value = "Edit My Choices"
                            }
                        }
                    };
                    searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
                else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                {
                    await innerDc.Context.SendActivityAsync(Constants.WbseValidationMessage);
                    searchQuery.Context = Constants.ProvideValidChargeCodeInEditMode;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    //await innerDc.Context.SendActivityAsync(Constants.ProvideValidChargeCodeInEditMode);
                }
                else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == false)
                {
                    await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);

                    return await innerDc.EndDialogAsync();
                }
            }
            #endregion
            #region When Wbse Choosen For Editing
            else if (innerDc.Context.Activity.Text.ToLower().Equals("wbse choosen for edit in raise ad hoc"))
            {
                searchQuery.Context = Constants.ProvideValidChargeCodeInEditMode;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                await innerDc.Context.SendActivityAsync(Constants.ProvideValidChargeCodeInEditMode);
            }
            #endregion
            #region When Adhoc Shift Edited
            else if (!string.IsNullOrEmpty(searchQuery.AdhocShift) && innerDc.Context.Activity.Text.ToLower().Contains("edit shift"))
            {
                // Populate Summary
                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToActivity.Attachments = new List<Attachment>();
                replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                if (searchQuery.AdhocReason != null)
                {
                    replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                }
                if (searchQuery.AdhocSourceFacility != null)
                {
                    replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                    searchQuery.AdhocFacility = null;
                }
                if (searchQuery.AdhocFacility != null)
                {
                    replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                }
                if (searchQuery.AdhocDestinationFacility != null)
                {
                    replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                }

                replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";

                ThumbnailCard tCard = new ThumbnailCard()
                {
                    Buttons = new List<CardAction>()
                    {
                        new CardAction()
                        {
                            Title = "Submit" ,
                            Type = ActionTypes.ImBack,
                            Value = "Submit"
                        },
                        new CardAction()
                        {
                            Title = "Cancel" ,
                            Type = ActionTypes.ImBack,
                            Value = "Cancel"
                        },
                        new CardAction()
                        {
                            Title = "Edit My Choices" ,
                            Type = ActionTypes.ImBack,
                            Value = "Edit My Choices"
                        }
                    }
                };
                searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                replyToActivity.Attachments.Add(tCard.ToAttachment());

                await innerDc.Context.SendActivityAsync(replyToActivity);
            }
            #endregion
            #region When Shift Date Choosen For Editing
            else if (innerDc.Context.Activity.Text.ToLower().Equals("shift date choosen for edit in raise ad hoc"))
            {
                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToActivity.Attachments = new List<Attachment>();
                replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice.ToLower()))
                {
                    ThumbnailCard tCard = new ThumbnailCard()
                    {
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Today" ,
                                Type = ActionTypes.ImBack,
                                Value = "Today"

                            },
                            new CardAction()
                            {
                                Title = "Tomorrow" ,
                                Type = ActionTypes.ImBack,
                                Value = "Tomorrow"
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                Type = ActionTypes.ImBack,
                                Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                Type = ActionTypes.ImBack,
                                Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                            },

                        }
                    };
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                }
                else
                {
                    ThumbnailCard tCard = new ThumbnailCard()
                    {
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Today" ,
                                Type = ActionTypes.ImBack,
                                Value = "Today"

                            },
                            new CardAction()
                            {
                                Title = "Tomorrow" ,
                                Type = ActionTypes.ImBack,
                                Value = "Tomorrow"
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                Type = ActionTypes.ImBack,
                                Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                            },
                            new CardAction()
                            {
                                Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                Type = ActionTypes.ImBack,
                                Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                            },

                        }
                    };
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                }


                searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                await innerDc.Context.SendActivityAsync(replyToActivity);

            }
            #endregion
            #region When Shift Choosen For Editing
            else if (innerDc.Context.Activity.Text.ToLower().Equals("shift choosen for edit in raise ad hoc"))
            {
                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))
                    objRaiseAdhocData.lstRaiseAdhoc = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                else
                    objRaiseAdhocData.lstRaiseAdhoc = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                if (objRaiseAdhocData != null)
                {
                    if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                    {
                        List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                        DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                        DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                        if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                        {
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                istShiftDate = istShiftDate.AddHours(4);
                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else
                            {
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc.ToList();
                            }

                        }
                        else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                        {
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                istShiftDate = istShiftDate.AddHours(2);
                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else
                            {
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc.ToList();
                            }
                        }
                        else if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice.ToLower()))
                        {
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                istShiftDate = istShiftDate.AddHours(2);
                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else
                            {
                                lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc;
                            }
                        }
                        else
                            lstRaiseAdhoc = objRaiseAdhocData.lstRaiseAdhoc;

                        if (lstRaiseAdhoc.Count > 0)
                        {
                            List<CardAction> lstCardAction = new List<CardAction>();

                            foreach (var item in lstRaiseAdhoc)
                            {
                                lstCardAction.Add(new CardAction()
                                {
                                    Title = item.Shift.Insert(2, ":"),
                                    Type = ActionTypes.ImBack,
                                    Value = item.Shift.Insert(2, ":")
                                });
                            }

                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            replyToActivity.Text = Constants.SelectShiftPostEdit;
                            tCard.Buttons = lstCardAction;
                            searchQuery.Context = Constants.SelectShiftPostEdit;
                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            lstRaiseAdhoc = null;

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                        else
                        {
                            await innerDc.Context.SendActivityAsync(Constants.NoShiftForEdit);
                            // Populate Summary
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                            replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                            replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                            replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                            if (searchQuery.AdhocReason != null)
                            {
                                replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                            }
                            if (searchQuery.AdhocSourceFacility != null)
                            {
                                replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                                searchQuery.AdhocFacility = null;
                            }
                            if (searchQuery.AdhocFacility != null)
                            {
                                replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                            }
                            if (searchQuery.AdhocDestinationFacility != null)
                            {
                                replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                            }

                            replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";

                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                                {
                                    new CardAction()
                                    {
                                        Title = "Submit" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Submit"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Cancel" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Cancel"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Edit My Choices" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Edit My Choices"
                                    }
                                }
                            };
                            searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
            }
            #endregion
            #region When Adhoc reason chosen for Editing
            else if (innerDc.Context.Activity.Text.ToLower().Equals("adhoc reason choosen for edit in raise ad hoc"))
            {
                // Populate AdhocReason
                var lstAdhocReason = await objRaiseAdhocService.GetEmployeeAdhocReason(searchQuery, _config);
                if (lstAdhocReason != null && lstAdhocReason.Count > 0)
                {
                    searchQuery.lstAdhcoReason = new List<RaiseAdhoc>();
                    searchQuery.lstAdhcoReason.AddRange(lstAdhocReason);
                    List<CardAction> lstCardAction = new List<CardAction>();
                    foreach (var item in lstAdhocReason)
                    {
                        lstCardAction.Add(new CardAction()
                        {
                            Title = item.AdhocReason,
                            Type = ActionTypes.ImBack,
                            Value = item.AdhocReason
                        });
                    }

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.SelectAdhocReason;
                    tCard.Buttons = lstCardAction;
                    searchQuery.Context = Constants.SelectAdhocReason;
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    lstAdhocReason = null;
                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
            }
            #endregion
            #region Response To Edit My Choice
            else if (innerDc.Context.Activity.Text.ToLower().Equals(Constants.ResponseToEditMyChoice))
            {
                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToActivity.Attachments = new List<Attachment>();
                replyToActivity.Text = Constants.ChooseOptionBelowForEdit + "\n\n";
                ThumbnailCard tCard = new ThumbnailCard();
                List<CardAction> lstCardAction = new List<CardAction>();
                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))
                {
                    lstCardAction.Add(new CardAction() { Title = "Source Facility", Type = ActionTypes.ImBack, Value = "Source Facility" });
                    lstCardAction.Add(new CardAction() { Title = "Destination Facility", Type = ActionTypes.ImBack, Value = "Destination Facility" });
                    lstCardAction.Add(new CardAction() { Title = "Trip Time", Type = ActionTypes.ImBack, Value = "Trip Time" });
                    lstCardAction.Add(new CardAction() { Title = "WBSE Code", Type = ActionTypes.ImBack, Value = "WBSE Code" });
                    lstCardAction.Add(new CardAction() { Title = "Trip Date", Type = ActionTypes.ImBack, Value = "Trip Date" });
                }
                else
                {
                    lstCardAction.Add(new CardAction() { Title = "Facility", Type = ActionTypes.ImBack, Value = "Facility" });
                    lstCardAction.Add(new CardAction() { Title = "Trip Time", Type = ActionTypes.ImBack, Value = "Trip Time" });
                    lstCardAction.Add(new CardAction() { Title = "WBSE Code", Type = ActionTypes.ImBack, Value = "WBSE Code" });
                    lstCardAction.Add(new CardAction() { Title = "Trip Date", Type = ActionTypes.ImBack, Value = "Trip Date" });
                    lstCardAction.Add(new CardAction() { Title = "Adhoc Reason", Type = ActionTypes.ImBack, Value = "Adhoc Reason" });
                }
                tCard.Buttons = lstCardAction;
                searchQuery.Context = Constants.ChooseOptionBelowForEdit;
                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                replyToActivity.Attachments.Add(tCard.ToAttachment());

                await innerDc.Context.SendActivityAsync(replyToActivity);
            }
            #endregion
            #region Response To Adjacent Routes
            else if (innerDc.Context.Activity.Text.ToLower().Equals("display adhocid in raise adhoc"))
            {
                RaiseAdhoc adhocResult = await objRaiseAdhocService.RaiseAdhocRequest(searchQuery, _config);
                searchQuery.adhocID = adhocResult.AdhocId;

                if (searchQuery.adhocID != 0 && adhocResult.Success == true)
                {
                    RaiseAdhoc adhocResultLog = await objRaiseAdhocService.RaiseAdhocRequestLog(searchQuery, _config);
                    // Send auto approval mail
                    if (!string.IsNullOrEmpty(searchQuery.Gender) && !string.IsNullOrEmpty(searchQuery.AdhocShift))
                    {
                        string shift = searchQuery.AdhocShift.ToLower().Replace(" ", "").Replace(":", "");
                        if ((searchQuery.Gender.ToLower().Trim().Equals("female") || searchQuery.Gender.ToLower().Trim().Equals("f")) && ((Convert.ToInt32(shift) >= 1900 && Convert.ToInt32(shift) <= 2359) || (Convert.ToInt32(shift) >= 0000 && Convert.ToInt32(shift) <= 0700)))
                        {
                            objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                            objRaiseAdhocService.SubmitAutoApproveAdhocRequest(searchQuery, _config, "AutoApprovalForWomenEmployee", "Approve");
                            RaiseAdhoc objRaiseAdhoc = await objRaiseAdhocService.SendAutoApprovalMail(searchQuery, _config, "Approve");

                            await innerDc.Context.SendActivityAsync($"Your request number is {searchQuery.adhocID}. It has been auto approved as the chosen drop time is between 7pm and 7am.");
                        }
                        else if (_config["IsRequiredToSendMail"].ToLower().Equals("yes"))
                        {
                            await innerDc.Context.SendActivityAsync($"{Constants.AdhocRequestID} {searchQuery.adhocID}. {Constants.AdhocRequestMessage}");
                            objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                            objRaiseAdhocService.SendNewAdhocMail(searchQuery, _config);
                        }
                        shift = null;
                    }
                    else if (_config["IsRequiredToSendMail"].ToLower().Equals("yes"))
                    {
                        await innerDc.Context.SendActivityAsync($"{Constants.AdhocRequestID} {searchQuery.adhocID}. {Constants.AdhocRequestMessage}");
                        objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                        objRaiseAdhocService.SendNewAdhocMail(searchQuery, _config);
                    }

                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }
                else if (searchQuery.adhocID == 0 && adhocResult.Success == true)
                {
                    await innerDc.Context.SendActivityAsync(Constants.AdhocRequestNotRaised);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }
                else if (searchQuery.adhocID == 0 && adhocResult.Success == false)
                {
                    await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }
            }
            else if (innerDc.Context.Activity.Text.ToLower().Equals("dont display adhocid in raise adhoc"))
            {
                string eid = searchQuery.EnterpriseId;
                searchQuery = new UserQuery();
                searchQuery.EnterpriseId = eid; eid = null;
                await PostToUserForAnyQuery(innerDc, searchQuery);
            }
            #endregion
            #region EarlyDropReason Has Been Provided
            else if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.PromptToInputEarlyDropReason))
            {
                string inputEarlyDropReason = innerDc.Context.Activity.Text.Substring(innerDc.Context.Activity.Text.IndexOf(":"));
                if (inputEarlyDropReason == null || string.IsNullOrEmpty(inputEarlyDropReason.Trim()))
                {
                    searchQuery.Context = Constants.ReasonEarlyDrop;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    await innerDc.Context.SendActivityAsync(Constants.ReasonEarlyDrop);
                }
                else
                {
                    searchQuery.TripEarlyDropReason = inputEarlyDropReason.Trim();
                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                    replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                    replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                    replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + "\n\n";
                    replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "\n\n";
                    replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "\n\n";
                    replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";

                    ThumbnailCard tCard = new ThumbnailCard()
                    {
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Submit" ,
                                Type = ActionTypes.ImBack,
                                Value = "Submit"
                            },
                            new CardAction()
                            {
                                Title = "Cancel" ,
                                Type = ActionTypes.ImBack,
                                Value = "Cancel"
                            },
                            new CardAction()
                            {
                                Title = "Edit My Choices" ,
                                Type = ActionTypes.ImBack,
                                Value = "Edit My Choices"
                            }
                        }
                    };
                    searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    replyToActivity.Attachments.Add(tCard.ToAttachment());

                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
            }
            #endregion
            #region When ConfirmRaiseAdhoc Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.ConfirmRaiseAdhoc))
            {
                if (searchQuery.ConfirmRaiseAdhoc.ToLower().Equals(Constants.Confirm.ToLower()))
                {
                    var adhocValidationRqst = await objRaiseAdhocService.ValidateAdhocRequest(searchQuery, _config);
                    if (adhocValidationRqst.AdhocRequestValidate == true && adhocValidationRqst.Success == true)
                    {
                        if (searchQuery.EarlyDrop != 'Y')
                            searchQuery.EarlyDrop = 'N';

                        if (searchQuery.TripEarlyDropReason == null)
                            searchQuery.TripEarlyDropReason = "";

                        if (searchQuery.AdhocSourceFacility == null)
                            searchQuery.AdhocSourceFacility = "";

                        if (searchQuery.AdhocDestinationFacility == null)
                            searchQuery.AdhocDestinationFacility = "";

                        if (searchQuery.AdhocReason == null)
                            searchQuery.AdhocReason = "";

                        if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office.ToLower()))
                            searchQuery.AdhocType = Constants.AdhocTypeInterOffice;

                        RaiseAdhoc adhocResult = new RaiseAdhoc();
                        // If trip type is Interoffice
                        if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice.ToLower()))
                        {
                            if (!string.IsNullOrEmpty(searchQuery.AdhocSourceFacility) && !string.IsNullOrEmpty(searchQuery.AdhocDestinationFacility))
                            {
                                if (searchQuery.AdhocSourceFacility == searchQuery.AdhocDestinationFacility)
                                {
                                    string resultMessage = string.Empty;
                                    resultMessage = Constants.SourceDestinationFacility + "\n\n";
                                    resultMessage += Constants.ModifySourceDestinationSelection;
                                    await innerDc.Context.SendActivityAsync(resultMessage);
                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await PostToUserForAnyQuery(innerDc, searchQuery);

                                    return await innerDc.EndDialogAsync();
                                }
                                else
                                {
                                    adhocResult = await objRaiseAdhocService.RaiseAdhocRequest(searchQuery, _config);
                                    searchQuery.adhocID = adhocResult.AdhocId;
                                }
                            }
                        }
                        else
                        {
                            List<RouteInfo> lstRouteInfo = new List<RouteInfo>();
                            lstRouteInfo = await objRaiseAdhocService.GetAdjacentRoutes(searchQuery, _config);

                            if (lstRouteInfo != null && lstRouteInfo.Count > 0)
                            {
                                foreach (var item in lstRouteInfo)
                                {
                                    await innerDc.Context.SendActivityAsync($"You have a regular **{item.triptype}** schedule at **{item.Shift}**.It stands cancelled if this request gets approved.Would you like to proceed? \n\n");

                                }

                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                replyToActivity.Attachments = new List<Attachment>();
                                replyToActivity.Text = Constants.AdjacentRouteConfirmation;
                                ThumbnailCard tCard = new ThumbnailCard()
                                {
                                    Buttons = new List<CardAction>()
                                    {
                                        new CardAction(){Title = "Yes",Type = ActionTypes.ImBack,Value = "Yes"},
                                        new CardAction(){Title = "No" ,Type = ActionTypes.ImBack,Value = "No"}
                                    }
                                };
                                searchQuery.Context = Constants.AdjacentRouteConfirmation;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                return await innerDc.EndDialogAsync();
                            }
                            else
                            {
                                adhocResult = await objRaiseAdhocService.RaiseAdhocRequest(searchQuery, _config);
                                searchQuery.adhocID = adhocResult.AdhocId;
                            }
                        }

                        if (searchQuery.adhocID != 0 && adhocResult.Success == true)
                        {
                            RaiseAdhoc adhocResultLog = await objRaiseAdhocService.RaiseAdhocRequestLog(searchQuery, _config);
                            // Send auto approval mail
                            if (!string.IsNullOrEmpty(searchQuery.Gender) && !string.IsNullOrEmpty(searchQuery.AdhocShift))
                            {
                                string shift = searchQuery.AdhocShift.ToLower().Replace(" ", "").Replace(":", "");
                                if ((searchQuery.Gender.ToLower().Trim().Equals("female") || searchQuery.Gender.ToLower().Trim().Equals("f")) && ((Convert.ToInt32(shift) >= 1900 && Convert.ToInt32(shift) <= 2359) || (Convert.ToInt32(shift) >= 0000 && Convert.ToInt32(shift) <= 0700)))
                                {
                                    objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                                    objRaiseAdhocService.SubmitAutoApproveAdhocRequest(searchQuery, _config, "AutoApprovalForWomenEmployee", "Approve");
                                    RaiseAdhoc objRaiseAdhoc = await objRaiseAdhocService.SendAutoApprovalMail(searchQuery, _config, "Approve");

                                    await innerDc.Context.SendActivityAsync($"Your request number is {searchQuery.adhocID}. It has been auto approved as the chosen drop time is between 7pm and 7am.");
                                }
                                else if (_config["IsRequiredToSendMail"].ToLower().Equals("yes"))
                                {
                                    await innerDc.Context.SendActivityAsync($"{Constants.AdhocRequestID} {searchQuery.adhocID}. {Constants.AdhocRequestMessage}");
                                    objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                                    objRaiseAdhocService.SendNewAdhocMail(searchQuery, _config);
                                }
                                shift = null;
                            }
                            else if (_config["IsRequiredToSendMail"].ToLower().Equals("yes"))
                            {
                                await innerDc.Context.SendActivityAsync($"{Constants.AdhocRequestID} {searchQuery.adhocID}. {Constants.AdhocRequestMessage}");
                                objRaiseAdhocService.SendAdhocMail(searchQuery, _config);
                                objRaiseAdhocService.SendNewAdhocMail(searchQuery, _config);
                            }



                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                        else if (searchQuery.adhocID == 0 && adhocResult.Success == true)
                        {
                            await innerDc.Context.SendActivityAsync(Constants.AdhocRequestNotRaised);
                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                        else if (searchQuery.adhocID == 0 && adhocResult.Success == false)
                        {
                            await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);

                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await PostToUserForAnyQuery(innerDc, searchQuery);
                        }
                    }
                    else if (adhocValidationRqst.AdhocRequestValidation == false && adhocValidationRqst.Success == true)
                    {
                        await innerDc.Context.SendActivityAsync(Constants.AlreadyApprovedRequest);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                    else if (adhocValidationRqst.AdhocRequestValidation == false && adhocValidationRqst.Success == false)
                    {
                        await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                    }
                }
                else
                {
                    //await innerDc.Context.SendActivityAsync(Constants.ResultMessageNotConfirm);
                    string eid = searchQuery.EnterpriseId;
                    searchQuery = new UserQuery();
                    searchQuery.EnterpriseId = eid; eid = null;
                    await PostToUserForAnyQuery(innerDc, searchQuery);
                }
            }
            #endregion
            #region When AdhocChargeCode Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocChargeCode) && !innerDc.Context.Activity.Text.ToLower().Contains("edit"))
            {
                if (!string.IsNullOrEmpty(searchQuery.AdhocShift))
                {
                    if (searchQuery.AdhocChargeCode.ToLower().Contains(Constants.ChargeCode.ToLower()))
                        searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode.Replace("charge code ", "");
                    else
                        searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode;

                    if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender) || string.IsNullOrEmpty(searchQuery.AdhocFacility))
                    {
                        var lstRaiseAdhoc = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                        searchQuery.Entity = lstRaiseAdhoc.objRaiseAdhoc.Entity;
                        searchQuery.Deal = lstRaiseAdhoc.objRaiseAdhoc.Deal;
                        searchQuery.Gender = lstRaiseAdhoc.objRaiseAdhoc.Gender;
                        searchQuery.AdhocFacility = lstRaiseAdhoc.objRaiseAdhoc.FacilityId;
                        lstRaiseAdhoc = null;

                        // When Entity,Deal,Gender is null
                        if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender))
                        {
                            string messageValidationOutput = "Your Entity";
                            messageValidationOutput = string.IsNullOrEmpty(searchQuery.Entity) ? "Your Entity" : string.Empty;
                            messageValidationOutput += string.IsNullOrEmpty(searchQuery.Deal) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Deal" : ", Deal" : messageValidationOutput = string.Empty;
                            messageValidationOutput += string.IsNullOrEmpty(searchQuery.Gender) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Gender" : ", Gender" : messageValidationOutput = string.Empty;
                            messageValidationOutput += Constants.EntityDealGenderNotUpdated;

                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            await innerDc.Context.SendActivityAsync(messageValidationOutput);
                            return await innerDc.EndDialogAsync();
                        }
                    }

                    var objValidChargeCode = await objRaiseAdhocService.ValidateChargeCode(searchQuery, _config);
                    // If WBSE is valid
                    if (objValidChargeCode.ChargeCodeValidation == true && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                    {
                        // Populate Summary
                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToActivity.Attachments = new List<Attachment>();
                        replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                        replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                        replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                        replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                        if (searchQuery.AdhocReason != null)
                        {
                            replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                        }
                        if (searchQuery.AdhocSourceFacility != null)
                        {
                            replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                            searchQuery.AdhocFacility = null;
                        }
                        if (searchQuery.AdhocFacility != null)
                        {
                            replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                        }
                        if (searchQuery.AdhocDestinationFacility != null)
                        {
                            replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                        }

                        replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";


                        ThumbnailCard tCard = new ThumbnailCard()
                        {
                            Buttons = new List<CardAction>()
                            {
                                new CardAction()
                                {
                                    Title = "Submit" ,
                                    Type = ActionTypes.ImBack,
                                    Value = "Submit"
                                },
                                new CardAction()
                                {
                                    Title = "Cancel" ,
                                    Type = ActionTypes.ImBack,
                                    Value = "Cancel"
                                },
                                new CardAction()
                                {
                                    Title = "Edit My Choices" ,
                                    Type = ActionTypes.ImBack,
                                    Value = "Edit My Choices"
                                }
                            }
                        };
                        searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                        await innerDc.Context.SendActivityAsync(replyToActivity);

                    }
                    else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                    {
                        await innerDc.Context.SendActivityAsync(Constants.WbseValidationMessage);
                        searchQuery.Context = Constants.EnterCorrectChargeCode;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                        //await innerDc.Context.SendActivityAsync(Constants.EnterCorrectChargeCode);
                    }
                    else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == false)
                    {
                        await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);

                        return await innerDc.EndDialogAsync();
                    }
                }
                else
                {
                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                    if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                    {
                        if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                        {
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                istShiftDate = istShiftDate.AddHours(4);
                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else
                            {
                                lstShiftByFacility = lstShiftByFacility.ToList();
                            }
                        }
                        else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                        {
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                istShiftDate = istShiftDate.AddHours(2);
                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                            }
                            else
                            {
                                lstShiftByFacility = lstShiftByFacility.ToList();
                            }
                        }

                        if (lstShiftByFacility.Count > 0)
                        {
                            List<CardAction> lstCardAction = new List<CardAction>();

                            foreach (var item in lstShiftByFacility)
                            {
                                lstCardAction.Add(new CardAction()
                                {
                                    Title = item.Shift.Insert(2, ":"),
                                    Type = ActionTypes.ImBack,
                                    Value = item.Shift.Insert(2, ":")
                                });
                            }

                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            ThumbnailCard tCard = new ThumbnailCard();
                            replyToActivity.Text = Constants.SelectShiftForAdhoc;
                            tCard.Buttons = lstCardAction;
                            searchQuery.Context = Constants.SelectShiftForAdhoc;
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);
                        }
                        else
                        {
                            //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                            //Display the message if the date is current date
                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                            {
                                //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                searchQuery.AdhocShiftDateFlag = "True";
                                List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                {
                                    List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                    foreach (var item in lstShiftByFacilityNextDay)
                                    {
                                        lstCardActionNextDay.Add(new CardAction()
                                        {
                                            Title = item.Shift.Insert(2, ":"),
                                            Type = ActionTypes.ImBack,
                                            Value = item.Shift.Insert(2, ":")
                                        });
                                    }

                                    ThumbnailCard tCard = new ThumbnailCard();
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    tCard.Buttons = lstCardActionNextDay;
                                    searchQuery.Context = Constants.SelectShiftForAdhoc;
                                    replyToActivity.Text = message;
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                }

                            }
                            else
                            {
                                await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                string eid = searchQuery.EnterpriseId;
                                searchQuery = new UserQuery();
                                searchQuery.EnterpriseId = eid; eid = null;

                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                return await innerDc.EndDialogAsync();
                            }

                        }
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await PostToUserForAnyQuery(innerDc, searchQuery);
                    }
                }
            }
            #endregion
            #region When AdhocReason isn't null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocReason) && string.IsNullOrEmpty(searchQuery.AdhocChargeCode) &&
                !(!string.IsNullOrEmpty(searchQuery.AdhocType) && (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office))))
            {
                // Set GatingExclusion Parameter
                List<RaiseAdhoc> lstShiftCallData = new List<RaiseAdhoc>();
                List<RaiseAdhoc> lstAdhocReason = new List<RaiseAdhoc>();

                if (searchQuery.lstShiftCallData != null && searchQuery.lstShiftCallData.Count > 0 && searchQuery.lstShiftCallData[0].Gender != null)
                    lstShiftCallData.AddRange(searchQuery.lstShiftCallData);
                else
                {
                    var lst = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                    lstShiftCallData = lst.lstRaiseAdhoc;
                    lstShiftCallData[0].Gender = lst.objRaiseAdhoc.Gender;
                    lst = null;
                }

                if (searchQuery.lstAdhcoReason != null && searchQuery.lstAdhcoReason.Count > 0)
                    lstAdhocReason.AddRange(searchQuery.lstAdhcoReason);
                else
                    lstAdhocReason = await objRaiseAdhocService.GetEmployeeAdhocReason(searchQuery, _config);

                string adhocReasonGating = lstAdhocReason.Where(t => t.AdhocReason.ToLower().Trim().Equals(searchQuery.AdhocReason.ToLower().Trim())).Select(t => t.ChargeCodeGating).First();
                string shiftReasonGating = lstShiftCallData.Where(t => t.Shift.ToLower().Trim().Equals(searchQuery.AdhocShift.Replace(" ", "").Replace(":", ""))).Select(t => t.ChargeCodeGating).First();

                if ((adhocReasonGating.Split('-')[0].ToString().Trim() == "N") || (lstShiftCallData[0].Gender == "Female" && (shiftReasonGating.Split('-')[0].ToString() == "N")))
                    searchQuery.gatingExclusion = "Y";
                else
                    searchQuery.gatingExclusion = "N";
                adhocReasonGating = shiftReasonGating = null;
                lstShiftCallData = lstAdhocReason = null;
                // Set GatingExclusion Parameter ends here

                // Check for WBSE
                var lstWbse = await objRaiseAdhocService.GetEmployeeWBSE(searchQuery, _config);
                if (lstWbse != null && lstWbse.Count > 0 && !string.IsNullOrEmpty(lstWbse[0].WBSElement))
                {
                    if (lstWbse.Count.Equals(1))
                    {
                        searchQuery.AdhocChargeCode = lstWbse[0].WBSElement;

                        //Check for WBSE validation when Auto filled
                        if (searchQuery.AdhocChargeCode.ToLower().Contains(Constants.ChargeCode.ToLower()))
                            searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode.Replace("charge code ", "");
                        else
                            searchQuery.AdhocChargeCode = searchQuery.AdhocChargeCode;

                        if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender) || string.IsNullOrEmpty(searchQuery.AdhocFacility))
                        {
                            var lstRaiseAdhoc = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType);
                            searchQuery.Entity = lstRaiseAdhoc.objRaiseAdhoc.Entity;
                            searchQuery.Deal = lstRaiseAdhoc.objRaiseAdhoc.Deal;
                            searchQuery.Gender = lstRaiseAdhoc.objRaiseAdhoc.Gender;
                            searchQuery.AdhocFacility = lstRaiseAdhoc.objRaiseAdhoc.FacilityId;
                            lstRaiseAdhoc = null;

                            // When Entity,Deal,Gender is null
                            if (string.IsNullOrEmpty(searchQuery.Entity) || string.IsNullOrEmpty(searchQuery.Deal) || string.IsNullOrEmpty(searchQuery.Gender))
                            {
                                string messageValidationOutput = "Your Entity";
                                messageValidationOutput = string.IsNullOrEmpty(searchQuery.Entity) ? "Your Entity" : string.Empty;
                                messageValidationOutput += string.IsNullOrEmpty(searchQuery.Deal) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Deal" : ", Deal" : messageValidationOutput = string.Empty;
                                messageValidationOutput += string.IsNullOrEmpty(searchQuery.Gender) ? string.IsNullOrEmpty(messageValidationOutput) ? "Your Gender" : ", Gender" : messageValidationOutput = string.Empty;
                                messageValidationOutput += Constants.EntityDealGenderNotUpdated;

                                string eid = searchQuery.EnterpriseId;
                                searchQuery = new UserQuery();
                                searchQuery.EnterpriseId = eid; eid = null;
                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                await innerDc.Context.SendActivityAsync(messageValidationOutput);
                                return await innerDc.EndDialogAsync();
                            }
                        }

                        var objValidChargeCode = await objRaiseAdhocService.ValidateChargeCode(searchQuery, _config);
                        // If WBSE is valid
                        if (objValidChargeCode.ChargeCodeValidation == true && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                        {
                            if (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage) && objValidChargeCode.ErrorMessage.ToLower() != "success")
                                await innerDc.Context.SendActivityAsync(objValidChargeCode.ErrorMessage);

                            // Populate Summary
                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            replyToActivity.Attachments = new List<Attachment>();
                            replyToActivity.Text = Constants.ReviewAdhocRequestDetail + "\n\n";
                            replyToActivity.Text += "**Trip Type** : " + searchQuery.AdhocType.First().ToString().ToUpper() + searchQuery.AdhocType.Substring(1) + "  \n\n";
                            replyToActivity.Text += "**Trip Date** : " + searchQuery.AdhocDate.ToString("MM/dd/yyyy") + "\n\n";
                            replyToActivity.Text += "**Trip Time** : " + searchQuery.AdhocShift.Replace(" ", "").Insert(2, ":") + " \n\n";

                            if (searchQuery.AdhocReason != null)
                            {
                                replyToActivity.Text += "**Adhoc Reason** : " + searchQuery.AdhocReason + "  \n\n";
                            }
                            if (searchQuery.AdhocSourceFacility != null)
                            {
                                replyToActivity.Text += "**Source Facility** : " + searchQuery.AdhocSourceFacility.First().ToString().ToUpper() + searchQuery.AdhocSourceFacility.Substring(1) + "  \n\n";
                                searchQuery.AdhocFacility = null;
                            }
                            if (searchQuery.AdhocFacility != null)
                            {
                                replyToActivity.Text += "**Facility** : " + searchQuery.AdhocFacility.First().ToString().ToUpper() + searchQuery.AdhocFacility.Substring(1) + "  \n\n";
                            }
                            if (searchQuery.AdhocDestinationFacility != null)
                            {
                                replyToActivity.Text += "**Destination Facility** : " + searchQuery.AdhocDestinationFacility.First().ToString().ToUpper() + searchQuery.AdhocDestinationFacility.Substring(1) + "  \n\n";
                            }

                            replyToActivity.Text += "**WBSE Code** : " + searchQuery.AdhocChargeCode.ToString().ToUpper() + "\n\n";

                            ThumbnailCard tCard = new ThumbnailCard()
                            {
                                Buttons = new List<CardAction>()
                                {
                                    new CardAction()
                                    {
                                        Title = "Submit" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Submit"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Cancel" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Cancel"
                                    },
                                    new CardAction()
                                    {
                                        Title = "Edit My Choices" ,
                                        Type = ActionTypes.ImBack,
                                        Value = "Edit My Choices"
                                    }
                                }
                            };
                            searchQuery.Context = Constants.ReviewAdhocRequestDetail;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                            await innerDc.Context.SendActivityAsync(replyToActivity);

                        }
                        else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == true && (!string.IsNullOrEmpty(objValidChargeCode.ErrorMessage)))
                        {
                            await innerDc.Context.SendActivityAsync(Constants.WbseValidationMessage);
                            searchQuery.Context = Constants.EnterCorrectChargeCode;
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                            //await innerDc.Context.SendActivityAsync(Constants.EnterCorrectChargeCode);
                        }
                        else if (objValidChargeCode.ChargeCodeValidation == false && objValidChargeCode.Success == false)
                        {
                            await innerDc.Context.SendActivityAsync(Constants.ErrorInApiCall);
                            string eid = searchQuery.EnterpriseId;
                            searchQuery = new UserQuery();
                            searchQuery.EnterpriseId = eid; eid = null;
                            await PostToUserForAnyQuery(innerDc, searchQuery);

                            return await innerDc.EndDialogAsync();
                        }
                    }
                    else
                    {
                        // Populate all the wbse and asked to choose
                        List<CardAction> lstCardAction = new List<CardAction>();
                        foreach (var item in lstWbse)
                        {
                            lstCardAction.Add(new CardAction()
                            {
                                Title = item.WBSElement,
                                Type = ActionTypes.ImBack,
                                Value = item.WBSElement
                            });
                        }

                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToActivity.Attachments = new List<Attachment>();
                        ThumbnailCard tCard = new ThumbnailCard();
                        replyToActivity.Text = Constants.EnterOrInputCorrectChargeCode;
                        tCard.Buttons = lstCardAction;
                        searchQuery.Context = Constants.EnterOrInputCorrectChargeCode;
                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                        await innerDc.Context.SendActivityAsync(replyToActivity);
                    }
                }
                else
                {
                    searchQuery.Context = Constants.ProvideValidChargeCode;
                    await innerDc.Context.SendActivityAsync(Constants.ProvideValidChargeCode);
                }
                lstWbse = null;

                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            }
            #endregion
            #region When Adhoc Shift has been choosen
            else if (!string.IsNullOrEmpty(searchQuery.AdhocShift) && string.IsNullOrEmpty(searchQuery.AdhocReason))
            {
                #region Validate BCPDay
                //bool result = await objRaiseAdhocService.ValidateBCPDay(searchQuery, _config);
                //if (result)
                //{
                //    await innerDc.Context.SendActivityAsync($"Adhoc cannot be requested for BCP/Incident day - {searchQuery.AdhocDate.ToString("MM/dd/yyy")} (\"{ searchQuery.AdhocShift.Replace(" ", ":")}\")!");
                //    string eid = searchQuery.EnterpriseId;
                //    searchQuery = new UserQuery();
                //    searchQuery.EnterpriseId = eid; eid = null;
                //    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                //    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                //    return await innerDc.EndDialogAsync();
                //}
                #endregion
                // Populate AdhocReason
                var lstAdhocReason = await objRaiseAdhocService.GetEmployeeAdhocReason(searchQuery, _config);
                if (lstAdhocReason != null && lstAdhocReason.Count > 0)
                {
                    searchQuery.lstAdhcoReason = new List<RaiseAdhoc>();
                    searchQuery.lstAdhcoReason.AddRange(lstAdhocReason);
                    List<CardAction> lstCardAction = new List<CardAction>();
                    foreach (var item in lstAdhocReason)
                    {
                        lstCardAction.Add(new CardAction()
                        {
                            Title = item.AdhocReason,
                            Type = ActionTypes.ImBack,
                            Value = item.AdhocReason
                        });
                    }

                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyToActivity.Attachments = new List<Attachment>();
                    ThumbnailCard tCard = new ThumbnailCard();
                    replyToActivity.Text = Constants.SelectAdhocReason;
                    tCard.Buttons = lstCardAction;
                    searchQuery.Context = Constants.SelectAdhocReason;
                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                    lstAdhocReason = null;
                    await innerDc.Context.SendActivityAsync(replyToActivity);
                }
            }
            #endregion
            #region When AdhocType Isn't Null
            else if (!string.IsNullOrEmpty(searchQuery.AdhocType) && string.IsNullOrEmpty(searchQuery.AdhocShift))
            {
                if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypePickEntity.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeDropEntity.ToLower()))
                {
                    objRaiseAdhocData = await objRaiseAdhocService.GetEmployeeShiftCumFacility(searchQuery, _config, searchQuery.AdhocType.Trim());
                    if (objRaiseAdhocData != null)
                    {
                        //To validate Date& Day Formats in raising the adhoc
                        if (innerDc.Context.Activity.Text.ToLower().Contains(Constants.TodayDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || innerDc.Context.Activity.Text.ToLower().Contains(Constants.NextDayOfTravelEntity) ||
                    (BotHelper.IsDateContains(inputMsg.Text, out inputDatefomrat) || !string.IsNullOrEmpty(inputDatefomrat)) || (!string.IsNullOrEmpty(searchQuery.AdhocDateFormat)))
                        {
                            if (string.IsNullOrEmpty(searchQuery.AdhocDay))
                            {
                                if (!string.IsNullOrEmpty(inputDatefomrat))
                                {
                                    DateTime test;
                                    DateTime test1;
                                    string dateRequired = string.Empty;
                                    // To Validate the input in DateTime Format
                                    if (DateTime.TryParse(inputDatefomrat, out test1))
                                    {
                                        DateTime dateInput = Convert.ToDateTime(inputDatefomrat);

                                        CultureInfo invC = CultureInfo.InvariantCulture;
                                        var ConvertedDate = dateInput.ToString("d", invC);
                                        //To validate date in MM/DD/YYYY format
                                        if (!(DateTime.TryParseExact(ConvertedDate, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out test)))
                                        {

                                            searchQuery.Context = Constants.InputDateInRaiseAdhoc;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                            await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);

                                        }
                                        else
                                        {
                                            dateRequired = inputDatefomrat;
                                            DateTime Date = Convert.ToDateTime(dateRequired);
                                            //4 day window validation to raise request for pick/drop in raise adhoc
                                            if (Validate4daywindow(Date))
                                            {
                                                searchQuery.AdhocShiftDateFlag = "True";
                                                searchQuery.AdhocDate = Convert.ToDateTime(inputDatefomrat);
                                                searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;

                                                // Fill Entity, Deal, EmpFacility & Gender
                                                if (objRaiseAdhocData.objRaiseAdhoc != null)
                                                {
                                                    searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                                    searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                                    searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                                    searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                                    searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                                }

                                                // Populate shifts to be choosen
                                                if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                                                {
                                                    List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                                                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                                    // store to manipulate gating exclusion
                                                    searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                                    searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                                    List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                                    if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                                    {
                                                        if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                                        {
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                istShiftDate = istShiftDate.AddHours(4);
                                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                            }
                                                            else
                                                            {
                                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                                            }
                                                        }
                                                        else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                                        {
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                istShiftDate = istShiftDate.AddHours(2);
                                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                            }
                                                            else
                                                            {
                                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                                            }
                                                        }

                                                        if (lstShiftByFacility.Count > 0)
                                                        {
                                                            List<CardAction> lstCardAction = new List<CardAction>();

                                                            foreach (var item in lstShiftByFacility)
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = item.Shift.Insert(2, ":"),
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = item.Shift.Insert(2, ":")
                                                                });
                                                            }

                                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                            replyToActivity.Attachments = new List<Attachment>();
                                                            ThumbnailCard tCard = new ThumbnailCard();
                                                            replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                                            tCard.Buttons = lstCardAction;
                                                            searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                            lstRaiseAdhoc = null;
                                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        }
                                                        else
                                                        {
                                                            //Display the message if the date is current date
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                                                searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                                string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);

                                                                searchQuery.AdhocShiftDateFlag = "True";
                                                                List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                                                lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                                                if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                                                {
                                                                    List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                                    foreach (var item in lstShiftByFacilityNextDay)
                                                                    {
                                                                        lstCardActionNextDay.Add(new CardAction()
                                                                        {
                                                                            Title = item.Shift.Insert(2, ":"),
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = item.Shift.Insert(2, ":")
                                                                        });
                                                                    }

                                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                    tCard.Buttons = lstCardActionNextDay;
                                                                    searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                    replyToActivity.Text = message;
                                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                }

                                                            }
                                                            else
                                                            {
                                                                await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                string eid = searchQuery.EnterpriseId;
                                                                searchQuery = new UserQuery();
                                                                searchQuery.EnterpriseId = eid; eid = null;
                                                                lstRaiseAdhoc = null;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                return await innerDc.EndDialogAsync();
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                        string eid = searchQuery.EnterpriseId;
                                                        searchQuery = new UserQuery();
                                                        searchQuery.EnterpriseId = eid; eid = null;
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            }
                                            else
                                            {
                                                // await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                replyToActivity.Text = Constants.validationMessageinAdhoc;
                                                ThumbnailCard tCard = new ThumbnailCard();

                                                tCard.Buttons = new List<CardAction>()
                                                {
                                                    new CardAction()
                                                    {
                                                        Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                    },
                                                     new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                    }
                                                };
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        searchQuery.Context = Constants.InputDateInRaiseAdhoc;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                        await innerDc.Context.SendActivityAsync(Constants.InputDateFormat);

                                    }

                                }
                                ///To validate 26th January etc kind of Date date format
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
                                                    searchQuery.AdhocDateFormat = DateTime.Now.ToString("MM/dd/yyyy");

                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(1).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(2).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(2).ToString("MM/dd/yyyy");
                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(3).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(3).ToString("MM/dd/yyyy");
                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(4).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(4).ToString("MM/dd/yyyy");
                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(5).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(5).ToString("MM/dd/yyyy");
                                                }
                                                else if (searchQuery.AdhocDateFormat.ToLower() == DateTime.Now.AddDays(6).DayOfWeek.ToString().ToLower())
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(6).ToString("MM/dd/yyyy");
                                                }
                                                else
                                                {
                                                    searchQuery.AdhocDateFormat = DateTime.Now.AddDays(7).ToString("MM/dd/yyyy");
                                                }

                                                DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                                searchQuery.AdhocDate = dt.Date;
                                                //4 day window validation to raise request for pick / drop in raise adhoc
                                                if (Validate4daywindow(searchQuery.AdhocDate))
                                                {
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                    searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;

                                                    // Fill Entity, Deal, EmpFacility & Gender
                                                    if (objRaiseAdhocData.objRaiseAdhoc != null)
                                                    {
                                                        searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                                        searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                                        searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                                        searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                                        searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                                    }

                                                    // Populate shifts to be choosen
                                                    if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                                                    {
                                                        List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                                                        DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                                        DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                                        // store to manipulate gating exclusion
                                                        searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                                        searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                                        List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                                        lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                                        if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                                        {
                                                            if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                                            {
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    istShiftDate = istShiftDate.AddHours(4);
                                                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                    lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                                }
                                                                else
                                                                {
                                                                    lstShiftByFacility = lstShiftByFacility.ToList();
                                                                }
                                                            }
                                                            else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                                            {
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    istShiftDate = istShiftDate.AddHours(2);
                                                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                    lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                                }
                                                                else
                                                                {
                                                                    lstShiftByFacility = lstShiftByFacility.ToList();
                                                                }
                                                            }
                                                            else
                                                                lstShiftByFacility = lstShiftByFacility.ToList();

                                                            if (lstShiftByFacility.Count > 0)
                                                            {
                                                                List<CardAction> lstCardAction = new List<CardAction>();

                                                                foreach (var item in lstShiftByFacility)
                                                                {
                                                                    lstCardAction.Add(new CardAction()
                                                                    {
                                                                        Title = item.Shift.Insert(2, ":"),
                                                                        Type = ActionTypes.ImBack,
                                                                        Value = item.Shift.Insert(2, ":")
                                                                    });
                                                                }

                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                replyToActivity.Attachments = new List<Attachment>();
                                                                ThumbnailCard tCard = new ThumbnailCard();
                                                                replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                                                tCard.Buttons = lstCardAction;
                                                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                lstRaiseAdhoc = null;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                            }
                                                            else
                                                            {
                                                                //Display the message if the date is current date
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                    // await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                                                    string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                                    List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                                                    lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);


                                                                    if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                                                    {
                                                                        List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                                        foreach (var item in lstShiftByFacilityNextDay)
                                                                        {
                                                                            lstCardActionNextDay.Add(new CardAction()
                                                                            {
                                                                                Title = item.Shift.Insert(2, ":"),
                                                                                Type = ActionTypes.ImBack,
                                                                                Value = item.Shift.Insert(2, ":")
                                                                            });
                                                                        }

                                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        tCard.Buttons = lstCardActionNextDay;
                                                                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                        replyToActivity.Text = message;
                                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                    string eid = searchQuery.EnterpriseId;
                                                                    searchQuery = new UserQuery();
                                                                    searchQuery.EnterpriseId = eid; eid = null;
                                                                    lstRaiseAdhoc = null;
                                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                    return await innerDc.EndDialogAsync();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                            string eid = searchQuery.EnterpriseId;
                                                            searchQuery = new UserQuery();
                                                            searchQuery.EnterpriseId = eid; eid = null;
                                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                                        }
                                                    }
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                }
                                                else
                                                {
                                                    await ValidateWeekDay(innerDc, searchQuery);
                                                }
                                            }
                                            else
                                            {
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                replyToActivity.Text = Constants.validationMessageinAdhoc;
                                                //replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                                                ThumbnailCard tCard = new ThumbnailCard()
                                                {
                                                    Buttons = new List<CardAction>()
                                                    {
                                                        new CardAction()
                                                        {
                                                            Title = "Today" ,
                                                            Type = ActionTypes.ImBack,
                                                            Value = "Today"
                                                        },
                                                        new CardAction()
                                                        {
                                                            Title = "Tomorrow" ,
                                                            Type = ActionTypes.ImBack,
                                                            Value = "Tomorrow"
                                                        },
                                                        new CardAction()
                                                        {
                                                            Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                                            Type = ActionTypes.ImBack,
                                                            Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                        },
                                                        new CardAction()
                                                        {
                                                            Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                                            Type = ActionTypes.ImBack,
                                                            Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                        }

                                                    }
                                                };
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }

                                        }
                                        //To validate for tomorrow,today,dayaftertomorrowetc..validate for next day shift if current day doesnt have shifts
                                        else if (searchQuery.AdhocDateFormat.ToLower().Contains(Constants.TodayDayOfTravelEntity) || searchQuery.AdhocDateFormat.ToLower().Contains(Constants.TomorrowDayOfTravelEntity) || (searchQuery.AdhocDateFormat.ToLower().Contains(Constants.NextDayOfTravelEntity) && !Constants.WeekDayFormats.Split(',').ToList().Any(t => searchQuery.AdhocDateFormat.ToLower().Contains(t))))
                                        {

                                            var inputmsgList = innerDc.Context.Activity.Text.ToLower().Replace(".", "").Split(new Char[] { ' ' });

                                            int result;
                                            searchQuery.AdhocShiftDateFlag = "False";
                                            //Split the inpout message to get the date from 2 days from today etc kind of scenarios
                                            foreach (var x in inputmsgList)
                                            {
                                                var msgValue = int.TryParse(x, out result);

                                                if (msgValue == true)
                                                {
                                                    //To validate 2 days from tomorrow,2 days from today etc
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
                                                    //To validate only today, tomorrow, next etc
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
                                            //4 day window validation to raise request for pick / drop in raise adhoc
                                            if (Validate4daywindow(searchQuery.AdhocDate))
                                            {

                                                // Fill Entity, Deal, EmpFacility & Gender
                                                if (objRaiseAdhocData.objRaiseAdhoc != null)
                                                {
                                                    searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                                    searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                                    searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                                    searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                                    searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                                }

                                                // Populate shifts to be choosen
                                                if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                                                {
                                                    List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                                                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                                    // store to manipulate gating exclusion
                                                    searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                                    searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                                    List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                                    if (lstShiftByFacility != null & lstShiftByFacility.Count > 0)
                                                    {
                                                        if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                                        {
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                istShiftDate = istShiftDate.AddHours(4);
                                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                            }
                                                            else
                                                            {
                                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                                            }
                                                        }
                                                        else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                                        {
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                istShiftDate = istShiftDate.AddHours(2);
                                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                            }
                                                            else
                                                            {
                                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                                            }
                                                        }
                                                        else
                                                            lstShiftByFacility = lstShiftByFacility.ToList();

                                                        if (lstShiftByFacility.Count > 0)
                                                        {
                                                            List<CardAction> lstCardAction = new List<CardAction>();

                                                            foreach (var item in lstShiftByFacility)
                                                            {
                                                                lstCardAction.Add(new CardAction()
                                                                {
                                                                    Title = item.Shift.Insert(2, ":"),
                                                                    Type = ActionTypes.ImBack,
                                                                    Value = item.Shift.Insert(2, ":")
                                                                });
                                                            }

                                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                            replyToActivity.Attachments = new List<Attachment>();
                                                            ThumbnailCard tCard = new ThumbnailCard();
                                                            replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                                            tCard.Buttons = lstCardAction;
                                                            searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                            replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                            lstRaiseAdhoc = null;
                                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                            await innerDc.Context.SendActivityAsync(replyToActivity);
                                                        }
                                                        else
                                                        {
                                                            //Display the message if the date is current date
                                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                            {
                                                                //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                                                string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);

                                                                searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                                searchQuery.AdhocShiftDateFlag = "True";
                                                                List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                                                lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);


                                                                if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                                                {
                                                                    List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                                    foreach (var item in lstShiftByFacilityNextDay)
                                                                    {
                                                                        lstCardActionNextDay.Add(new CardAction()
                                                                        {
                                                                            Title = item.Shift.Insert(2, ":"),
                                                                            Type = ActionTypes.ImBack,
                                                                            Value = item.Shift.Insert(2, ":")
                                                                        });
                                                                    }

                                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                    tCard.Buttons = lstCardActionNextDay;
                                                                    searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                                    replyToActivity.Text = message;
                                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                }

                                                            }
                                                            else
                                                            {
                                                                await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                string eid = searchQuery.EnterpriseId;
                                                                searchQuery = new UserQuery();
                                                                searchQuery.EnterpriseId = eid; eid = null;
                                                                lstRaiseAdhoc = null;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                return await innerDc.EndDialogAsync();
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                        string eid = searchQuery.EnterpriseId;
                                                        searchQuery = new UserQuery();
                                                        searchQuery.EnterpriseId = eid; eid = null;
                                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                                    }
                                                }
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            }
                                            else
                                            {
                                                //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                replyToActivity.Text = Constants.validationMessageinAdhoc;
                                                ThumbnailCard tCard = new ThumbnailCard();

                                                tCard.Buttons = new List<CardAction>()
                                                {
                                                    new CardAction()
                                                    {
                                                        Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                    },
                                                     new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                    }
                                                };
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }

                                        }
                                        else
                                        {
                                            DateTime testAdhocdate;
                                            //To display the message for invalid formats
                                            if (!DateTime.TryParse(searchQuery.AdhocDateFormat, out testAdhocdate))
                                            {
                                                //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                replyToActivity.Attachments = new List<Attachment>();
                                                replyToActivity.Text = Constants.validationMessageInvalid;
                                                ThumbnailCard tCard = new ThumbnailCard();

                                                tCard.Buttons = new List<CardAction>()
                                                {
                                                    new CardAction()
                                                    {
                                                        Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                                    },
                                                    new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                    },
                                                     new CardAction()
                                                    {
                                                        Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                    }
                                                };
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }
                                            else
                                            {
                                                DateTime dt = Convert.ToDateTime(searchQuery.AdhocDateFormat);
                                                searchQuery.AdhocDate = dt.Date;
                                                //4 day window validation to raise request for pick / drop in raise adhoc
                                                if (Validate4daywindow(searchQuery.AdhocDate))
                                                {
                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                    searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;

                                                    // Fill Entity, Deal, EmpFacility & Gender
                                                    if (objRaiseAdhocData.objRaiseAdhoc != null)
                                                    {
                                                        searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                                        searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                                        searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                                        searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                                        searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                                    }

                                                    // Populate shifts to be choosen
                                                    if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                                                    {
                                                        List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                                                        DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                                        DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                                        // store to manipulate gating exclusion
                                                        searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                                        searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                                        List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                                        lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                                        if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                                        {
                                                            if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                                            {
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    istShiftDate = istShiftDate.AddHours(4);
                                                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                    lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                                }
                                                                else
                                                                {
                                                                    lstShiftByFacility = lstShiftByFacility.ToList();
                                                                }
                                                            }
                                                            else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                                            {
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    istShiftDate = istShiftDate.AddHours(2);
                                                                    int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                                    lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                                                }
                                                                else
                                                                {
                                                                    lstShiftByFacility = lstShiftByFacility.ToList();
                                                                }
                                                            }
                                                            else
                                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                                            if (lstShiftByFacility.Count > 0)
                                                            {
                                                                List<CardAction> lstCardAction = new List<CardAction>();

                                                                foreach (var item in lstShiftByFacility)
                                                                {
                                                                    lstCardAction.Add(new CardAction()
                                                                    {
                                                                        Title = item.Shift.Insert(2, ":"),
                                                                        Type = ActionTypes.ImBack,
                                                                        Value = item.Shift.Insert(2, ":")
                                                                    });
                                                                }

                                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                                replyToActivity.Attachments = new List<Attachment>();
                                                                ThumbnailCard tCard = new ThumbnailCard();
                                                                replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                                                tCard.Buttons = lstCardAction;
                                                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                replyToActivity.Attachments.Add(tCard.ToAttachment());

                                                                lstRaiseAdhoc = null;
                                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                                            }
                                                            else
                                                            {
                                                                //Display the message if the date is current date
                                                                if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                                                {
                                                                    //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                    // await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                                                    string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                                                    searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                                    searchQuery.AdhocShiftDateFlag = "True";
                                                                    List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                                                    lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                                                    if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                                                    {
                                                                        List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                                        foreach (var item in lstShiftByFacilityNextDay)
                                                                        {
                                                                            lstCardActionNextDay.Add(new CardAction()
                                                                            {
                                                                                Title = item.Shift.Insert(2, ":"),
                                                                                Type = ActionTypes.ImBack,
                                                                                Value = item.Shift.Insert(2, ":")
                                                                            });
                                                                        }

                                                                        ThumbnailCard tCard = new ThumbnailCard();
                                                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                                        tCard.Buttons = lstCardActionNextDay;
                                                                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                                        replyToActivity.Text = message;
                                                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                                    string eid = searchQuery.EnterpriseId;
                                                                    searchQuery = new UserQuery();
                                                                    searchQuery.EnterpriseId = eid; eid = null;
                                                                    lstRaiseAdhoc = null;
                                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                                    return await innerDc.EndDialogAsync();
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                            string eid = searchQuery.EnterpriseId;
                                                            searchQuery = new UserQuery();
                                                            searchQuery.EnterpriseId = eid; eid = null;
                                                            await PostToUserForAnyQuery(innerDc, searchQuery);
                                                        }
                                                    }
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                }
                                                else
                                                {
                                                    // await innerDc.Context.SendActivityAsync($"Sorry !. You can't raise the Adhoc request for choosen date");
                                                    // await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                                    replyToActivity.Attachments = new List<Attachment>();
                                                    replyToActivity.Text = Constants.validationMessageinAdhoc;
                                                    // replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                                                    ThumbnailCard tCard = new ThumbnailCard()
                                                    {
                                                        Buttons = new List<CardAction>()
                                                        {
                                                            new CardAction()
                                                            {
                                                                Title = "Today" ,
                                                                Type = ActionTypes.ImBack,
                                                                Value = "Today"

                                                            },
                                                            new CardAction()
                                                            {
                                                                Title = "Tomorrow" ,
                                                                Type = ActionTypes.ImBack,
                                                                Value = "Tomorrow"
                                                            },
                                                            new CardAction()
                                                            {
                                                                Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                                                Type = ActionTypes.ImBack,
                                                                Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                            },
                                                             new CardAction()
                                                            {
                                                                Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                                                Type = ActionTypes.ImBack,
                                                                Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                            },

                                                        }
                                                    };
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);


                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        replyToActivity.Text = Constants.validationMessageinAdhoc;
                                        // replyToActivity.Text = Constants.SelectDayForRaiseAdhoc;
                                        ThumbnailCard tCard = new ThumbnailCard()
                                        {
                                            Buttons = new List<CardAction>()
                                            {
                                                new CardAction()
                                                {
                                                    Title = "Today" ,
                                                    Type = ActionTypes.ImBack,
                                                    Value = "Today"

                                                },
                                                new CardAction()
                                                {
                                                    Title = "Tomorrow" ,
                                                    Type = ActionTypes.ImBack,
                                                    Value = "Tomorrow"
                                                },
                                                new CardAction()
                                                {
                                                    Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()),
                                                    Type = ActionTypes.ImBack,
                                                    Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                                },
                                                 new CardAction()
                                                {
                                                    Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()),
                                                    Type = ActionTypes.ImBack,
                                                    Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                                },

                                            }
                                        };
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                                        searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                    }
                                }
                                else
                                {
                                    //await innerDc.Context.SendActivityAsync(Constants.validationMessageinAdhoc);
                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                    replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                    replyToActivity.Attachments = new List<Attachment>();
                                    replyToActivity.Text = Constants.validationMessageInvalid;
                                    ThumbnailCard tCard = new ThumbnailCard();

                                    tCard.Buttons = new List<CardAction>()
                                    {
                                        new CardAction()
                                        {
                                            Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                                        },
                                        new CardAction()
                                        {
                                            Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                                        },
                                        new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                                        },
                                         new CardAction()
                                        {
                                            Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                                        }
                                    };
                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                    searchQuery.Context = Constants.SelectDayForRaiseAdhoc;
                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                                    await innerDc.Context.SendActivityAsync(replyToActivity);

                                }
                            }
                            //To validate date format
                            else
                            {
                                if (searchQuery.AdhocDay.ToLower().Equals(Constants.day1.ToLower()))
                                {
                                    searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.ToShortDateString());
                                    searchQuery.AdhocShiftDateFlag = "True";
                                }
                                if (searchQuery.AdhocDay.ToLower().Equals(Constants.day2.ToLower()))
                                {
                                    searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(1).ToShortDateString());
                                    searchQuery.AdhocShiftDateFlag = "True";
                                }
                                if (searchQuery.AdhocDay.ToLower().Equals(Constants.day3.ToLower()))
                                {
                                    searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(2).ToShortDateString());
                                    searchQuery.AdhocShiftDateFlag = "True";
                                }
                                if (searchQuery.AdhocDay.ToLower().Equals(Constants.day4.ToLower()))
                                {
                                    searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(3).ToShortDateString());
                                    searchQuery.AdhocShiftDateFlag = "True";
                                }

                                searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;

                                // Fill Entity, Deal, EmpFacility & Gender
                                if (objRaiseAdhocData.objRaiseAdhoc != null)
                                {
                                    searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                    searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                    searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                    searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility; ;
                                    searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                                }

                                // Populate shifts to be choosen
                                if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                                {
                                    List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                                    DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                    DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                    // store to manipulate gating exclusion
                                    searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                    searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                    List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                    lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                    if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                    {
                                        if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                        {
                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                            {
                                                istShiftDate = istShiftDate.AddHours(4);
                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                            }
                                            else
                                            {
                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                            }
                                        }
                                        else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                        {
                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                            {
                                                istShiftDate = istShiftDate.AddHours(2);
                                                int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                                lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                            }
                                            else
                                            {
                                                lstShiftByFacility = lstShiftByFacility.ToList();
                                            }
                                        }
                                        else
                                            lstShiftByFacility = lstShiftByFacility.ToList();

                                        if (lstShiftByFacility.Count > 0)
                                        {
                                            List<CardAction> lstCardAction = new List<CardAction>();

                                            foreach (var item in lstShiftByFacility)
                                            {
                                                lstCardAction.Add(new CardAction()
                                                {
                                                    Title = item.Shift.Insert(2, ":"),
                                                    Type = ActionTypes.ImBack,
                                                    Value = item.Shift.Insert(2, ":")
                                                });
                                            }

                                            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                            replyToActivity.Attachments = new List<Attachment>();
                                            ThumbnailCard tCard = new ThumbnailCard();
                                            replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                            tCard.Buttons = lstCardAction;
                                            searchQuery.Context = Constants.SelectShiftForAdhoc;
                                            replyToActivity.Attachments.Add(tCard.ToAttachment());
                                            lstRaiseAdhoc = null;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            await innerDc.Context.SendActivityAsync(replyToActivity);

                                        }
                                        else
                                        {
                                            //Display the message if the date is current date
                                            if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                            {
                                                //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                                string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                                searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                                searchQuery.AdhocShiftDateFlag = "True";
                                                List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                                lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                                if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                                {
                                                    List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                    foreach (var item in lstShiftByFacilityNextDay)
                                                    {
                                                        lstCardActionNextDay.Add(new CardAction()
                                                        {
                                                            Title = item.Shift.Insert(2, ":"),
                                                            Type = ActionTypes.ImBack,
                                                            Value = item.Shift.Insert(2, ":")
                                                        });
                                                    }

                                                    ThumbnailCard tCard = new ThumbnailCard();
                                                    Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                    tCard.Buttons = lstCardActionNextDay;
                                                    searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                    replyToActivity.Text = message;
                                                    replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                    await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                    await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                    await innerDc.Context.SendActivityAsync(replyToActivity);
                                                }

                                            }
                                            else
                                            {
                                                await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                                string eid = searchQuery.EnterpriseId;
                                                searchQuery = new UserQuery();
                                                searchQuery.EnterpriseId = eid; eid = null;
                                                lstRaiseAdhoc = null;
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                return await innerDc.EndDialogAsync();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                        string eid = searchQuery.EnterpriseId;
                                        searchQuery = new UserQuery();
                                        searchQuery.EnterpriseId = eid; eid = null;
                                        await PostToUserForAnyQuery(innerDc, searchQuery);
                                    }
                                }
                            }
                        }
                        else
                        {
                            searchQuery.AdhocDate = searchQuery.AdhocShiftDateFlag != null ? searchQuery.AdhocDate : DateTime.Now;
                            // Fill Entity, Deal, EmpFacility & Gender
                            if (objRaiseAdhocData.objRaiseAdhoc != null)
                            {
                                searchQuery.Entity = objRaiseAdhocData.objRaiseAdhoc.Entity;
                                searchQuery.Gender = objRaiseAdhocData.objRaiseAdhoc.Gender;
                                searchQuery.Deal = objRaiseAdhocData.objRaiseAdhoc.Deal;
                                searchQuery.AdhocFacility = string.IsNullOrEmpty(searchQuery.AdhocFacility) ? objRaiseAdhocData.objRaiseAdhoc.FacilityId : searchQuery.AdhocFacility;
                                searchQuery.EmployeeAssignFacility = objRaiseAdhocData.objRaiseAdhoc.FacilityId;
                            }

                            // Populate shifts to be choosen
                            if (objRaiseAdhocData.lstRaiseAdhoc != null && objRaiseAdhocData.lstRaiseAdhoc.Count > 0)
                            {
                                List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();

                                DateTime utcShiftDate = TimeZoneInfo.ConvertTimeToUtc(searchQuery.AdhocDate);
                                DateTime istShiftDate = TimeZoneInfo.ConvertTimeFromUtc(utcShiftDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                                // store to manipulate gating exclusion
                                searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                                searchQuery.lstShiftCallData.AddRange(objRaiseAdhocData.lstRaiseAdhoc);

                                List<RaiseAdhoc> lstShiftByFacility = new List<RaiseAdhoc>();
                                lstShiftByFacility = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);
                                if (lstShiftByFacility != null && lstShiftByFacility.Count > 0)
                                {
                                    if (searchQuery.AdhocType.Trim().ToLower().Equals("pick"))
                                    {
                                        if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                        {
                                            istShiftDate = istShiftDate.AddHours(4);
                                            int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                            lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                        }
                                        else
                                        {
                                            lstShiftByFacility = lstShiftByFacility.ToList();
                                        }
                                    }
                                    else if (searchQuery.AdhocType.Trim().ToLower().Equals("drop"))
                                    {
                                        if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                        {
                                            istShiftDate = istShiftDate.AddHours(2);
                                            int currentShift = Convert.ToInt32(istShiftDate.ToString("HHmm"));
                                            lstShiftByFacility = lstShiftByFacility.Where(t => Convert.ToInt32(t.Shift) > (currentShift)).ToList();
                                        }
                                        else
                                        {
                                            lstShiftByFacility = lstShiftByFacility.ToList();
                                        }
                                    }

                                    if (lstShiftByFacility.Count > 0)
                                    {
                                        List<CardAction> lstCardAction = new List<CardAction>();

                                        foreach (var item in lstShiftByFacility)
                                        {
                                            lstCardAction.Add(new CardAction()
                                            {
                                                Title = item.Shift.Insert(2, ":"),
                                                Type = ActionTypes.ImBack,
                                                Value = item.Shift.Insert(2, ":")
                                            });
                                        }

                                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        replyToActivity.Attachments = new List<Attachment>();
                                        ThumbnailCard tCard = new ThumbnailCard();
                                        replyToActivity.Text = Constants.SelectShiftForAdhoc;
                                        tCard.Buttons = lstCardAction;
                                        searchQuery.Context = Constants.SelectShiftForAdhoc;
                                        replyToActivity.Attachments.Add(tCard.ToAttachment());

                                        lstRaiseAdhoc = null;
                                        await innerDc.Context.SendActivityAsync(replyToActivity);
                                    }
                                    else
                                    {
                                        //await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                        //Display the message if the date is current date
                                        if (searchQuery.AdhocDate.Date == DateTime.Now.Date)
                                        {
                                            //await innerDc.Context.SendActivityAsync($"Too Late! I cannot raise a {searchQuery.AdhocType} adhoc request for today as no shifts are applicable. But you can raise {searchQuery.AdhocType} adhoc for tomorrow by choosing one of the below shift timings:");
                                            string message = string.Format("Too Late!I cannot raise a {0} adhoc request for today as no shifts are applicable. But you can raise {0} adhoc for tomorrow by choosing one of the below shift timings:", searchQuery.AdhocType);
                                            searchQuery.AdhocDate = DateTime.Now.AddDays(1);
                                            searchQuery.AdhocShiftDateFlag = "True";
                                            List<RaiseAdhoc> lstShiftByFacilityNextDay = new List<RaiseAdhoc>();
                                            lstShiftByFacilityNextDay = await objRaiseAdhocService.PopulateShiftsByFacility(searchQuery, _config);

                                            if (lstShiftByFacilityNextDay != null && lstShiftByFacilityNextDay.Count > 0)
                                            {
                                                List<CardAction> lstCardActionNextDay = new List<CardAction>();

                                                foreach (var item in lstShiftByFacilityNextDay)
                                                {
                                                    lstCardActionNextDay.Add(new CardAction()
                                                    {
                                                        Title = item.Shift.Insert(2, ":"),
                                                        Type = ActionTypes.ImBack,
                                                        Value = item.Shift.Insert(2, ":")
                                                    });
                                                }

                                                ThumbnailCard tCard = new ThumbnailCard();
                                                Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                                                tCard.Buttons = lstCardActionNextDay;
                                                searchQuery.Context = Constants.SelectShiftForAdhoc;
                                                replyToActivity.Text = message;
                                                replyToActivity.Attachments.Add(tCard.ToAttachment());
                                                await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                                await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                                await innerDc.Context.SendActivityAsync(replyToActivity);
                                            }

                                        }
                                        else
                                        {
                                            await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                            string eid = searchQuery.EnterpriseId;
                                            searchQuery = new UserQuery();
                                            searchQuery.EnterpriseId = eid; eid = null;
                                            lstRaiseAdhoc = null;
                                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                                            return await innerDc.EndDialogAsync();
                                        }

                                    }
                                }
                                else
                                {
                                    await innerDc.Context.SendActivityAsync(Constants.NoShift);
                                    string eid = searchQuery.EnterpriseId;
                                    searchQuery = new UserQuery();
                                    searchQuery.EnterpriseId = eid; eid = null;
                                    await PostToUserForAnyQuery(innerDc, searchQuery);
                                }
                            }
                            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
                        }

                    }
                }
                else if (searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInter_Office.ToLower()) || searchQuery.AdhocType.ToLower().Equals(Constants.AdhocTypeInterOffice.ToLower()))
                {
                    if (!string.IsNullOrEmpty(searchQuery.AdhocDay))
                    {
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day1.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day2.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(1).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day3.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(2).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }
                        if (searchQuery.AdhocDay.ToLower().Equals(Constants.day4.ToLower()))
                        {
                            searchQuery.AdhocDate = Convert.ToDateTime(DateTime.Today.AddDays(3).ToShortDateString());
                            searchQuery.AdhocShiftDateFlag = "True";
                        }

                        searchQuery.AdhocDate = searchQuery.AdhocDate.Date == DateTime.Now.Date ? DateTime.Now : searchQuery.AdhocDate;

                    }

                    List<RaiseAdhoc> lstRaiseAdhoc = new List<RaiseAdhoc>();
                    lstRaiseAdhoc = await objRaiseAdhocService.GetAllFacility(searchQuery, _config);

                    if (lstRaiseAdhoc.Count > 0)
                    {
                        searchQuery.lstShiftCallData = new List<RaiseAdhoc>();
                        searchQuery.lstShiftCallData.AddRange(lstRaiseAdhoc);
                        List<CardAction> lstCardAction = new List<CardAction>();

                        foreach (var item in lstRaiseAdhoc)
                        {
                            lstCardAction.Add(new CardAction()
                            {
                                Title = item.FacilityId,
                                Type = ActionTypes.ImBack,
                                Value = item.FacilityId
                            });
                        }

                        Activity replyToActivity = innerDc.Context.Activity.CreateReply();
                        replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                        replyToActivity.Attachments = new List<Attachment>();
                        ThumbnailCard tCard = new ThumbnailCard();
                        replyToActivity.Text = Constants.SelectSourceFaility;
                        tCard.Buttons = lstCardAction;
                        searchQuery.Context = Constants.SelectSourceFaility;
                        replyToActivity.Attachments.Add(tCard.ToAttachment());
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                        await innerDc.Context.SendActivityAsync(replyToActivity);
                    }
                    else
                    {
                        await innerDc.Context.SendActivityAsync(Constants.NoFacilityFound);
                        await innerDc.Context.SendActivityAsync(Constants.AssignYourSelfFacility);
                        string eid = searchQuery.EnterpriseId;
                        searchQuery = new UserQuery();
                        searchQuery.EnterpriseId = eid; eid = null;
                        await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, searchQuery);
                        await _accessors.UserState.SaveChangesAsync(innerDc.Context);

                        return await innerDc.EndDialogAsync();
                    }


                }
                //Check
            }
            #endregion

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
            userQuery.Context = $"{MessageStatements.AnythingElseMessage} in raise adhoc";
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);
            replyToActivity.Attachments.Add(tCard.ToAttachment());

            await innerDc.Context.SendActivityAsync(replyToActivity);
        }

        private async Task ValidateWeekDay(DialogContext innerDc, UserQuery userQuery)
        {
            Activity replyToActivity = innerDc.Context.Activity.CreateReply();
            replyToActivity.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToActivity.Attachments = new List<Attachment>();
            replyToActivity.Text = Constants.validationMessageInvalid;
            ThumbnailCard tCard = new ThumbnailCard();

            tCard.Buttons = new List<CardAction>()
            {
                new CardAction()
                {
                    Title = "Today", Type = ActionTypes.ImBack, Value = "Today"
                },
                new CardAction()
                {
                    Title = "Tomorrow", Type = ActionTypes.ImBack, Value = "Tomorrow"
                },
                new CardAction()
                {
                    Title = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(2).ToShortDateString())
                },
                new CardAction()
                {
                    Title = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString()), Type = ActionTypes.ImBack, Value = Convert.ToString(DateTime.Today.AddDays(3).ToShortDateString())
                }
            };
            replyToActivity.Attachments.Add(tCard.ToAttachment());
            userQuery.Context = Constants.SelectDayForRaiseAdhoc;
            await _accessors.UserQueryAccessor.SetAsync(innerDc.Context, userQuery);
            await _accessors.UserState.SaveChangesAsync(innerDc.Context);

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