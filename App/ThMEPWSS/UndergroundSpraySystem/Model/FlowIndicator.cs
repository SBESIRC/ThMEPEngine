using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class FlowIndicator
    {
        public DBObjectCollection DBObjs { get; set; }

        public string BlockName = "信号阀＋水流指示器";
        public FlowIndicator()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<Entity>()
                    .Where(o => IsTarget(o))
                    .ToList();
                if (Results.Count == 0) return;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);

                dbObjs.Cast<Entity>()
                    .ForEach(e => DBObjs.Add(ExplodeValve(e)));
            }
        }

        private bool IsTarget(Entity entity)
        {
            if (entity is BlockReference blockReference)
            {
                var blkName = blockReference.GetEffectiveName().ToUpper();
                return IsTarget(blkName);
            }
            else if (entity.IsTCHValve())
            {
                var objs = new DBObjectCollection();
                entity.Explode(objs);
                if (objs[0] is BlockReference bkr)
                {
                    var blkName = bkr.Name.ToUpper();
                    return IsTarget(blkName);
                }
            }
            return false;
        }

        private bool IsTarget(string blkName)
        {
            return (blkName.Contains("VALVE") && blkName.Contains("531")) ||
                (blkName.Contains("VALVE") && blkName.Contains("74")) ||
                (blkName.Contains("VALVE") && blkName.Contains("75")) ||
                (blkName.Contains("VALVE") && blkName.Contains("76")) ||
                (blkName.Contains("VALVE") && blkName.Contains("77")) ||
                (blkName.Contains("VALVE") && blkName.Contains("27")) ||
                blkName.Contains("信号阀+水流指示器") ;
        }

        private BlockReference ExplodeValve(Entity entity)
        {
            if (entity is BlockReference bkr)
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

        public List<Point3dEx> CreatePts(SprayIn sprayIn)
        {
            var pts = new List<Point3dEx>();
            foreach (var db in DBObjs)
            {
                if(db is BlockReference br)
                {
                    var bounds = br.Bounds;
                    var pt = new Point2d((bounds.Value.MaxPoint.X + bounds.Value.MinPoint.X) / 2,
                        (bounds.Value.MaxPoint.Y + bounds.Value.MinPoint.Y) / 2);
                    var newpt = pt.ToPoint3d();
                    pts.Add(new Point3dEx(newpt));
                    try
                    {
                        sprayIn.FlowTypeDic.Add(new Point3dEx(newpt), br.ObjectId.GetDynBlockValue("可见性"));
                    }
                    catch
                    {
                        sprayIn.FlowTypeDic.Add(new Point3dEx(newpt), "");
                    }

                }
            }
            return pts;
        }
        public List<Polyline> CreatBlocks()
        {
            var result = new List<Polyline>();
            foreach (var db in DBObjs)
            {
                if (db is BlockReference br)
                {
                    result.Add(GetFlowRect(br));
                }
            }
            return result;
        }


        private Polyline GetFlowRect(BlockReference flow)
        {
            var objs = new DBObjectCollection();
            flow.Explode(objs);
            var rect1 = new Polyline();//水流指示器
            var rect2 = new Polyline();//阀门
            bool rst1 = false;
            bool rst2 = false;
            foreach(var obj in objs)
            {
                if(obj is BlockReference br)
                {
                    if(br.Name.Contains("水流指示器"))
                    {
                        rect1 = br.GetRect();
                        rst1 = true;
                    }
                    if(br.Name.Contains("阀"))
                    {
                        rect2 = br.GetRect();
                        rst2 = true;
                    }
                }
            }
            if(!rst1|| !rst2)
            {
                return flow.GetRect();
            }
            var centPt1 = rect1.GetCenter();
            var centPt2 = rect2.GetCenter();
            var pts = new List<Point3d>();
            pts.AddRange(rect1.GetPoints());
            pts.AddRange(rect2.GetPoints());
            if (Math.Abs(centPt1.X - centPt2.X) < Math.Abs(centPt1.Y - centPt2.Y))//竖排
            {
                var orderPtY = pts.OrderBy(p => p.Y).ToList();
                var rect = GetRectY(centPt1.X,orderPtY.First().Y, orderPtY.Last().Y);
                return rect;
            }
            else
            {
                var orderPtX = pts.OrderBy(p => p.X).ToList();
                var rect = GetRectX(centPt1.Y, orderPtX.First().X, orderPtX.Last().X);
                return rect;
            }
        }
        private Polyline GetRectY(double x,double minY,double maxY)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            pts.Add(new Point2d(x - 180, minY)); // low left
            pts.Add(new Point2d(x - 180, maxY)); // high left
            pts.Add(new Point2d(x + 180, maxY)); // low right
            pts.Add(new Point2d(x + 180, minY)); // high right
            pts.Add(new Point2d(x - 180, minY)); // low left
            pl.CreatePolyline(pts);
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                currentDb.CurrentSpace.Add(pl);
            }
#endif
            return pl;
        }
        private Polyline GetRectX(double y, double minX, double maxX)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            pts.Add(new Point2d(minX, y-180)); // low left
            pts.Add(new Point2d(minX, y + 180)); // high left
            pts.Add(new Point2d(maxX, y + 180)); // low right
            pts.Add(new Point2d(maxX, y - 180)); // high right
            pts.Add(new Point2d(minX, y - 180)); // low left
            pl.CreatePolyline(pts);
#if DEBUG
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                currentDb.CurrentSpace.Add(pl);
            }
#endif
            return pl;
        }
    }
}
