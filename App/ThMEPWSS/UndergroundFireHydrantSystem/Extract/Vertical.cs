using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class Vertical//提取管道末端标记
    {
        public DBObjectCollection DBobjsResults { get; private set; }
        public List<Point3dEx> VerticalPts { get; private set; }
        public DBObjectCollection Extract(AcadDatabase acadDatabase, Point3dCollection polygon)
        {
            var Results = acadDatabase.ModelSpace //TCH_PIPE
               .OfType<Entity>()
               .Where(e => IsTargetLayer(e.Layer))
               .Where(e => e.IsTCHPipe());

            var Results1 = acadDatabase.ModelSpace   //处理圆
                   .OfType<Circle>()
                   .Where(o => IsTargetLayer(o.Layer));

            var Results2 = acadDatabase  //BlockRefrence
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => o.GetEffectiveName().Contains("定位立管"))
                   .ToList();

            var DBObjs = Results.ToCollection();

            //spatialIndex不支持圆
            var map = new Dictionary<Polyline, Circle>();
            Results1.ToList().ForEach(o => map.Add(o.ToRectangle(), o));
            var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());
            var DBObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);

            DBobjsResults = new DBObjectCollection();

            foreach (DBObject db in DBObjs)
            {
                ExplodeTZBlock(db as Entity, DBobjsResults);
            }
            foreach (var db in DBObjs1)//添加圆
            {
                var circle = map[db as Polyline];
                var dbPt = new DBPoint(circle.Center);
                DBobjsResults.Add(dbPt);
            }
            foreach (var db in Results2)
            {
                ExplodeDWLG(db, DBobjsResults);//添加定位立管
            }

            var rstSpatialIndex = new ThCADCoreNTSSpatialIndex(DBobjsResults);

            return rstSpatialIndex.SelectCrossingPolygon(polygon);
        }

        private static bool IsPipeLayer(string layer)
        {
            return layer.Contains("W-FRPT") && layer.Contains("HYDT") && layer.Contains("PIPE");
        }
       
        private static bool IsTargetLayer(string layer)//立管图层
        {
            return layer.Equals("W-FRPT-HYDT-VPIPE")
                 || layer.Equals("W-FRPT-HYDT-EQPM")
                 || layer.Equals("W-FRPT-HYDT");
        }

        private static void ExplodeBlock(BlockReference br, DBObjectCollection DBobjsResults)
        {
            if (IsDWLGBlock(br))//如果是定位立管
            {
                return;
            }
            else
            {
                var objs = new DBObjectCollection();
                br.Explode(objs);//把块炸开
                foreach (var obj in objs)//遍历
                {
                    var ent = obj as Entity;
                    if (!ent.Visible)
                    {
                        continue;
                    }
                    if (obj is Circle circle)//圆
                    {
                        if (IsTargetLayer(circle.Layer))
                        {
                            var dbPt = new DBPoint(circle.Center);
                            DBobjsResults.Add(dbPt);
                        }
                    }
                    if (obj is BlockReference)//块
                    {
                        ExplodeBlock(obj as BlockReference, DBobjsResults);//炸块
                    }
                }
            }
        }
        private static void ExplodeTZBlock(Entity ent, DBObjectCollection DBobjsResults)
        {
            try
            {
                var objs = new DBObjectCollection();
                ent.Explode(objs);

                objs.Cast<Entity>()
                    .Where(e => e is Circle)
                    .ForEach(e => DBobjsResults.Add(new DBPoint((e as Circle).Center)));
            }
            catch
            {
            }
        }
        private static bool IsDWLGBlock(BlockReference br)
        {
            try
            {
                return br.GetEffectiveName().Contains("定位立管");
            }
            catch
            {
                return br.Name.Contains("定位立管");
            }
        }
        public static void ExplodeDWLG(BlockReference br, DBObjectCollection DBobjsResults)//炸定位立管
        {
            var objColl = new DBObjectCollection();
            var objs = new DBObjectCollection();
            br.Explode(objColl);
            objColl.Cast<Entity>().Where(e => e is Circle).ForEach(e => objs.Add(e));
            var circles = objs.OfType<Circle>().OrderByDescending(e => e.Radius);
            if(circles.Count() > 0)
            {
                var circle = circles.First();
                var dbPt = new DBPoint(circle.Center);
                DBobjsResults.Add(dbPt);
            }
        }

        private static bool IsRepeatedPt(Point3dEx pt, List<Point3dEx> verticalPts)
        {
            double tor = 100.0;

            foreach (var pt2 in verticalPts)//去掉在一定范围内的重复点
            {
                if (pt2._pt.DistanceTo(pt._pt) < tor)
                {
                    return true;
                }
            }
            return false;
        }
        public List<Point3dEx> CreatePointList()
        {
            VerticalPts = new List<Point3dEx>();
            foreach (var db in DBobjsResults)
            {
                var centerPt = (db as DBPoint).Position;
                var pt = new Point3dEx(new Point3d(centerPt.X, centerPt.Y, 0));
                if(!IsRepeatedPt(pt, VerticalPts))
                {
                    VerticalPts.Add(pt);
                }
            }
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                foreach (var ptEx in VerticalPts)
                {
                    var pt = ptEx._pt;
                    var c = new Circle(pt, new Vector3d(0, 0, 1), 200);
                    c.LayerId = DbHelper.GetLayerId("立管圆圈图层");
                    currentDb.CurrentSpace.Add(c);
                }
            }
#endif
            return VerticalPts;
        }
    }
}
