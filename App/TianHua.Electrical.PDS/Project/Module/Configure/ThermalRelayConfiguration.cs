using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_Isolator_19DX101.xlsx
    /// </summary>
    public class ThermalRelayConfiguration : PDSBaseComponentConfiguration
    {
        /// <summary>
        /// 热继电器配置
        /// </summary>
        public static List<ThermalRelayConfigurationItem> thermalRelayInfos = new List<ThermalRelayConfigurationItem>();
    }

    [Serializable]
    public class ThermalRelayConfigurationItem : PDSBaseComponentConfigurationItem
    {
        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 整定电流上限
        /// </summary>
        public double MaxAmps { get; set; }

        /// <summary>
        /// 整定电流下限
        /// </summary>
        public double MinAmps { get; set; }

        /// <summary>
        /// 热元件代号
        /// </summary>
        public string ThermalDeviceCode { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string Poles { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public string MaxKV { get; set; }

        /// <summary>
        /// 安装方式
        /// </summary>
        public string InstallMethod { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public string Width { get; set; }

        /// <summary>
        /// 深度
        /// </summary>
        public string Depth { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public string Height { get; set; }
    }
}
