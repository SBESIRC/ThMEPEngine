using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Configure;

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
            var ATSEComponent = ATSEConfiguration.ATSEComponentInfos.FirstOrDefault(o =>
                double.Parse(o.Amps.Split(';').Last())>calculateCurrent
                && o.Poles.Contains(polesNum));
            if (ATSEComponent.IsNull())
            {
                throw new NotSupportedException();
            }
            TransferSwitchType = ATSEComponent.Model;
            PolesNum = polesNum;
            RatedCurrent = ATSEComponent.Amps.Split(';').Select(o => double.Parse(o)).First(o => o > calculateCurrent).ToString();
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
            var MTSEComponent = MTSEConfiguration.MTSEComponentInfos.FirstOrDefault(o =>
                double.Parse(o.Amps.Split(';').Last())>calculateCurrent
                && o.Poles.Contains(polesNum));
            if (MTSEComponent.IsNull())
            {
                throw new NotSupportedException();
            }
            TransferSwitchType = MTSEComponent.Model;
            PolesNum = polesNum;
            RatedCurrent = MTSEComponent.Amps.Split(';').Select(o => double.Parse(o)).First(o => o > calculateCurrent).ToString();
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
