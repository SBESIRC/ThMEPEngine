using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPWSS.UndergroundWaterSystem.Command;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    /// <summary>
    /// 给水立管
    /// </summary>
    public class ThRiserInfo
    {
        public string MarkName { set; get; }
        public List<Point3d> RiserPts { set; get; }
        public ThRiserInfo()
        {
            MarkName = "";
            RiserPts = new List<Point3d>();
        }
    }

    public class ThRiserModel : ThBaseModel
    {
        /// <summary>
        /// 立管所在楼层索引号
        /// </summary>
        public int FloorIndex { set; get; }
        /// <summary>
        /// 立管标注
        /// </summary>
        public string MarkName { set; get; }
        public override void Initialization(Entity entity)
        {
            FloorIndex = -1;
            MarkName = "";
            if (ThUndergroundWaterSystemUtils.IsTianZhengElement(entity))
            {
                Position = entity.GeometricExtents.ToRectangle().GetCenter().ToPoint2D().ToPoint3d();
            }
            else
            {
                if (entity is Circle circle)
                {
                    Position = circle.Center.ToPoint2D().ToPoint3d() ;
                }
                else if (entity is BlockReference blk)
                {
                    Position = blk.Position.ToPoint2D().ToPoint3d();
                }
            }
        }
    }
}
