using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// TSE 转换开关
    /// </summary>
    public abstract class TransferSwitch : PDSBaseComponent
    {
    }

    /// <summary>
    /// ATSE 自动转换开关
    /// </summary>
    public class AutomaticTransferSwitch : TransferSwitch
    {
        public AutomaticTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.ATSE;
        }

        public string Content { get { return $"{TransferSwitchType} {RatedCurrent}A {PolesNum}"; } }

        /// <summary>
        /// 隔离开关类型
        /// </summary>
        public string TransferSwitchType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }
    }

    /// <summary>
    /// MTSE 手动转换开关
    /// </summary>
    public class ManualTransferSwitch : TransferSwitch
    {
        public ManualTransferSwitch(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.MTSE;
        }

        public string Content { get { return $"{TransferSwitchType} {RatedCurrent}A {PolesNum}"; } }

        /// <summary>
        /// 隔离开关类型
        /// </summary>
        public string TransferSwitchType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }
    }
}
