using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if(contactor.IsNull())
            {
                throw new NotSupportedException();
            }
            ContactorType = contactor.Model;
            PolesNum = contactor.Poles;
            RatedCurrent = contactor.Amps.ToString();
        }

        //public string Content { get { return $"{ContactorType} {RatedCurrent}/{PolesNum}"; } }


        /// <summary>
        /// 接触器类型
        /// </summary>
        public string ContactorType { get; set; }

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
