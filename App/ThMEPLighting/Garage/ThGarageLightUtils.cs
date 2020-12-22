using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage
{
    public static class ThGarageLightUtils
    {
        public static ObjectId AddLineType(Database db, string linetypeName)
        {
            using(AcadDatabase acadDatabase= AcadDatabase.Use(db))
            {
                var lt = acadDatabase.Element<LinetypeTable>(db.LinetypeTableId);
                if(!lt.Has(linetypeName))
                {
                    lt.UpgradeOpen();
                    var ltr = new LinetypeTableRecord();
                    ltr.Name = linetypeName;
                    lt.Add(ltr);
                    lt.DowngradeOpen();
                }
                return lt[linetypeName];
            }
        }
        public static ObjectId LoadLineType(Database db, string linetypeName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                var lt = acadDatabase.Element<LinetypeTable>(db.LinetypeTableId);
                if (!lt.Has(linetypeName))
                {
                    db.LoadLineTypeFile(linetypeName,"acad.lin");
                }
                return lt[linetypeName];
            }
        }
        public static ObjectId AddLayer(Database db, string layer)
        {
            using (AcadDatabase acadDatabase= AcadDatabase.Use(db))
            {
                var lt = acadDatabase.Element<LayerTable>(db.LayerTableId);
                if(!lt.Has(layer))
                {
                    lt.UpgradeOpen();
                    var ltr = new LayerTableRecord();
                    ltr.Name = layer;
                    lt.Add(ltr);
                    lt.DowngradeOpen();
                }
                return lt[layer];
            }
        }
        public static List<Entity> GetRegionCurves(this Polyline regionBorder, List<string> layers,List<Type> types)
        {
            List<Entity> ents = new List<Entity>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                int count = types.Count;
                List<string> starts =new List<string>();
                types.ForEach(o => starts.Add(RXClass.GetClass(o).DxfName));
                List<TypedValue> tvs = new List<TypedValue>();
                if (starts.Count > 0)
                {
                    tvs.Add(new TypedValue((int)DxfCode.Start, string.Join(",", starts)));
                }
                if (layers.Count>0)
                {
                    tvs.Add(new TypedValue((int)DxfCode.LayerName, string.Join(",", layers)));
                }
                var pts = regionBorder.Vertices();
                SelectionFilter sf = new SelectionFilter(tvs.ToArray());
                var psr = Active.Editor.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {
                    var dbObjs = new DBObjectCollection();
                    psr.Value.GetObjectIds().ForEach(o => dbObjs.Add(acdb.Element<Entity>(o)));
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    spatialIndex.SelectCrossingPolygon(pts).Cast<Entity>().ForEach(o=>ents.Add(o));
                }
                return ents;
            }
        }
        public static ThCADCoreNTSSpatialIndex BuildSpatialIndex(List<Line> lines)
        {
            DBObjectCollection objs = new DBObjectCollection();
            lines.ForEach(o => objs.Add(o));
            return new ThCADCoreNTSSpatialIndex(objs);
        }
        public static bool IsLink(this Line line, Point3d pt,double tolerance=1.0)
        {
            return line.StartPoint.DistanceTo(pt) <= tolerance ||
                    line.EndPoint.DistanceTo(pt) <= tolerance;
        }
        public static bool IsLink(this Line first, Line second,double tolerance=1.0)
        {            
            if (ThGeometryTool.IsCollinearEx(first.StartPoint, 
                first.EndPoint,second.StartPoint,second.EndPoint))
            {
                double sum = first.Length + second.Length;
                var pairPts = new List<Tuple<Point3d, Point3d>>();
                pairPts.Add(Tuple.Create(first.StartPoint, second.StartPoint));
                pairPts.Add(Tuple.Create(first.StartPoint, second.EndPoint));
                pairPts.Add(Tuple.Create(first.EndPoint, second.StartPoint));
                pairPts.Add(Tuple.Create(first.EndPoint, second.EndPoint));
                var biggest = pairPts.OrderByDescending(o => o.Item1.DistanceTo(o.Item2)).First();
                var bigDistance = biggest.Item1.DistanceTo(biggest.Item2);
                return Math.Abs(sum - bigDistance) <= tolerance;
            }
            else
            {
                return IsLink(first, second.StartPoint, tolerance) ||
                 IsLink(first, second.EndPoint, tolerance);
            }
        }
        public static bool IsCoincide(this Line first,Line second,double tolerance= 1e-4)
        {
            return IsCoincide(first.StartPoint,first.EndPoint,
                second.StartPoint,second.EndPoint, tolerance);
        }
        public static bool IsCoincide(Point3d firstSp,Point3d firstEp,
            Point3d secondSp,Point3d secondEp,double tolerance=1e-4)
        {
            return 
                (firstSp.DistanceTo(secondSp) <= tolerance &&
                firstEp.DistanceTo(secondEp) <= tolerance) ||
                (firstSp.DistanceTo(secondEp) <= tolerance &&
                firstEp.DistanceTo(secondSp) <= tolerance);
        }
        public static List<Point3d> PtOnLines(this List<Point3d> pts,List<Line> lines,double tolerance=1.0)
        {
            var results = new List<Point3d>();
            foreach (var pt in pts)
            {
                foreach(var line in lines)
                {
                    if(line.IsLink(pt, tolerance))
                    {
                        results.Add(pt);
                        break;
                    }
                }
            }
            return results;
        }
        public static Tuple<Point3d, Point3d> GetMaxPts(this List<ThLightEdge> edges)
        {
            var pts = new List<Point3d>();
            edges.ForEach(o =>
            {
                pts.Add(o.Edge.StartPoint);
                pts.Add(o.Edge.EndPoint);
            });
            return pts.GetCollinearMaxPts();
        }
        public static bool IsContains(this List<Line> lines, Line line,double tolerance=1.0)
        {
            return lines.Where(o => line.IsCoincide(o, tolerance)).Any();
        }
    }
}
