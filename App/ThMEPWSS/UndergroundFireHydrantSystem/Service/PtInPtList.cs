using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
