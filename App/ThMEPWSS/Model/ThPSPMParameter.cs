using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.Model
{
    public class ThPSPMParameter
    {
        /// <summary>
        /// 参数设置
        /// </summary>
        public ParamSettingViewModel paraSettingViewModel { get; set; }

        /// <summary>
        /// 面板参数
        /// </summary>
        public FirstFloorPlaneViewModel firstFloorPlaneViewModel { get; set; }

        /// <summary>
        /// 洁具配置
        /// </summary>
        public Dictionary<string, List<string>> config { get; set; }
    }
}
