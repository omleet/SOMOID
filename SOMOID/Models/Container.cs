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
        public DateTime CreationDatetime { get; set; }
        [JsonProperty("res-type")]
        public string ResType { get; set; }
        public string ApplicationResourceName { get; set; }
    }
}
