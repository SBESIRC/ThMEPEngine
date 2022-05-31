using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.LowVoltageCabinet
{
    /// <summary>
    /// 馈线柜
    /// </summary>
    public class FeederCabinet : PDSBaseLowVoltageCabinet
    {
        //馈线柜的柜子个数在[1,9]区间内，不同样式的柜子内含的规格是不一样的，具体逻辑待后续补充给

        /// <summary>
        /// 馈线柜
        /// </summary>
        public FeederCabinet()
        {

        }
    }
}
