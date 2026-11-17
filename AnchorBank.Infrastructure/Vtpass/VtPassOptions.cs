using System;
using System.Collections.Generic;
using System.Text;

namespace AnchorBank.Infrastructure.Vtpass
{
  

    public class VtpassOptions
    {
        public const string SectionName = "Vtpass";

        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
