using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using Draw = ThMEPWSS.UndergroundSpraySystem.Method.Draw;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class VerticalPipeNew
    {
        public DBObjectCollection DBObjs { get; set; }
        public VerticalPipeNew()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, Point3dCollection selectArea, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results1 = acadDatabase //TCH_PIPE
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsTchVerticalLayer(o.Layer))
                   .Where(o => o.IsTCHPipe())
                   .ToList();

                var Results2 = acadDatabase  //Circle
                   .ModelSpace
                   .OfType<Circle>()
                   .Where(o => IsCircleLayer(o.Layer))
                   .Where(o => o.Radius < 150 && o.Radius > 50)
                   .ToList();

                var Results3 = acadDatabase  //BlockRefrence
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => o.GetEffectiveName().Contains("定位立管"))
                   .ToList();

                var spatialIndex1 = new ThCADCoreNTSSpatialIndex(Results1.ToCollection());

                //spatialIndex不支持圆
                var map = new Dictionary<Polyline, Circle>();
                Results2.ForEach(o => map.Add(o.ToRectangle(), o));
                var spatialIndex2 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());

                var spatialIndex3 = new ThCADCoreNTSSpatialIndex(Results3.ToCollection());

                foreach (var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);
                    var dbObjs2 = spatialIndex2.SelectCrossingPolygon(polygon);
                    var dbObjs3 = spatialIndex3.SelectCrossingPolygon(polygon);

                    dbObjs1.Cast<Entity>().ForEach(e => ExplodeTchPipe(e));

                    dbObjs2.Cast<Entity>().ForEach(e => DBObjs.Add(map[e as Polyline]));

                    dbObjs3.Cast<Entity>().ForEach(e => ExplodeBlock(e));

                }
                sprayIn.Verticals = GetVerticals();
                Draw.Verticals(sprayIn.Verticals);
            }
        }

        private bool IsTchVerticalLayer(string layer)
        {
            var rst1 = layer.Contains("W-FRPT") && layer.Contains("SPRL-EQPM");
            var rst2 = layer.Contains("W-FRPT") && layer.Contains("XTG");
            var rst3 = layer.Contains("W-FRPT") && layer.Contains("EXT");
            return rst1 || rst2 || rst3;
        }

        private bool IsCircleLayer(string layer)
        {
            var rst1 = layer.Contains("W-FRPT") && layer.Contains("-EQPM");
            return rst1;
        }

        private void ExplodeTchPipe(Entity entity)
        {
            var objs = new DBObjectCollection();
            entity.Explode(objs);

            objs.Cast<Entity>()
                .Where(e => e is Circle)
                .ForEach(e => DBObjs.Add(e));
        }

        private void ExplodeBlock(Entity entity)
        {
            var objs = new DBObjectCollection();
            entity.Explode(objs);
            foreach(var obj in objs)
            {
                if(obj is Circle circle)
                {
                    if(circle.Radius > 50 && circle.Radius < 120)
                    {
                        DBObjs.Add(circle);
                        return;
                    }
                }
            }
        }

        public List<Point3dEx> GetVerticals()
        {
            var verticals = new List<Point3dEx>();
            DBObjs.Cast<Entity>()
                .ForEach(e => verticals.Add(new Point3dEx((e as Circle).Center)));
            return verticals;
        }

        public ThCADCoreNTSSpatialIndex CreateVerticalSpatialIndex()
        {
            var rects = new DBObjectCollection();
            foreach(var obj in DBObjs)
            {
                var circle = obj as Circle;
                var rect = circle.Center.GetRect(100);
                rects.Add(rect);
            }
            return new ThCADCoreNTSSpatialIndex(rects);
        }
    }
}
