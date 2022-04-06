using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public class ThPointModel
    {
        public int TeeCount { set; get; }//通过三通的数量
        public bool IsTraversal { set; get; }//是否被遍历
        public Point3d Position { set; get; }//点位置
        public ThRiserInfo Riser { set; get; }//立管
        public ThBreakModel Break { set; get; }//断线
        public ThDimModel DimMark { set; get; }//管径标注
        public ThPointModel()
        {
            TeeCount = 0;
            IsTraversal = false;
            Riser = null;
        }
    }
}
