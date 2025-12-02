using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class Subscription
    {
        public string ResourceName { get; set; }
        public DateTime CreationDatetime { get; set; }
        public string ContainerResourceName { get; set; }
        public string ResType { get; set; }
        public int Evt { get; set; }
        public string Endpoint { get; set; }
    }
}
