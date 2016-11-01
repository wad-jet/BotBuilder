using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Bot.Connector
{
    public static class HttpClientEx
    {
        /// <summary>
        /// add Bearer authorization token for making API calls
        /// </summary>
        /// <param name="client">The http client</param>
        /// <param name="appId">(default)Setting["microsoftAppId"]</param>
        /// <param name="password">(default)Setting["microsoftAppPassword"]</param>
        /// <returns>HttpClient with Bearer Authorization header</returns>
        public static async Task AddAPIAuthorization(this HttpClient client, IConfigurationRoot configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, string appId = null, string password = null)
        {
            var token = await new MicrosoftAppCredentials(configuration, httpContextAccessor, memoryCache, appId, password).GetTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
