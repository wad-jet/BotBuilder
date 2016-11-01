﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Bot.Connector
{
    public partial class StateClient
    {
        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <param name="baseUri">Base URI for the State service</param>
        /// <param name="microsoftAppId">Optional. Your Microsoft app id. If null, this setting is read from settings["MicrosoftAppId"]</param>
        /// <param name="microsoftAppPassword">Optional. Your Microsoft app password. If null, this setting is read from settings["MicrosoftAppPassword"]</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        public StateClient(IConfigurationRoot configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, Uri baseUri, string microsoftAppId = null, string microsoftAppPassword = null, params DelegatingHandler[] handlers)
            : this(baseUri, new MicrosoftAppCredentials(configuration, httpContextAccessor, memoryCache, microsoftAppId, microsoftAppPassword), handlers: handlers)
        {
        }

        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <param name="baseUri">Base URI for the State service</param>
        /// <param name="credentials">Credentials for the Connector service</param>
        /// <param name="addJwtTokenRefresher">True, if JwtTokenRefresher should be included; False otherwise.</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        public StateClient(Uri baseUri, MicrosoftAppCredentials credentials, bool addJwtTokenRefresher = true, params DelegatingHandler[] handlers)
            : this(baseUri, addJwtTokenRefresher ? AddJwtTokenRefresher(handlers, credentials) : handlers)
        {
            this.Credentials = credentials;
        }

        /// <summary>
        /// Create a new instance of the StateClient class
        /// </summary>
        /// <remarks> This constructor will use https://state.botframework.com as the baseUri</remarks>
        /// <param name="credentials">Credentials for the Connector service</param>
        /// <param name="addJwtTokenRefresher">True, if JwtTokenRefresher should be included; False otherwise.</param>
        /// <param name="handlers">Optional. The delegating handlers to add to the http client pipeline.</param>
        public StateClient(MicrosoftAppCredentials credentials, bool addJwtTokenRefresher = true, params DelegatingHandler[] handlers)
            : this(addJwtTokenRefresher ? AddJwtTokenRefresher(handlers, credentials) : handlers)
        {
            this.Credentials = credentials;
        }

        private static DelegatingHandler[] AddJwtTokenRefresher(DelegatingHandler[] srcHandlers, MicrosoftAppCredentials credentials)
        {
            var handlers = new List<DelegatingHandler>(srcHandlers);
            handlers.Add(new JwtTokenRefresher(credentials));
            return handlers.ToArray();
        }
    }
}
