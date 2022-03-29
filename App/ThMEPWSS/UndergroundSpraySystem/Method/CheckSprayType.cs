using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public static class CheckSprayType
    {
        public static int IsAcrossFloor(SprayIn sprayIn, List<Point3d> alarmPts)
        {
            var dbObjs = new DBObjectCollection();
            alarmPts.ForEach(p => dbObjs.Add(new DBPoint(p)));
            var ptSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            var alarmFloorCnt = 0;
            foreach (var rect in sprayIn.FloorRectDic.Values)
            {
                if(ptSpatialIndex.SelectCrossingPolygon(rect).Count > 0)
                {
                    alarmFloorCnt++;
                }
                if (alarmFloorCnt > 1) return 1;
            }
            return 0;
        }

        public static bool HasAlarmValveWithoutStartPt(Point3dEx startPt, Polyline rect, List<Point3dEx> alarmPts)
        {
            if(rect.Contains(startPt._pt))
            {
                return false;
            }
            var dbObjs = new DBObjectCollection();
            alarmPts.ForEach(p => dbObjs.Add(new DBPoint(p._pt)));
            var ptSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            
            if (ptSpatialIndex.SelectCrossingPolygon(rect).Count > 0)
            {
                return true;
            }
            
            return false;
        }
    }
}
