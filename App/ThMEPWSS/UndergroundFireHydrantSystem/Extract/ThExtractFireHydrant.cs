using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
//using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractFireHydrant//室内消火栓平面
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBobjs { get; private set; }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);


                DBobjs = new DBObjectCollection();
                foreach (var db in dbObjs)
                {
                    if (db is DBPoint)
                    {
                        continue;
                    }
                    if (db is BlockReference)
                    {
                        if (IsFireHydrant((db as BlockReference).GetEffectiveName()))
                        {
                            DBobjs.Add((DBObject)db);
                        }
                        else
                        {
                            var objs = new DBObjectCollection();

                            var blockRecordId = (db as BlockReference).BlockTableRecord;
                            var btr = acadDatabase.Blocks.Element(blockRecordId);

                            int indx = 0;
                            var indxFlag = false;
                            foreach (var entId in btr)
                            {
                                var dbObj = acadDatabase.Element<Entity>(entId);
                                if (dbObj is BlockReference)
                                {
                                    if (IsFireHydrant((dbObj as BlockReference).GetEffectiveName()))
                                    {
                                        indxFlag = true;
                                        break;
                                    }
                                }
                                indx += 1;
                            }

                            (db as BlockReference).Explode(objs);
                            if (indxFlag)
                            {
                                if (indx > objs.Count - 1)
                                {
                                    continue;
                                }
                                DBobjs.Add((DBObject)objs[indx]);
                            }

                        }
                    }
                }
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-HYDT" || layer.ToUpper() == "0";
        }

        private bool IsFireHydrant(string valve)
        {
            return valve.ToUpper().Contains("室内消火栓平面");
        }

        public void CreateVerticalHydrantDic(List<Point3dEx> verticals, FireHydrantSystemIn fireHydrantSysIn)
        {
            var verticalSpatialIndex = new ThCADCoreNTSSpatialIndex(CreateRect(verticals));
            var dbObjs = DBobjs.ToArray().ToList();
            for (int i = dbObjs.Count - 1; i >= 0; i--)
            {
                try
                {
                    var obj = dbObjs[i];
                    var pt = GetCenter((obj as BlockReference).GeometricExtents);
                    var pline = CreatePolyline(pt, 1000);
                    var res = verticalSpatialIndex.SelectCrossingPolygon(pline).ToArray();
                    if (res.Count() == 0)
                    {
                        continue;
                    }
                    var closedObj = res.OrderBy(e => (e as Polyline).GetCentroidPoint().DistanceTo(pt)).First();
                    var closedPt = (closedObj as Polyline).GetCentroidPoint();
                    fireHydrantSysIn.VerticalHasHydrant.Add(new Point3dEx(closedPt));
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
                var pline = CreatePolyline(pt);
                dbObjs.Add(pline);
            }
            return dbObjs;
        }

        private static Polyline CreatePolyline(Point3dEx c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y + tolerance)); // high left
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y + tolerance)); // high right
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y - tolerance)); // low right
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
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
