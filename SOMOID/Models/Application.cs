using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class Application
    {
        public string ResourceName { get; set; }
        [JsonProperty("res-type")]
        public string ResType { get; set; }
        public DateTime CreationDatetime { get; set; }
    }
}
