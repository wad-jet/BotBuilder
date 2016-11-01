using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Connector
{
    public partial class APIResponseEx
    {
        public partial class Entity
        {
            [JsonExtensionData(ReadData = true, WriteData = true)]
            public JObject Properties { get; set; }
        }
    }
}
