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
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class Vertical//提取管道末端标记
    {
        public DBObjectCollection DBobjsResults { get; private set; }
        public List<Point3dEx> VerticalPts { get; private set; }
        public void Extract(AcadDatabase acadDatabase, Point3dCollection polygon)
        {
            var Results = acadDatabase.ModelSpace //TCH_PIPE
               .OfType<Entity>()
               .Where(e => IsTargetLayer(e.Layer))
               .Where(e => e.IsTCHPipe());

            var Results1 = acadDatabase.ModelSpace   //处理圆
                   .OfType<Circle>()
                   .Where(o => IsTargetLayer(o.Layer));

            var Results2 = BlockExtractService.ExtractBlocks(acadDatabase.Database, "定位立管"); 

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
                ExplodeDWLG(db as BlockReference, DBobjsResults);//添加定位立管
            }
        }
        
        private static bool IsTargetLayer(string layer)//立管图层
        {
            return (layer.Contains("HYDT")&& layer.Contains("VPIPE"))
                 || layer.Equals("W-FRPT-HYDT-EQPM")
                 || layer.Equals("W-FRPT-HYDT")
                 || layer.Equals("W-FRPT-EXTG");
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
            catch(Exception ex)
            {
                ;
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

            return VerticalPts;
        }
    }
}
