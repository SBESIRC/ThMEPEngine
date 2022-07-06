using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingCoilSetting
    {
        public Dictionary<string, List<string>> BlockNameDict { get; set; }
        public bool WithUI = false;
        public static ThFloorHeatingCoilSetting Instance = new ThFloorHeatingCoilSetting();

        public ThFloorHeatingCoilSetting()
        {
            BlockNameDict = new Dictionary<string, List<string>>();
        }
    }
}
