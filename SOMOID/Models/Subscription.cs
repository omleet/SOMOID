using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class Subscription
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }
        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }
        public string ContainerResourceName { get; set; }
        public string ApplicationResourceName { get; set; }
        [JsonProperty("res-type")]
        public string ResType => "subscription";
        [JsonProperty("evt")]
        public int Evt { get; set; }
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
    }
}
