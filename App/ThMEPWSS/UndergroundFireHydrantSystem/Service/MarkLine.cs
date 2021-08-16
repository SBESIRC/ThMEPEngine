using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class MarkLine
    {
        public static void GetMarkLineList(ref FireHydrantSystemIn fireHydrantSysIn, List<List<Point3d>> pipeMarkSite, List<Line> lineList)
        {
            foreach (var pms in pipeMarkSite)
            {
                var markL = new List<Line>();
                var nullMark = false;//手抖多画了一对环管标记
                foreach (var v in pms)
                {
                    var line = PointCompute.PointOnLine(v, lineList);
                    if (line.StartPoint.Equals(new Point3d(0, 0, 0)))
                    {
                        nullMark = true;
                        break;
                    }
                    markL.Add(PointCompute.PointOnLine(v, lineList));
                }
                if (!nullMark)
                {
                    fireHydrantSysIn.MarkLineList.Add(markL);
                }

            }
        }
    }
}
