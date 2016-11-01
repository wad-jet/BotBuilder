using Microsoft.Bot.Builder.Calling.Exceptions;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Bot.Builder.Calling
{
    public class CallingBotServiceSettings
    {
        private readonly IConfigurationRoot _configuration;

        /// <summary>
        /// The url where the Callingcallbacks from Skype Bot platform will be sent. Needs to match the domain name of service and the route configured in BotController.
        /// For example "https://testservice.azurewebsites.net/api/calling/callback"   
        /// </summary>
        public string CallbackUrl { get; set; }

        //public CallingBotServiceSettings(IConfigurationRoot configuration)
        //{
        //    if (configuration == null)
        //    {
        //        throw new ArgumentNullException(nameof(configuration));
        //    }
        //    _configuration = configuration;
        //}

        /// <summary>
        /// Loads core bot library configuration from the cloud service configuration
        /// </summary>
        /// <returns>MessagingBotServiceSettings</returns>
        public static CallingBotServiceSettings LoadFromCloudConfiguration(IConfigurationRoot configuration)
        {
            CallingBotServiceSettings settings;

            try
            {
                settings = new CallingBotServiceSettings
                {
                    //TODO: точно ли App:Settings:... ??
                    CallbackUrl = configuration["App:Settings:Microsoft.Bot.Builder.Calling.CallbackUrl"]
                };
            }
            catch (Exception e)
            {
                throw new BotConfigurationException(
                    "A mandatory configuration item is missing or invalid", e);
            }

            settings.Validate();
            return settings;
        }

        /// <summary>
        ///     Validates current bot configuration and throws BotConfigurationException if the configuration is invalid
        /// </summary>
        public void Validate()
        {
            Uri callBackUri;
            if (!Uri.TryCreate(this.CallbackUrl, UriKind.Absolute, out callBackUri))
            {
                throw new BotConfigurationException($"Bot calling configuration is invalid, callback url: {CallbackUrl} is not a valid url!");
            }
        }
    }
}
