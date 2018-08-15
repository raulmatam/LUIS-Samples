namespace LuisBot
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Configuration;
    using System.Web.Http;
    using Dialogs;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Services;
    using System.Globalization;
    using System.Web.Http.Description;
    using System.Threading;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private static readonly bool IsSpellCorrectionEnabled = bool.Parse(WebConfigurationManager.AppSettings["IsSpellCorrectionEnabled"]);

        private readonly BingSpellCheckService spellService = new BingSpellCheckService();

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                if (IsSpellCorrectionEnabled)
                {
                    try
                    {
                        activity.Text = await this.spellService.GetCorrectedTextAsync(activity.Text);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }

                await Conversation.SendAsync(activity, () => new RootLuisDialog());
            }
            else
            {
                this.HandleSystemMessageAsync(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                IConversationUpdateActivity iConversationUpdated = message as IConversationUpdateActivity;
                if (iConversationUpdated != null)
                {
                    ConnectorClient connector = new ConnectorClient(new System.Uri(message.ServiceUrl));
                    foreach (var member in iConversationUpdated.MembersAdded ?? System.Array.Empty<ChannelAccount>())
                    {
                        // if the bot is added, then   
                        if (member.Id == iConversationUpdated.Recipient.Id)
                        {

                            // Saludos dependiendo la hora
                            Thread.CurrentThread.CurrentCulture = new CultureInfo("es-MX");
                            Int32 hora = DateTime.Now.Hour;

                            /*if (hora < 12)
                            {
                                var reply = ((Activity)iConversationUpdated).CreateReply($"¡Hola! Buenos días.");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }
                            if (hora < 19)
                            {
                                var reply = ((Activity)iConversationUpdated).CreateReply($"¡Hola! Buena tarde.");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }
                            if (hora < 24)
                            {
                                var reply = ((Activity)iConversationUpdated).CreateReply($"¡Hola! Buena noche.");
                                await connector.Conversations.ReplyToActivityAsync(reply);
                            }*/

                            string nombre = "Luis Raúl";
                                                       
                            var reply = ((Activity)iConversationUpdated).CreateReply($"¡Hola {nombre}! Buenos días.");
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }




            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}