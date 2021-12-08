using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Service;
using NFox.Cad;

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
        public static void ClassifyOutlineWalls(Dictionary<Polyline, HashSet<Polyline>> outlineWalls, Dictionary<Polyline, HashSet<Point3d>> outlineClumns)
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
                Classify(PreprocessLinealElements(outlineWall.Value.ToCollection()), outlineClumns[houseOutline], outlineWalls[houseOutline]);
            }
        }

        /// <summary>
        /// 分类：多边形分为柱或墙
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        private static void Classify(DBObjectCollection curves, HashSet<Point3d> columns, HashSet<Polyline> walls)
        {
            foreach (var curve in curves)
            {
                if (curve is Polyline polyline)
                {
                    polyline.Closed = true;
                    if (IsColumns(polyline))
                    {
                        columns.Add(polyline.GetCentroidPoint());
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
                    polyline.Closed = true;
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
        /// <param name="outlineWalls"></param>
        /// <param name="outsideShearwall"></param>
        public static void OuterClassify(List<Entity> outsideColumns, Point3dCollection clumnPts, ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls, List<Entity> outsideShearwall)
        {
            ////先合并柱子和墙
            //List<Entity> mixEntity = new List<Entity>();
            //mixEntity.AddRange(outsideColumns);
            //mixEntity.AddRange(outsideShearwall);
            //var newObjs = mixEntity.ToCollection().UnionPolygons().OfType<Polyline>().ToHashSet();
            ////分类
            //HashSet<Polyline> polylineColumns = new HashSet<Polyline>();
            //foreach (var entity in newObjs)
            //{
            //    if (entity is Polyline polyline)
            //    {
            //        polyline.Closed = true;
            //        if (IsColumns(polyline))
            //        {
            //            polylineColumns.Add(polyline);
            //        }
            //        else
            //        {
            //            DataProcess.AddOutline(polyline, ref outlineWalls);
            //        }
            //    }
            //}

            //HashSet<Polyline> polylineColumns = new HashSet<Polyline>();
            //foreach (var outsideColumn in outsideColumns)
            //{
            //    if (outsideColumn is Polyline pl)
            //    {
            //        if (!polylineColumns.Contains(pl))
            //        {
            //            polylineColumns.Add(pl);
            //        }
            //    }
            //}
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
                    var mergedPolyline = polyline;

                    DBObjectCollection mergeCollection = new DBObjectCollection();
                    mergeCollection.Add(polyline);
                    foreach (var polylineColumn in polylineColumns)
                    {
                        if(plColumnVisted[polylineColumn] == false && mergedPolyline.Intersects(polylineColumn))
                        {
                            mergeCollection.Add(polylineColumn);
                            plColumnVisted[polylineColumn] = true;
                        }
                    }
                    //mergedPolyline = mergeCollection.Fix().UnionPolygons().OfType<Polyline>().FirstOrDefault();
                    mergedPolyline = mergeCollection.UnionPolygons().OfType<Polyline>().FirstOrDefault();
                    if (mergedPolyline != null)
                    {
                        DataProcess.AddOutline(mergedPolyline, ref outlineWalls);
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
            Dictionary<Polyline, HashSet<Polyline>> outlineWalls, Dictionary<Polyline, HashSet<Polyline>> outlinePlColumns)
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
    }
}
