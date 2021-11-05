using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Service
{
    /// <summary>
    /// 管路提取器，得到管路树
    /// </summary>
    public class ThPipeExtractServiece
    {
        public static ThFanTreeModel<ThFanPipeModel> GetPipeTreeModel(Point3dCollection area)
        {
            ThFanTreeModel<ThFanPipeModel> resTree = new ThFanTreeModel<ThFanPipeModel>();
            return resTree;
        }
    }
}
