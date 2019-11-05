// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = @"こんにちは、ヘルプデスクチャットボットです。" +
                                        "\n\n" +
                                       @"会社の問題に答えます。" +
                                       "\n\n" +
                                       @"サンプルの質問: アウトソーシングとはどのような意味ですか" +
                                       "\n\n" +
                                       @"参照リンク: https://www.noc-net.co.jp/faq/";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeText), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            const string NoAnser = @"申し訳ありませんが、システムにはその質問に対する正しい答えがありません。";
            var httpClient = _httpClientFactory.CreateClient();
            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAAuthKey"],
                Host = GetHostname()
            },
            new QnAMakerOptions
            {
                Top = 3
            },
            httpClient);

            _logger.LogInformation("Calling QnA Maker");

            // The actual call to the QnA Maker service.
            var responses = await qnaMaker.GetAnswersAsync(turnContext);
            if (responses != null && responses.Length > 0)
            {
                if (responses[0].Score * 100 >= 100)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(responses[0].Answer), cancellationToken);
                }
                else
                {
                    var card = new HeroCard()
                    {
                        Text = "以下が聞きたい質問ですか？",
                        Buttons = new List<CardAction>()
                    };
                    foreach (var response in responses)
                    {
                        card.Buttons.Add(new CardAction(type: ActionTypes.ImBack, title: response.Questions[0], value: response.Questions[0]));
                    }
                    var reply = MessageFactory.Attachment(card.ToAttachment());
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(NoAnser), cancellationToken);
            }
        }

        //private static async Task DisplayOptionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        //{
        //    // Create a HeroCard with options for the user to interact with the bot.
        //    var card = new HeroCard()
        //    {
        //        Text = "You can upload an image or select one of the following choices",
        //        Buttons = new List<CardAction>
        //        {
        //            // Note that some channels require different values to be used in order to get buttons to display text.
        //            // In this code the emulator is accounted for with the 'title' parameter, but in other channels you may
        //            // need to provide a value for other parameters like 'text' or 'displayText'.
        //            new CardAction(ActionTypes.ImBack, title: "1. Inline Attachment", value: "1"),
        //            new CardAction(ActionTypes.ImBack, title: "2. Internet Attachment", value: "2"),
        //            new CardAction(ActionTypes.ImBack, title: "3. Uploaded Attachment", value: "3"),
        //        },
        //    };

        //    var reply = MessageFactory.Attachment(card.ToAttachment());
        //    await turnContext.SendActivityAsync(reply, cancellationToken);
        //}


        private string GetHostname()
        {
            var hostname = _configuration["QnAEndpointHostName"];
            if (!hostname.StartsWith("https://"))
            {
                hostname = string.Concat("https://", hostname);
            }

            if (!hostname.EndsWith("/qnamaker"))
            {
                hostname = string.Concat(hostname, "/qnamaker");
            }

            return hostname;
        }
    }
}
