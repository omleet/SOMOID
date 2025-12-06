using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class ContentInstance
    {
        public string ResourceName { get; set; }
        public DateTime CreationDatetime { get; set; }
        public string ContainerResourceName { get; set; }
        public string ApplicationResourceName { get; set; }
        [JsonProperty("res-type")]

        public string ResType { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
    }
}
