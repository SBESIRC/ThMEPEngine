using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    public class IsolatingSwitch : PDSBaseComponent
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        /// <param name="polesNum">极数</param>
        public IsolatingSwitch(double calculateCurrent, string polesNum)
        {
            ComponentType = ComponentType.隔离开关;
            var isolator = IsolatorConfiguration.isolatorInfos.FirstOrDefault(o => o.Poles == polesNum && o.Amps > calculateCurrent);
            if (isolator.IsNull())
            {
                throw new NotSupportedException();
            }
            IsolatingSwitchType = isolator.ModelName;
            PolesNum = isolator.Poles;
            RatedCurrent = isolator.Amps.ToString();
        }

        public string Content { get { return $"{IsolatingSwitchType} {RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 隔离开关类型
        /// </summary>
        public string IsolatingSwitchType { get; set; }

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
