using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Accenture.CIO.WPBot.Core;
using Accenture.CIO.WPBot.Logger;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accenture.CIO.WPBot.Core.BotState;

namespace Accenture.CIO.WPBot
{
    public class QnAHandlerDialog : ComponentDialog
    {
        private ILoggerRepository<SqlLoggerRepository> _sqlLoggerRepository;
        private IStatePropertyAccessor<PrevActivityState> _prevActivityAccessor;
        private IConfiguration _configuration;
        private readonly StateBotAccessors _accessors;

        public QnAHandlerDialog(ILoggerRepository<SqlLoggerRepository> sqlLoggerRepository, StateBotAccessors accessors , IConfiguration configuration)
            : base(nameof(QnAHandlerDialog))
        {
            _sqlLoggerRepository = sqlLoggerRepository;
            _accessors = accessors;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool isResponded = false;
            string response = string.Empty;
            // QnA Service
            _configuration.CheckKeyVault(_configuration["QnA:EndpointKey"]);
            var qnaEndpoint = new QnAMakerEndpoint()
            {
                KnowledgeBaseId = _configuration["QnA:KbId"],
                EndpointKey = _configuration[_configuration["QnA:EndpointKey"]],
                Host = _configuration["QnA:Hostname"],
            };
            QnAMakerOptions qnaOptions = float.TryParse(_configuration["QnA:Threshold"], out float scoreThreshold)
                ? new QnAMakerOptions { ScoreThreshold = scoreThreshold, Top = 1, }
                : null;

            var qnaMaker = new QnAMaker(qnaEndpoint, qnaOptions, null);
            QueryResult[] answer = await qnaMaker.GetAnswersAsync(innerDc.Context);
            if (answer != null && answer.Length > 0)
            {
                response = Regex.Replace(answer.FirstOrDefault().Answer, Utilities.GetResourceMessage(Constants.FirstNamePlaceHolder), innerDc.Context.Activity.From.Name, RegexOptions.IgnoreCase);
                await innerDc.Context.SendActivityAsync(response.Trim());
                await innerDc.Context.AskUserFeedbackAsync(_prevActivityAccessor);
                isResponded = true;
            }
            TaskResult taskResult = new TaskResult()
            {
                Category = CategoryType.QnA,
                ModelName = _configuration["QnA:Name"],
                Intent = string.Empty,
                Entity = string.Empty,
                Response = response,
                ResponseType = BotResponseType.ValidResponse,
                Score = (answer == null || answer.Length ==0) ? 0 : answer.FirstOrDefault().Score,
                Source = string.IsNullOrEmpty(response) ? CategoryType.QnA : CategoryType.BotResponse
            };
            await _sqlLoggerRepository.InsertBotLogAsync(innerDc.Context.Activity, taskResult);
            return await innerDc.EndDialogAsync(result: isResponded);
        }
    }
}
