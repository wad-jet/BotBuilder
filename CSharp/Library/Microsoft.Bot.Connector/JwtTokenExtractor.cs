﻿using System;
using System.Collections.Generic;
//using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.Bot.Connector
{
    public class JwtTokenExtractor
    {
        /// <summary>
        /// Shared of OpenIdConnect configuration managers (one per metadata URL)
        /// </summary>
        private static readonly Dictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _openIdMetadataCache =
            new Dictionary<string, ConfigurationManager<OpenIdConnectConfiguration>>();

        /// <summary>
        /// Token validation parameters for this instance
        /// </summary>
        private readonly TokenValidationParameters _tokenValidationParameters;

        /// <summary>
        /// OpenIdConnect configuration manager for this instances
        /// </summary>
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _openIdMetadata;

        public JwtTokenExtractor(TokenValidationParameters tokenValidationParameters, string metadataUrl)
        {
            // Make our own copy so we can edit it
            _tokenValidationParameters = tokenValidationParameters.Clone();

            //TODO: Проверить, может не работать
            var configRetriever = new OpenIdConnectConfigurationRetriever();

            if (!_openIdMetadataCache.ContainsKey(metadataUrl)) 
                _openIdMetadataCache[metadataUrl] = new ConfigurationManager<OpenIdConnectConfiguration>(metadataUrl, configRetriever);

            _openIdMetadata = _openIdMetadataCache[metadataUrl];

            _tokenValidationParameters.ValidateAudience = true;
            _tokenValidationParameters.RequireSignedTokens = true;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(HttpRequestMessage request)
        {
            if (request.Headers.Authorization != null)
                return await GetIdentityAsync(request.Headers.Authorization.Scheme, request.Headers.Authorization.Parameter).ConfigureAwait(false);
            return null;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(string authorizationHeader)
        {
            if (authorizationHeader == null)
                return null;

            string[] parts = authorizationHeader?.Split(' ');
            if (parts.Length == 2)
                return await GetIdentityAsync(parts[0], parts[1]).ConfigureAwait(false);
            return null;
        }

        public async Task<ClaimsIdentity> GetIdentityAsync(string scheme, string parameter)
        {
            // No header in correct scheme?
            if (scheme != "Bearer")
                return null;

            // Issuer isn't allowed? No need to check signature
            if (!HasAllowedIssuer(parameter))
                return null;

            try
            {
                ClaimsPrincipal claimsPrincipal = await ValidateTokenAsync(parameter).ConfigureAwait(false);
                return claimsPrincipal.Identities.OfType<ClaimsIdentity>().FirstOrDefault();
            }
            catch (Exception e)
            {
                //LOG: Trace.TraceWarning("Invalid token. " + e.ToString());
                return null;
            }
        }

        public void GenerateUnauthorizedResponse(ActionExecutingContext actionContext)
        {
            string host = new Uri(actionContext.HttpContext.Request.Host.ToUriComponent()).DnsSafeHost;
            actionContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            actionContext.HttpContext.Response.Headers.Add("WWW-Authenticate", string.Format("Bearer realm=\"{0}\"", host));
            return;
        }

        private bool HasAllowedIssuer(string jwtToken)
        {
            JwtSecurityToken token = new JwtSecurityToken(jwtToken);
            if (_tokenValidationParameters.ValidIssuer != null && _tokenValidationParameters.ValidIssuer == token.Issuer)
                return true;

            if ((_tokenValidationParameters.ValidIssuers ?? Enumerable.Empty<string>()).Contains(token.Issuer))
                return true;

            return false;
        }

        public string GetBotIdFromClaimsIdentity(ClaimsIdentity identity)
        {
            if (identity == null)
                return null;

            Claim botClaim = identity.Claims.FirstOrDefault(c => _tokenValidationParameters.ValidIssuers.Contains(c.Issuer) && c.Type == "appid");
            if (botClaim != null)
                return botClaim.Value;

            // Fallback for BF-issued tokens
            botClaim = identity.Claims.FirstOrDefault(c => c.Issuer == "https://api.botframework.com" && c.Type == "aud");
            if (botClaim != null)
                return botClaim.Value;

            return null;
        }

        private async Task<ClaimsPrincipal> ValidateTokenAsync(string jwtToken)
        {
            // _openIdMetadata only does a full refresh when the cache expires every 5 days
            OpenIdConnectConfiguration config = null;
            try
            {
                config = await _openIdMetadata.GetConfigurationAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //LOG: Trace.TraceError($"Error refreshing OpenId configuration: {e}");

                // No config? We can't continue
                if (config == null)
                    throw;
            }

            // Update the signing tokens from the last refresh
            _tokenValidationParameters.IssuerSigningKeys = config.SigningKeys;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                SecurityToken parsedToken;
                ClaimsPrincipal principal = tokenHandler.ValidateToken(jwtToken, _tokenValidationParameters, out parsedToken);
                return principal;
            }
            catch (SecurityTokenSignatureKeyNotFoundException)
            {
                string keys = string.Join(", ", ((config?.SigningKeys) ?? Enumerable.Empty<SecurityKey>()).Select(t => t.KeyId));
                //LOG: Trace.TraceError("Error finding key for token. Available keys: " + keys);
                throw;
            }
        }
    }
}