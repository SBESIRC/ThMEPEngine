using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_ATSE_ImportLibrary_19DX101.xlsx
    /// </summary>
    public class ATSEConfiguration
    {
        /// <summary>
        /// ATSE配置
        /// </summary>
        public static List<ATSEComponentInfo> ATSEComponentInfos = new List<ATSEComponentInfo>();
    }
    public class ATSEComponentInfo
    {
        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 型号（全称）
        /// </summary>
        public string ModelName { get; set; }

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
        public string Amps { get; set; }

        /// <summary>
        /// ATSE功能特点
        /// </summary>
        public string ATSECharacteristics { get; set; }

        /// <summary>
        /// 使用类别
        /// </summary>
        public string UtilizationCategory { get; set; }

        /// <summary>
        /// 主触头O位置
        /// </summary>
        public string ATSEMainContact { get; set; }

        /// <summary>
        /// 额定极限分断能力
        /// </summary>
        public string Icu { get; set; }

        /// <summary>
        /// 额定运行短路分断能力倍数
        /// </summary>
        public string IcuMultiple { get; set; }

        /// <summary>
        /// 额定短路接通能力
        /// </summary>
        public string Icm { get; set; }

        /// <summary>
        /// 额定短时耐受能力
        /// </summary>
        public string Icw { get; set; }

        /// <summary>
        /// 短时耐受时间
        /// </summary>
        public string Tkr { get; set; }

        /// <summary>
        /// 瞬时脱扣时间
        /// </summary>
        public string TrippingTime { get; set; }

        /// <summary>
        /// 延时脱扣时间
        /// </summary>
        public string TrippingTimeDelay { get; set; }

        /// <summary>
        /// 接线能力
        /// </summary>
        public string WiringCapacity { get; set; }
        
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
