using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.BotCore.Sample.EchoBot
{
    [ServiceFilter(typeof(BotAuthentication))]
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [HttpPost]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            if (activity != null)
            {
                // one of these will have an interface and process it
                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        //await Conversation.SendAsync(activity, () => new EchoDialog());
                        //await Conversation.SendAsync(activity, () => EchoCommandDialog.dialog);
                        //await Conversation.SendAsync(activity, () => new EchoAttachmentDialog());
                        
                        
                        //await Conversation.SendAsync(activity, () => EchoChainDialog.dialog);
                        break;

                    case ActivityTypes.ConversationUpdate:
                        IConversationUpdateActivity update = activity;
                        //using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                        //{
                        //    var client = scope.Resolve<IConnectorClient>();
                        //    if (update.MembersAdded.Any())
                        //    {
                        //        var reply = activity.CreateReply();
                        //        foreach (var newMember in update.MembersAdded)
                        //        {
                        //            if (newMember.Id != activity.Recipient.Id)
                        //            {
                        //                reply.Text = $"Welcome {newMember.Name}!";
                        //            }
                        //            else
                        //            {
                        //                reply.Text = $"Welcome {activity.From.Name}";
                        //            }
                        //            await client.Conversations.ReplyToActivityAsync(reply);
                        //        }
                        //    }
                        //}
                        break;
                    case ActivityTypes.ContactRelationUpdate:
                    case ActivityTypes.Typing:
                    case ActivityTypes.DeleteUserData:
                    case ActivityTypes.Ping:
                    default:
                        //LOG: Trace.TraceError($"Unknown activity type ignored: {activity.GetActivityType()}");
                        break;
                }
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
    }
}
