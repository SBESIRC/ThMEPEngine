using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.Service
{
    public class ThArchitectureWallSimplifier
    {
        private const double OFFSET_DISTANCE = 30.0;
        private const double NEGATIVE_OFFSET_DISTANCE = -30.0;

        public static DBObjectCollection Simplify(AcPolygon wall)
        {
            // 由于投影精度的原因，
            // DB3切出来的墙的区域（封闭多段线）会存在一些“杂线”
            // 需要剔除掉这些杂线
            var objs = new DBObjectCollection();
            wall.GetOffsetCurves(OFFSET_DISTANCE).Cast<Polyline>()
                .ForEach(o => 
                {
                    o.GetOffsetCurves(NEGATIVE_OFFSET_DISTANCE).Cast<Polyline>().ForEach(e => objs.Add(e));
                });
            return objs;
        }
    }
}
