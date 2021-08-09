using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ThAnalytics.SDK
{
    [Serializable]
    [DataContract]
    public class THConfig
    {
        public string Token { get; set; }

        public string ServerUrl { get; set; }

        public string AppVersion { get; set; }

        public string SSOUrl { get; set; }
    }
}
