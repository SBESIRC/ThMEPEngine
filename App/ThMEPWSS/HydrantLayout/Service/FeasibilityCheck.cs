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
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

using NFox.Cad;
using ThMEPEngineCore.Diagnostics;
using Linq2Acad;

namespace ThMEPWSS.HydrantLayout.Engine
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

        //校验外框是否可行，一般是用于消火栓和灭火器的实体部分
        public static bool IsFireFeasible(Polyline fireArea, Polyline shell) 
        {
            bool flag = false;
            var bufferArea = fireArea.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var objs1 = ProcessedData.ForbiddenIndex.SelectCrossingPolygon(pl);
            var objs2 = ProcessedData.ParkingIndex.SelectCrossingPolygon(pl);
            bool NotInPaking = FeasibilityCheck.NotInPaking(fireArea, shell);
            objs1.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1forbidden", 8));
            objs2.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1paking", 9));
            if (objs1.Count == 0 && objs2.Count == 0 && shell.Contains(pl) && NotInPaking)
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

        //判断“某种框线”是否能通过“某种判断”，本质上是上面几种判断的灵活通用版本
        public static bool IsBoundaryOK(Polyline area, Polyline shell, ThCADCoreNTSSpatialIndex forbidden) 
        {
            bool flag = false;
            var bufferArea = area.Buffer(-10);
            //如果是paking则加一步判断
            if (forbidden == ProcessedData.ParkingIndex)
            {
                if (!FeasibilityCheck.NotInPaking(area,shell)) 
                {
                    return false;
                }
            }
            //不是paking则直接走正常流程
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var obj = forbidden.SelectCrossingPolygon(pl);
            if (obj.Count == 0) 
            {
                flag = true;
            }
            return flag;
        }

        //因为空间索引无法进行小搜大，因此要判断整个框线是否都在车位里面
        public static bool NotInPaking(Polyline area, Polyline shell) 
        {
            bool flag = true;
            var bufferArea = area.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            List<Polyline> pakings = ProcessedData.ParkingIndex.SelectCrossingPolygon(shell).OfType<Polyline>().ToList();
            foreach (Polyline paking in pakings) 
            {
                if (paking.Contains(pl)) 
                {
                    flag = false;
                }
            }
            return flag;
        }



        //此函数已经废弃，留在此处当模板
        public static bool IsFireBlocked(Polyline fireArea)
        {
            bool flag = false;
            var bufferArea = fireArea.Buffer(-10);
            var pl = bufferArea.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
            var objs1 = ProcessedData.ForbiddenIndex.SelectCrossingPolygon(pl);
            var objs2 = ProcessedData.ParkingIndex.SelectCrossingPolygon(pl); //?
            if (objs1.Count == 0 && objs2.Count == 0)
            {
                flag = true;
            }
            return flag;
        }
    }
}
