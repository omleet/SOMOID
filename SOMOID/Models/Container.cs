using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class Container
    {
        public string ResourceName { get; set; }
        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }
        [JsonProperty("res-type")]
        public string ResType => "container";
        public string ApplicationResourceName { get; set; }
    }
}
