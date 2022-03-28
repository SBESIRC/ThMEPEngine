using System;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 接触器
    /// </summary>
    public class Contactor : PDSBaseComponent
    {
        /// <summary>
        /// 接触器
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="polesNum">级数</param>
        public Contactor(double calculateCurrent, string polesNum)
        {
            ComponentType = ComponentType.QAC;
            var contactor = ContactorConfiguration.contactorInfos.FirstOrDefault(o => o.Poles == polesNum && o.Amps > calculateCurrent);
            if (contactor.IsNull())
            {
                throw new NotSupportedException();
            }
            Model = contactor.Model;
            PolesNum = contactor.Poles;
            RatedCurrent = contactor.Amps.ToString();
        }

        /// <summary>
        /// 模型
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
    }
}
