using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 绘制模型
    /// </summary>
    public class ThDrawModel
    {
        public string FireDistrictName { get; set; }

        public DataSummary Data { get; set; }

        public bool DrawCircuitName { get; set; } = false;

        public string WireCircuitName { get; set; }
    }
}
