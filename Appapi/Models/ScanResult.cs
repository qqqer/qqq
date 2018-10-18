using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Appapi.Models
{
    public class ScanResult
    {
        public IEnumerable<Receipt> batch { get; set; }
        public string error { get; set; }
    }
}