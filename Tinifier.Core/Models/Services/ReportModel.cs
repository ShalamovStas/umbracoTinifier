using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Tinifier.Core.Models.Services
{
    public class ReportModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string[] Files { get; set; }
        public string File { get; set; }

    }
}