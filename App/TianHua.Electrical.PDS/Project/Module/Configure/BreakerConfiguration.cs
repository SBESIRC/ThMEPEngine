using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_CircuitBreaker_ImportLibrary_19DX101.xlsx
    /// </summary>
    public class BreakerConfiguration
    {
        /// <summary>
        /// 断路器配置
        /// </summary>
        public static List<BreakerComponentInfo> breakerComponentInfos = new List<BreakerComponentInfo>();
    }

    [Serializable]
    public class BreakerComponentInfo
    {
        /// <summary>
        /// 型号
        /// </summary>
        public BreakerModel Model { get; set; }

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
        public double Amps { get; set; }

        /// <summary>
        /// 脱扣器
        /// </summary>
        public string TripDevice { get; set; }

        /// <summary>
        /// 剩余电流动作
        /// </summary>
        public string ResidualCurrent { get; set; }

        /// <summary>
        /// 瞬时脱扣器型式
        /// </summary>
        public string Characteristics { get; set; }

        /// <summary>
        /// 剩余电流脱扣器类型
        /// </summary>
        public string RCDCharacteristics { get; set; }

        /// <summary>
        /// 额定极限分断能力级别
        /// </summary>
        public string IcuLevel { get; set; }

        /// <summary>
        /// 额定极限分断能力
        /// </summary>
        public string Icu { get; set; }

        public string IcuConfig { get { return $"{IcuLevel}:{Icu}KA"; } }

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

        /// <summary>
        /// 默认不选中
        /// </summary>
        public bool DefaultPick { get; set; }
    }
}
