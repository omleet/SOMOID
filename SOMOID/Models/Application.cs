using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class Application
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("resourceName")]
        public string LegacyResourceName
        {
            get => ResourceName;
            set => ResourceName = value;
        }

        public bool ShouldSerializeLegacyResourceName() => false;

        [JsonProperty("res-type")]
        public string ResType => "application";


        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }
    }
}
