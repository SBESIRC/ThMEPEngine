using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;


using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Model;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;
using Linq2Acad;

namespace ThMEPWSS.HydrantLayout.tmp.Engine
{
    class FeasibilityCheck
    {

        //外界输入
        //MPolygon LeanWall;

        //frame 圆形区域内的属性
        public FeasibilityCheck() 
        {
            //LeanWall = leanWall;
        }

        //校验外框是否可行
        public static bool IsFireFeasible(Polyline fireArea, Polyline shell) 
        {
            bool flag = false;
            var bufferArea = fireArea.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var objs1 = ProcessedData.ForbiddenIndex.SelectCrossingPolygon(pl);
            var objs2 = ProcessedData.ParkingIndex.SelectCrossingPolygon(pl);
            objs1.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1forbidden", 8));
            objs2.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1paking", 9));
            //objs2.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1testblock", 8));
            if (objs1.Count == 0 && objs2.Count == 0 && shell.Contains(pl))
            {
                flag = true;
            }
            return flag;
        }

        //校验开门方向是否可行
        public static bool IsDoorFeasible(Polyline doorArea, Polyline shell)
        {
            bool flag = false;
            var bufferArea = doorArea.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var objs = ProcessedData.ForbiddenIndex.SelectCrossingPolygon(pl);
            objs.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1forbidden", 2));

            if (objs.Count == 0 && shell.Contains(pl))
            {
                flag = true;
            }
            return flag;
        }


        //校验外框遮挡
        public static bool IsFireBlocked(Polyline fireArea)
        {
            bool flag = false;
            var bufferArea= fireArea.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var objs1 = ProcessedData.ForbiddenIndex.SelectCrossingPolygon(pl);
            var objs2 = ProcessedData.ParkingIndex.SelectCrossingPolygon(pl);
            if (objs1.Count == 0 && objs2.Count == 0)
            {
                flag = true;
            }
            return flag;
        }

        public static bool IsBoundaryOK(Polyline area, ThCADCoreNTSSpatialIndex forbidden) 
        {
            bool flag = false;
            var bufferArea = area.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var obj = forbidden.SelectCrossingPolygon(pl);
            if (obj.Count == 0) 
            {
                flag = true;
            }
            return flag;
        }
    }
}
