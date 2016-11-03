using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.BotCore.Sample.EchoBot
{
    [ServiceFilter(typeof(BotAuthentication))]
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;

        public MessagesController(IConfigurationRoot configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }
            if (memoryCache == null)
            {
                throw new ArgumentNullException(nameof(memoryCache));
            }
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
        }

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
                IConnectorClientFactory factory = new ConnectorClientFactory(Address.FromActivity(activity), new MicrosoftAppCredentials(_configuration, _httpContextAccessor, _memoryCache));                
                
                // one of these will have an interface and process it
                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        
                        using (IConnectorClient connectorClient = factory.MakeConnectorClient())
                        {
                            IBotToUser botToUser = new AlwaysSendDirect_BotToUser(activity, connectorClient);
                            await botToUser.PostAsync($"HELLO, {activity.From.Name}!");

                            // .. OR NOT HELPER EXTENSION: 
                            //IMessageActivity msgActivity = botToUser.MakeMessage();
                            //msgActivity.Text = "HELLO!!! HI!";                        
                            //await botToUser.PostAsync(msgActivity);
                        }

                        break;

                    case ActivityTypes.ConversationUpdate:
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
