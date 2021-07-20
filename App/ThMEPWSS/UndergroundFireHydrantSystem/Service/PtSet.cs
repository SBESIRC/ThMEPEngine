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
    class PtSet
    {
        public static void AddVisit(ref HashSet<Point3dEx> visited, List<List<Point3dEx>> PathList)
        {
            foreach (var ptls in PathList)
            {
                foreach (var pt in ptls)
                {
                    visited.Add(pt);
                }
            }
        }

        public static List<Point3dEx> SetStartEndPt(FireHydrantSystemIn fireHydrantSysIn, List<Line> markLine)
        {
            var PtList = new List<Point3dEx>();
            var startPt = new Point3dEx(new Point3d());// 主环起始点
            if (fireHydrantSysIn.ptDic[new Point3dEx(markLine[0].StartPoint)].Count == 1)
            {
                startPt = new Point3dEx(markLine[0].StartPoint);
            }
            else
            {
                startPt = new Point3dEx(markLine[0].EndPoint);
            }
            var targetPt = new Point3dEx(markLine[1].EndPoint);//主环终止点
            if (fireHydrantSysIn.ptDic[new Point3dEx(markLine[1].StartPoint)].Count == 1)
            {
                targetPt = new Point3dEx(markLine[1].StartPoint);
            }
            else
            {
                targetPt = new Point3dEx(markLine[1].EndPoint);
            }

            PtList.Add(startPt);
            PtList.Add(targetPt);
            return PtList;
        }
    }
}
