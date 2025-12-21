using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class ContentInstance
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }

        [JsonProperty("container-resource-name")]
        public string ContainerResourceName { get; set; }

        [JsonProperty("application-resource-name")]
        public string ApplicationResourceName { get; set; }

        [JsonProperty("res-type")]
        public string ResType => "content-instance";

        [JsonProperty("content-type")]
        public string ContentType { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
