using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PtInPtList
    {
        public static bool PtIsTermLine(Line line, List<Point3dEx> ptLs)
        {
            var flag = false;
            foreach(var tpt in ptLs)
            {
                if(line.StartPoint.DistanceTo(tpt._pt) < 80)
                {
                    flag = true;
                    break;
                }
            }
            if(!flag)
            {
                return false;
            }
            foreach (var tpt in ptLs)
            {
                if (line.EndPoint.DistanceTo(tpt._pt) < 80)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool PtIsTermPt(Point3dEx pt, List<Point3dEx> ptLs)
        {
            foreach (var tpt in ptLs)
            {
                if (pt._pt.DistanceTo(tpt._pt) < 80)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
