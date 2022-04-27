using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_Current_Transformer_19DX101.xlsx
    /// </summary>
    public class CurrentTransformerConfiguration
    {
        /// <summary>
        /// 电流互感器CT配置
        /// </summary>
        public static List<CTComponentInfo> CTComponentInfos = new List<CTComponentInfo>();
    }

    [Serializable]
    public class CTComponentInfo
    {
        /// <summary>
        /// 额定电流
        /// </summary>
        public double Amps { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string parameter { get; set; }
    }
}
