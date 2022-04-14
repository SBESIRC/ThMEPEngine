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
    /// 冲洗点位、皮带水嘴
    /// </summary>
    public class ThFlushPointModel
    {
        public ThFlushPointModel(BlockReference valve)
        {
            Valve = valve;
            Point = new Point3d(valve.Position.X, valve.Position.Y, 0);
        }
        public ThFlushPointModel()
        {

        }
        public BlockReference Valve;
        public Point3d Point;
    }
}
