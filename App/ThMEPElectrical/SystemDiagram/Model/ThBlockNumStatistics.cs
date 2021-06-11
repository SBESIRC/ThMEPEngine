using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 块配置表
    /// 在此配置块名和获取数量的白名单
    /// </summary>
    public class ThBlockNumStatistics
    {
        //string 块名  int 数量
        public Dictionary<string, int> BlockStatistics;
        public ThBlockNumStatistics()
        {
            this.BlockStatistics = new Dictionary<string, int>();
            ThBlockConfigModel.BlockConfig.ForEach(o => this.BlockStatistics.Add(o.UniqueName, o.DefaultQuantity));
        }
    }
}
