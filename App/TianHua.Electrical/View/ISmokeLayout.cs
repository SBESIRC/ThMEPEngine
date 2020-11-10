using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical
{
    public interface ISmokeLayout
    {

        
        SmokeLayoutDataModel m_SmokeLayout { get; set; }

        /// <summary>
        /// 烟感房间面积
        /// </summary>
        List<string> m_ListSmokeRoomArea { get; set; }

        /// <summary>
        /// 温感房间面积
        /// </summary>
        List<string> m_ListThalposisRoomArea { get; set; }


        /// <summary>
        /// 烟感房间高度
        /// </summary>
        List<string> m_ListSmokeRoomHeight { get; set; }

        /// <summary>
        /// 温感房间高度
        /// </summary>
        List<string> m_ListThalposisRoomHeight { get; set; }
        
    }
}
