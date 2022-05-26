using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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
    public class ThVerticalpipe
    {
        private double MaxRadius { get; set; }
        private double MinRadius { get; set; }
        public DBObjectCollection DBObjs { get; set; }
        public ThVerticalpipe()
        {
            DBObjs = new DBObjectCollection();
            MaxRadius = 200;
            MinRadius = 30;
        }
        public List<Point3dEx> Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var block = acadDatabase.ModelSpace.OfType<Entity>()
                    .Where(o => (o is BlockReference)).ToList();
                var blockname = new HashSet<string>();
                foreach (var bk in block)
                {
                    var name = (bk as BlockReference).GetEffectiveName();
                    if (!blockname.Contains(name))
                    {
                        blockname.Add(name);
                    }
                }

                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => (o is BlockReference && !IsNotTargetBlock(o as BlockReference)) || o.GetType().Name.Contains("ImpCurve"))
                   .Where(o => !IsNotTargetLayer(o.Layer))
                   .Where(o => !IsTCHPipeFitting(o))
                   .Where(o => !o.IsTCHValve())
                   .Where(o => !o.IsTCHText())
                   .ToList();

                var Results1 = acadDatabase
                   .ModelSpace
                   .OfType<Circle>()
                   .Where(o => o.Radius < MaxRadius && o.Radius > MinRadius)
                   .ToList();

                var Results2 = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => o.GetType().Name.Contains("ImpCurve"))
                   .Where(o => !IsNotTargetLayer(o.Layer))
                   .Where(o => !IsTCHPipeFitting(o))
                   .Where(o => !o.IsTCHValve())
                   .Where(o => !o.IsTCHText())
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());

                //spatialIndex不支持圆
                var map = new Dictionary<Polyline, Circle>();
                Results1.ForEach(o => map.Add(o.ToRectangle(), o));
                var spatialIndex1 = new ThCADCoreNTSSpatialIndex(map.Keys.ToCollection());

   
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
                
                return GetVerticals();
            }
        }

        private bool IsNotTargetLayer(string layer)
        {
            return layer.Contains("W-RAIN-PIPE") ||
                   (layer.Contains("W-FRPT") && layer.Contains("HYDT") && layer.Contains("PIPE"));
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
                        .Where(e => (e as Circle).Radius < MaxRadius && (e as Circle).Radius > MinRadius)
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
        private bool IsNotTargetBlock(BlockReference bkr)
        {
            var name = bkr.GetEffectiveName();
            return name.Contains("泵") ||
                   name.Contains("阀") ||
                   name.Contains("xhs") ||
                   name.Contains("接头") ||
                   name.Contains("器") ||
                   name.Contains("气压罐") ||
                   name.Contains("平面") ||
                   name.Contains("柜") ||
                   name.Contains("铃") ||
                   name.Contains("框") ||
                   name.Contains("套管") ||
                   name.Contains("THAPE") ||
                   name.Contains("板") ||
                   name.Contains("喷") ||
                   name.Contains("标高") ||
                   name.Contains("水管中断") ||
                   name.Contains("覆盖") ||
                   name.Contains("*");
        }

        private bool NotExplodeBlock(BlockReference bkr)
        {
            var name = "";
            try
            {
                name = bkr.GetEffectiveName();
            }
            catch
            {
                name = bkr.Name;
            }
            if (name.Contains("潜水泵") ||
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
        private static bool IsTCHPipeFitting(Entity entity)
        {
            string dxfName = entity.GetRXClass().DxfName.ToUpper();
            return dxfName.Equals("TCH_PIPEFITTING");
        }
    }
}
