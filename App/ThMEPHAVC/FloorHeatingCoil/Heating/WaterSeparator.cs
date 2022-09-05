using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;

using ThMEPHVAC.FloorHeatingCoil.Heating;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class WaterSeparator
    {
        static public Polyline EntranceCorrection(Polyline originPl, Vector3d waterOffset,List<Point3d> fixList) 
        {
            var points = PassageWayUtils.GetPolyPoints(originPl);
            points = SmoothUtils.SmoothPoints(points);
            var si = PassageWayUtils.GetPointIndex(fixList[0], points);
            var ei = PassageWayUtils.GetPointIndex(fixList[1], points);
            Point3d start = fixList[0];
            Point3d end = fixList[1];


            if (si == -1 || ei == -1) return originPl;

            if ((ei + 1) % points.Count != si)
            {
                var tmp = si;
                si = ei;
                ei = tmp;

                start = fixList[1];
                end = fixList[0];
            }
            PassageWayUtils.RearrangePoints(ref points, si);
            // 多段线倒圆角
            Point3d start2 = start + waterOffset;
            Point3d end2 = end + waterOffset;
            points.Insert(0, start2);
            points.Add(end2);
            
            

            points = SmoothUtils.SmoothPoints(points);

            var ret = PassageWayUtils.BuildPolyline(points);
            
            return ret;
        }
    }
}
