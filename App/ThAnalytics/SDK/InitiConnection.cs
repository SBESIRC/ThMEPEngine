using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThAnalytics.SDK
{
    public class InitiConnection
    {
        /// <summary>
        /// 
        /// </summary>
        public string session_id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cmd_name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int cmd_seconds { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Segmentation cmd_data { get; set; }

    }
}
