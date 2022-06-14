using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class MarkLine
    {
        public static void GetPipeMark(FireHydrantSystemIn fireHydrantSysIn, List<List<Point3d>> pipeMarkSite, 
            Point3d startPt, double tor = 30)
        {
            foreach(var pts in pipeMarkSite)
            {
                var sptEx = new Point3dEx(pts[0]);
                var eptEx = new Point3dEx(pts[1]);
                if (pts[0].DistanceTo(startPt) < tor)
                {
                    fireHydrantSysIn.StartEndPts.Add(sptEx);
                    fireHydrantSysIn.StartEndPts.Add(eptEx);
                    return;
                }
                if (pts[1].DistanceTo(startPt) < tor)
                {
                    fireHydrantSysIn.StartEndPts.Add(eptEx);
                    fireHydrantSysIn.StartEndPts.Add(sptEx);
                    return;
                }
            }
        }
        public static bool GetMarkLineList(this FireHydrantSystemIn fireHydrantSysIn, 
            List<Line> lineList, Dictionary<Point3dEx, double> markAngleDic)
        {
            var spt = fireHydrantSysIn.StartEndPts[0];
            var ept = fireHydrantSysIn.StartEndPts[1];
            var sPt = PointCompute.PointOnLine(spt._pt, lineList, markAngleDic[spt], 30);
            var ePt = PointCompute.PointOnLine(ept._pt, lineList, markAngleDic[ept], 30);
            if(sPt.Equals(new Point3dEx()) || ePt.Equals(new Point3dEx()))
            {
                return false;
            }

            fireHydrantSysIn.StartEndPts.Clear();
            fireHydrantSysIn.StartEndPts.Add(sPt);
            fireHydrantSysIn.StartEndPts.Add(ePt);
            return true;
        }
    }
}
