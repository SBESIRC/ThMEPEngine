using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    /// <summary>
    /// 断线
    /// </summary>
    public class ThBreakModel
    {
        public string BreakName { set; get; }
        public Point3d Point;
        public bool Used = false;
    }
}
