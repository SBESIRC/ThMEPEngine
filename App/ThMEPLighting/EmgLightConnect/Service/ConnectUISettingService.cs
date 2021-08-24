using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class ConnectUISettingService
    {
        public int groupMin { get; set; }
        public int groupMax { get; set; }

        public static ConnectUISettingService Instance = new ConnectUISettingService();

        public ConnectUISettingService()
        {
            groupMin = 5;
            groupMax = 25;
        }
    }
}
