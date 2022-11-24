using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class Valve//提取阀门
    {
        public DBObjectCollection SignalValve { get; set; }//遥控信号阀
        public DBObjectCollection PressureValve { get; set; }//减压阀
        public DBObjectCollection DieValve { get; set; }//蝶阀
        public List<Point3dEx> SignalValves { get; set; }//遥控信号阀
        public List<Point3dEx> PressureValves { get; set; }//减压阀
        public List<Point3dEx> DieValves { get; set; }//蝶阀

        public Valve()
        {
            SignalValve = new DBObjectCollection();
            PressureValve = new DBObjectCollection();
            DieValve = new DBObjectCollection();
            SignalValves = new List<Point3dEx>();
            PressureValves = new List<Point3dEx>();
            DieValves = new List<Point3dEx>();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<Entity>()
                    .Where(o => IsTargetLayer(o.Layer))
                    .Where(o => IsValve(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                    dbObjs.Cast<Entity>()
                        .Where(e => IsSignalValve(e))
                        .ForEach(e => SignalValve.Add(ExplodeValve(e)));

                    dbObjs.Cast<Entity>()
                        .Where(e => IsPRValve(e))
                        .ForEach(e => PressureValve.Add(ExplodeValve(e)));

                    dbObjs.Cast<Entity>()
                        .Where(e => IsDieValve(e))
                        .ForEach(e => DieValve.Add(ExplodeValve(e)));
                }
            }
        }

        private bool IsTargetLayer(string layer)
        {
            return layer.Contains("EQPM");
        }
        private bool IsValve(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("阀") || blkName.Contains("VALVE");
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("阀") || blkName.Contains("VALVE");
                }
            }
            return false;
        }
        private bool IsSignalValve(Entity entity)
        {
            if(entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("遥控信号阀") ||
                        (blkName.Contains("VALVE") && blkName.Contains("437"));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if(objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("遥控信号阀") ||
                            (blkName.Contains("VALVE") && (blkName.Contains("437") || blkName.Contains("697")));
                }
            }
            return false;
        }
        private bool IsPRValve(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("减压阀") ||
                    (blkName.Contains("VALVE") && blkName.Contains("301"));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("减压阀") || 
                        (blkName.Contains("VALVE") && (blkName.Contains("301") || blkName.Contains("673") || blkName.Contains("437")));
                }
            }
            return false; 
        }

        private bool IsDieValve(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return blkName.Contains("蝶阀") ||
                    (blkName.Contains("VALVE") && blkName.Contains("316"));
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return blkName.Contains("蝶阀") ||
                        (blkName.Contains("VALVE") && blkName.Contains("316"));
                }
            }
            return false;
        }

        private BlockReference ExplodeValve(Entity entity)
        {
            if(entity is BlockReference bkr)
            {
                return bkr;
            }
            else
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                return (BlockReference)objs[0];
            }
        }
        public void CreateValveLine()
        {
            foreach (var sv in SignalValve)
            {
                if(sv is BlockReference br)
                {
                    var pt = br.Position;
                    SignalValves.Add(new Point3dEx(pt));
                }
            }
            foreach (var sv in PressureValve)
            {
                if (sv is Entity entity)
                {
                    var pt1 = entity.GeometricExtents.MaxPoint;
                    var pt2 = entity.GeometricExtents.MinPoint;
                    var pt = PtTools.GetMidPt(pt1, pt2);
                    PressureValves.Add(new Point3dEx(pt));
                }
            }

            foreach (var sv in DieValve)
            {
                if (sv is BlockReference br)
                {
                    var pt = br.Position;
                    DieValves.Add(new Point3dEx(pt));
                }
            }
        }
    }
}
