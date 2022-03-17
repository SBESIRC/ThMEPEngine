using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    [CascadeComponent]
    public class CPS : PDSBaseComponent
    {
        public CPS()
        {
            this.ComponentType = ComponentType.CPS;
        }

        //public string Content { get { return $"{BreakerType}{FrameSpecifications}-{TripUnitType}{RatedCurrent}/{PolesNum}"; } }

        /// <summary>
        /// 型号
        /// </summary>
        public string CPSType { get; set; }

        /// <summary>
        /// 壳架规格
        /// </summary>
        public string FrameSpecifications { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 组合形式
        /// </summary>
        public string Combination { get; set; }

        /// <summary>
        /// 级别代号
        /// </summary>
        public string CodeLevel { get; set; }

        public override double GetCascadeRatedCurrent()
        {
            if (double.TryParse(RatedCurrent, out double result))
            {
                return result;
            }
            return 0;
        }
    }
}
