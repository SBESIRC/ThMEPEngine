using System;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_Isolator_19DX101.xlsx
    /// </summary>
    public class IsolatorConfiguration : PDSBaseComponentConfiguration
    {
        /// <summary>
        /// 隔离开关配置
        /// </summary>
        public static List<IsolatorConfigurationItem> isolatorInfos = new List<IsolatorConfigurationItem>();
    }

    [Serializable]
    public class IsolatorConfigurationItem : PDSBaseComponentConfigurationItem
    {
        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public string MaxKV { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string Poles { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public double Amps { get; set; }

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
