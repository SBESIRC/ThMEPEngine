using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;

namespace ThMEPStructure.GirderConnect.Data.Utils
{
    class DataClassify
    {
        private const double SIMILARITY_MEASURE_TOLERANCE = 0.99;

        /// <summary>
        /// 对房间中的事物进行分类
        /// </summary>
        /// <param name="outlineWalls"></param>
        /// <param name="outlineClumns"></param>
        public static void ClassifyOutlineWalls(ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls, ref Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
        {
            foreach (var outlineWall in outlineWalls)
            {
                var houseOutline = outlineWall.Key;
                if (!outlineClumns.ContainsKey(houseOutline))
                {
                    outlineClumns.Add(houseOutline, new HashSet<Point3d>());
                }
                if (!outlineWalls.ContainsKey(houseOutline))
                {
                    outlineWalls.Add(houseOutline, new HashSet<Polyline>());
                }
                Classify(PreprocessLinealElements(outlineWall.Value.ToCollection()), outlineClumns[houseOutline], outlineWalls[houseOutline], houseOutline, 600);
            }
        }

        /// <summary>
        /// 分类：多边形分为柱或墙
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        private static void Classify(DBObjectCollection curves, HashSet<Point3d> columns, HashSet<Polyline> walls, Polyline outline, double maxDis = 600)
        {
            foreach (var curve in curves)
            {
                if (curve is Polyline polyline)
                {
                    polyline.Closed = true;
                    if (IsColumns(polyline))
                    {
                        var centroidPt = polyline.GetCentroidPoint();
                        double curDis = centroidPt.DistanceTo(outline.GetClosePoint(centroidPt));
                        if (curDis < maxDis)
                        {
                            columns.Add(centroidPt);
                        }
                    }
                    else
                    {
                        walls.Add(polyline);
                    }
                }
            }
        }
        private static void Classify(HashSet<Entity> curves, HashSet<Polyline> columns, HashSet<Polyline> walls)
        {
            foreach (var curve in curves)
            {
                if (curve is Polyline polyline)
                {
                    if (IsColumns(polyline) && !columns.Contains(polyline))
                    {
                        columns.Add(polyline);
                    }
                    else if (!walls.Contains(polyline))
                    {
                        walls.Add(polyline);
                    }
                }
            }
        }

        /// <summary>
        /// 对于房间外的事物（wall & column）outsideColumns & outsideShearwall -> clumnPts & outlineWalls
        /// </summary>
        /// <param name="outsideColumns"></param>
        /// <param name="clumnPts"></param>
        /// <param name="outerWalls"></param>
        /// <param name="outsideShearwall"></param>
        public static void OuterClassify(List<Entity> outsideColumns, List<Entity> outsideShearwall, 
            Point3dCollection clumnPts, ref Dictionary<Polyline, HashSet<Polyline>> outerWalls, 
            ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts, ref Dictionary<Polyline, Polyline> outline2OriOutline)
        {
            Dictionary<Polyline, bool> plColumnVisted = new Dictionary<Polyline, bool>();
            foreach (var outsideColumn in outsideColumns)
            {
                if (outsideColumn is Polyline pl)
                {
                    
                    if (!plColumnVisted.ContainsKey(pl))
                    {
                        plColumnVisted.Add(pl, false);
                    }
                }
            }
            HashSet<Polyline> polylineColumns = plColumnVisted.Keys.ToHashSet();
            foreach (var entity in outsideShearwall)
            {
                if (entity is Polyline polyline)
                {
                    polyline.Closed = true;
                    DBObjectCollection mergeCollection = new DBObjectCollection();
                    List<Point3d> olCrossPt = PointsDealer.CrossPointsOnPolyline(polyline, 100);
                    mergeCollection.Add(polyline);
                    foreach (var polylineColumn in polylineColumns)
                    {
                        var centroidPt = polylineColumn.GetCentroidPoint();
                        if (plColumnVisted[polylineColumn] == false && centroidPt.DistanceTo(polyline.GetClosestPointTo(centroidPt, false)) < 900)
                        {
                            mergeCollection.Add(polylineColumn);
                            plColumnVisted[polylineColumn] = true;
                        }
                    }
                    var unionPolygons = mergeCollection.UnionPolygons().OfType<Polyline>().Where(p => p.Area > 1.0).OrderByDescending(o => o.Area).ToCollection();
                    if (unionPolygons.Count == 0)
                    {
                        continue;
                    }
                    var simplifiedPolyline = unionPolygons.OfType<Polyline>().First().DPSimplify(1);
                    if (simplifiedPolyline != null)
                    {
                        DataProcess.AddOutline(simplifiedPolyline, ref outerWalls);
                        olCrossPts.Add(simplifiedPolyline, olCrossPt.ToHashSet());
                        outline2OriOutline.Add(simplifiedPolyline, polyline);
                    }
                }
            }

            polylineColumns.Clear();
            foreach (var plColumnVist in plColumnVisted)
            {
                if(plColumnVist.Value == false)
                {
                    polylineColumns.Add(plColumnVist.Key);
                }
            }
            polylineColumns = DataProcess.DeleteOverlap(polylineColumns);


            foreach (var polylineColumn in polylineColumns)
            {
                clumnPts.Add(polylineColumn.GetCentroidPoint());
            }
        }

        public static void InnerColumnTypeClassify(Dictionary<Entity, HashSet<Entity>> columnGroupDict,
           ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls, ref Dictionary<Polyline, HashSet<Polyline>> outlinePlColumns)
        {
            foreach (var columnGroup in columnGroupDict)
            {
                var columnGroupKey = columnGroup.Key;
                if (columnGroupKey is Polyline outline)
                {
                    if (!outlineWalls.ContainsKey(outline))
                    {
                        outlineWalls.Add(outline, new HashSet<Polyline>());
                    }
                    if (!outlinePlColumns.ContainsKey(outline))
                    {
                        outlinePlColumns.Add(outline, new HashSet<Polyline>());
                    }
                    Classify(columnGroup.Value, outlinePlColumns[outline], outlineWalls[outline]);
                }
            }
        }

        /// <summary>
        /// 判断是否形似矩形
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static bool IsRectangle(Polyline polygon)
        {
            return polygon.IsSimilar(polygon.GetMinimumRectangle(), SIMILARITY_MEASURE_TOLERANCE);
        }

        /// <summary>
        /// 判断是不是柱子
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static bool IsColumns(Polyline polygon)
        {
            return IsRectangle(polygon) && AspectRatio(polygon) && polygon.Area < 2000000;
        }

        private static bool AspectRatio(Polyline polygon)
        {
            var obb = polygon.GetMinimumRectangle();
            var length1 = obb.GetPoint2dAt(0).GetDistanceTo(obb.GetPoint2dAt(1));
            var length2 = obb.GetPoint2dAt(1).GetDistanceTo(obb.GetPoint2dAt(2));
            return length1 > length2 ? (length1 / length2 < 3 && length2 > 450) : (length2 / length1 < 3 && length1 > 450);
        }
        
        private static DBObjectCollection PreprocessLinealElements(DBObjectCollection curves)
        {
            var results = ThVStructuralElementSimplifier.Tessellate(curves);
            results = ThVStructuralElementSimplifier.MakeValid(curves);
            results = ThVStructuralElementSimplifier.Normalize(results);
            results = ThVStructuralElementSimplifier.Simplify(results);
            return results;
        }

        public static Dictionary<Polyline, HashSet<Polyline>> SimplifyOutlineThings(Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            Dictionary<Polyline, HashSet<Polyline>> newOutlineWalls = new Dictionary<Polyline, HashSet<Polyline>>();
            foreach (var outlineWall in outlineWalls)
            {
                var outline = outlineWall.Key;
                newOutlineWalls.Add(outline, new HashSet<Polyline>());
                var walls = PreprocessLinealElements(outlineWall.Value.ToCollection());
                foreach(var wall in walls)
                {
                    if(wall is Polyline polyline)
                    {
                        polyline.Closed = true;
                        newOutlineWalls[outline].Add(polyline);
                    }
                }
            }
            return newOutlineWalls;
        }

        public static void ClassifyColumnPoints(ref HashSet<Point3d> allColumnPts, List<Entity> outsideColumns, Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
        {
            foreach (var outsideColumn in outsideColumns)
            {
                if (outsideColumn is Polyline pl)
                {
                    allColumnPts.Add(pl.GetCentroidPoint());
                }
            }
            foreach (var pts in outlineClumns.Values)
            {
                foreach (Point3d pt in pts)
                {
                    allColumnPts.Add(pt);
                }
            }
        }
    }
}
