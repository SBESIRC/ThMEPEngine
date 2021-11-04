using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class Verticalpipe
    {
        public DBObjectCollection DBObjs { get; set; }
        public Verticalpipe()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o is BlockReference || o.GetType().Name.Contains("ImpCurve"))
                   .Where(o => !IsNotTargetLayer(o.Layer))
                   .Where(o => !IsTCHPipeFitting(o))
                   .Where(o => !o.IsTCHValve())
                   .Where(o => !o.IsTCHText())
                   .ToList();

                var Results1 = acadDatabase
                   .ModelSpace
                   .OfType<Circle>()
                   .Where(o => o.Radius < 120 && o.Radius > 30)
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());

                //spatialIndex不支持圆
                var map = new Dictionary<Polyline, Circle>();
                Results1.ForEach(o => map.Add(o.ToRectangle(), o));
                var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());

                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    var dbObjs1 = spatialIndex1.SelectCrossingPolygon(polygon);
                    try
                    {
                        dbObjs.Cast<Entity>()
                            .ForEach(e => ExplodeCircle(e, DBObjs));

                        dbObjs1.Cast<Entity>()
                            .ForEach(e => DBObjs.Add(map[e as Polyline]));
                    }
                    catch
                    {
                        ;
                    }
                }

                sprayIn.Verticals = GetVerticals();
            }
        }

        private bool IsNotTargetLayer(string layer)
        {
            return layer.Contains("W-RAIN-PIPE") ||
                   layer.Contains("W-FRPT-HYDT-PIPE");
        }

        public List<Point3dEx> GetVerticals()
        {
            var verticals = new List<Point3dEx>();
            DBObjs.Cast<Entity>()
                .ForEach(e => verticals.Add(new Point3dEx((e as Circle).Center)));
            return verticals;
        }

        private void ExplodeCircle(Entity entity, DBObjectCollection DBObjs)
        {
            try
            {
                if (entity is BlockReference bkr)
                {
                    if (NotExplodeBlock(bkr))
                    {
                        return;
                    }
                }
                if (entity is BlockReference || entity.GetType().Name.Contains("ImpCurve"))
                {
                    var objs = new DBObjectCollection();
                    entity.Explode(objs);
                    objs.Cast<Entity>()
                        .Where(e => e is Circle)
                        .Where(e => (e as Circle).Radius < 120 && (e as Circle).Radius > 30)
                        .ForEach(e => DBObjs.Add(e));
                    objs.Cast<Entity>()
                            .ForEach(e => ExplodeCircle(e, DBObjs));
                    return;
                }
            }
            catch
            {
                ;
            }
        }

        private bool NotExplodeBlock(BlockReference bkr)
        {
            var name = "";
            try
            {
                name = bkr.GetEffectiveName();
            }
            catch (Exception ex)
            {
                name = bkr.Name;
            }
            if(name.Contains("潜水泵") || 
                name.Contains("报警阀") ||
                name.Contains("xhs") ||
                name.Contains("灭火器") ||
                name.Contains("气压罐") ||
                name.Contains("*"))
            {
                return true;
            }
            return false;
        }
        public static bool IsTCHPipeFitting(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.Equals("TCH_PIPEFITTING");
        }
    }
}
