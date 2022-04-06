using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundWaterSystem.Model;

namespace ThMEPWSS.UndergroundWaterSystem.ViewModel
{
    public class ThWaterSystemInfoModel
    {
        //楼层间距
        public double FloorLineSpace{ set; get; }
        public List<ThFloorModel> FloorList { set; get; }
        public ThWaterSystemInfoModel()
        {
            FloorLineSpace = 5000.0;
        }
    }
}
