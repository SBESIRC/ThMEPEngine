using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_CPS_ImportLibrary_19DX101.xlsx
    /// </summary>
    public class CPSConfiguration
    {
        /// <summary>
        /// ATSE配置
        /// </summary>
        public static List<CPSComponentInfo> CPSComponentInfos = new List<CPSComponentInfo>();
    }

    [Serializable]
    public class CPSComponentInfo
    {
        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSize { get; set; }

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
        /// 剩余电流动作
        /// </summary>
        public string ResidualCurrent { get; set; }

        /// <summary>
        /// 组合形式
        /// </summary>
        public string CPSCombination { get; set; }

        /// <summary>
        /// 类别代号
        /// </summary>
        public string CPSCharacteristics { get; set; }

        /// <summary>
        /// 安装方式
        /// </summary>
        public string InstallMethod { get; set; }
    }
}
