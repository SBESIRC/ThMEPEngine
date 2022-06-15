using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class DisplayInfo
    {
        public string BlockName { get; set; }
        public string FinalIterations { get; set; }
        public string FinalStalls { get; set; }
        public string FinalAveAreas { get; set; }
        public string CostTime { get; set; }
        public DisplayInfo(string blockName)
        {
            BlockName = "地库块名：" + blockName;
        }
    }
}
