using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractFireHydrant//室内消火栓平面
    {
        public DBObjectCollection DBobjs { get; private set; }
        public HashSet<BlockReference> Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var hydrantWithReel = new HashSet<BlockReference>();
                DBobjs = BlockExtractService.ExtractBlocks(acadDatabase.Database, "室内消火栓平面", out hydrantWithReel);
                return hydrantWithReel;
            }
        }

        public void CreateVerticalHydrantDic(List<Point3dEx> verticals, FireHydrantSystemIn fireHydrantSysIn)
        {
            var verticalSpatialIndex = new ThCADCoreNTSSpatialIndex(CreateRect(verticals));
            var dbObjs = DBobjs.ToArray().ToList();
            for (int i = dbObjs.Count - 1; i >= 0; i--)
            {
                try
                {
                    var block = dbObjs[i] as BlockReference;
                    var pt = GetCenter(block.GeometricExtents);
                    var pline = CreatePolyline(pt, 1000);
                    var res = verticalSpatialIndex.SelectCrossingPolygon(pline).ToArray();
                    if (res.Count() == 0)
                    {
                        continue;
                    }
                    var closedObj = res.OrderBy(e => (e as Polyline).GetCentroidPoint().DistanceTo(pt)).First();
                    var closedPt = (closedObj as Polyline).GetCentroidPoint();
                    fireHydrantSysIn.VerticalHasHydrant.Add(new Point3dEx(closedPt));
                    if (fireHydrantSysIn.HydrantWithReel.Contains(block))
                    {
                        fireHydrantSysIn.VerticalHasReelHydrant.Add(new Point3dEx(closedPt));
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private Point3d GetCenter(Extents3d extent3d)
        {
            var pt1 = extent3d.MaxPoint;
            var pt2 = extent3d.MinPoint;
            return new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
        }

        private DBObjectCollection CreateRect(List<Point3dEx> verticals)
        {
            var dbObjs = new DBObjectCollection();
            foreach (var pt in verticals)
            {
                var pline = CreatePolyline(pt._pt);
                dbObjs.Add(pline);
            }
            return dbObjs;
        }

        private static Polyline CreatePolyline(Point3d c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pts.Add(new Point2d(c.X - tolerance, c.Y + tolerance)); // high left
            pts.Add(new Point2d(c.X + tolerance, c.Y + tolerance)); // high right
            pts.Add(new Point2d(c.X + tolerance, c.Y - tolerance)); // low right
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

       
    }
}
