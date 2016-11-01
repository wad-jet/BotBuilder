using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Bot.Connector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class BotAuthentication : ActionFilterAttribute
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string MicrosoftAppId { get; set; }
        public string MicrosoftAppIdSettingName { get; set; }
        public bool DisableSelfIssuedTokens { get; set; }
        public virtual string OpenIdConfigurationUrl { get; set; } = JwtConfig.ToBotFromChannelOpenIdMetadataUrl;

        public BotAuthentication(IConfigurationRoot configuration, IHttpContextAccessor httpContextAccessor)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (httpContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
        {         
            MicrosoftAppId = MicrosoftAppId ?? _configuration[$"{MicrosoftAppIdSettingName}:MicrosoftAppId"];

            if (Debugger.IsAttached && String.IsNullOrEmpty(MicrosoftAppId))
                // then auth is disabled
                return;

            var tokenExtractor = new JwtTokenExtractor(JwtConfig.GetToBotFromChannelTokenValidationParameters(MicrosoftAppId), OpenIdConfigurationUrl);

            //TODO: Надо проверить!
            var identity = await tokenExtractor.GetIdentityAsync(actionContext.HttpContext.Request.Headers["WWW-Authenticate"]);

            // No identity? If we're allowed to, fall back to MSA
            // This code path is used by the emulator
            if (identity == null && !DisableSelfIssuedTokens)
            {
                tokenExtractor = new JwtTokenExtractor(JwtConfig.ToBotFromMSATokenValidationParameters, JwtConfig.ToBotFromMSAOpenIdMetadataUrl);

                //TODO: Надо проверить!
                identity = await tokenExtractor.GetIdentityAsync(actionContext.HttpContext.Request.Headers["WWW-Authenticate"]);

                // Check to make sure the app ID in the token is ours
                if (identity != null)
                {
                    // If it doesn't match, throw away the identity
                    if (tokenExtractor.GetBotIdFromClaimsIdentity(identity) != MicrosoftAppId)
                        identity = null;
                }
            }

            // Still no identity? Fail out.
            if (identity == null)
            {
                tokenExtractor.GenerateUnauthorizedResponse(actionContext);
                return;
            }

            var activity = actionContext.ActionArguments.Select(t => t.Value).OfType<Activity>().FirstOrDefault();
            if (activity != null)
            {
                MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl);
            }
            else
            {
                // No model binding to activity check if we can find JObject or JArray
                var obj = actionContext.ActionArguments.Where(t => t.Value is JObject || t.Value is JArray).Select(t => t.Value).FirstOrDefault();
                if (obj != null)
                {
                    Activity[] activities = (obj is JObject) ? new Activity[] { ((JObject)obj).ToObject<Activity>() } : ((JArray)obj).ToObject<Activity[]>();
                    foreach (var jActivity in activities)
                    {
                        if (!string.IsNullOrEmpty(jActivity.ServiceUrl))
                        {
                            MicrosoftAppCredentials.TrustServiceUrl(jActivity.ServiceUrl);
                        }
                    }
                }
                else
                {
                    //LOG: Trace.TraceWarning("No activity in the Bot Authentication Action Arguments");
                }
            }

            //Thread.CurrentPrincipal = new ClaimsPrincipal(identity);

            // Inside of ASP.NET this is required
            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.User = new ClaimsPrincipal(identity);
            }

            base.OnActionExecuting(actionContext);
        }
    }
}
