using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Models
{
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = null;
        public object ValidationErrors { get; set; } = null;
    }
}