using Newtonsoft.Json;
using System;

namespace StopProcessor
{
    [Serializable]
    class OisMappingRecord
    {
        [JsonProperty(PropertyName = "ois")]
        public int OisNumber { get; set; }

        [JsonProperty(PropertyName = "node")]
        public int NodeNumber { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string StopName { get; set; }
    }
}
