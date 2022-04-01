using System;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    public class IsolatingSwitch : PDSBaseComponent
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="calculateCurrent"></param>
        /// <param name="polesNum"></param>
        /// <exception cref="NotSupportedException"></exception>
        public IsolatingSwitch(double calculateCurrent, string polesNum)
        {
            ComponentType = ComponentType.QL;
            var isolator = IsolatorConfiguration.isolatorInfos.FirstOrDefault(o => o.Poles == polesNum && o.Amps > calculateCurrent);
            if (isolator.IsNull())
            {
                throw new NotSupportedException();
            }
            MaxKV = isolator.MaxKV;
            PolesNum = isolator.Poles;
            Model = isolator.Model;
            RatedCurrent = isolator.Amps.ToString();
        }

        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public string MaxKV { get; set; }
    }
}
