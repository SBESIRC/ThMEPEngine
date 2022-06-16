using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPStructure.GirderConnect.Data.Utils
{
    class DataProcess
    {
        /// <summary>
        /// 将多边形无差别的插入结构outlineWalls中
        /// </summary>
        /// <param name="groupDict"></param>
        /// <param name="outlineWalls"></param>
        public static void PolylineAddToOutlineWalls(Dictionary<Entity, HashSet<Entity>> groupDict, ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            foreach (var group in groupDict)
            {
                if (group.Key is Polyline newHouseOutline)
                {
                    if (!outlineWalls.ContainsKey(newHouseOutline))
                    {
                        outlineWalls.Add(newHouseOutline, new HashSet<Polyline>());
                    }
                    foreach (var wall in group.Value)
                    {
                        if (wall is Polyline newPolyline)
                        {
                            if (!outlineWalls[newHouseOutline].Contains(newPolyline))
                            {
                                outlineWalls[newHouseOutline].Add(newPolyline);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将层叠的墙、柱合并
        /// </summary>
        /// <param name="outlineWalls"></param>
        public static Dictionary<Polyline, HashSet<Polyline>> MergeWall(Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            var outlineWallLists = outlineWalls.ToList();
            var result = new Dictionary<Polyline, HashSet<Polyline>>();
            outlineWalls.ForEach(o =>
            {
                var objs = o.Value.ToCollection();
                var newObjs = objs.UnionPolygons();
                var inners = newObjs.OfType<Polyline>().ToHashSet();
                result.Add(o.Key, inners);
            });
            return result;
        }

        /// <summary>
        /// 合并重叠，保留大的（去毛边）
        /// </summary>
        /// <param name="outlineWalls"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, HashSet<Polyline>> MergeWithSimplifyWalls(Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            var outlineWallLists = outlineWalls.ToList();
            var result = new Dictionary<Polyline, HashSet<Polyline>>();
            foreach(var outlineWallList in outlineWallLists)
            {
                HandleOverlap polies = new HandleOverlap(outlineWallList.Value.ToList(), 40);
                var inners  = polies.Handle().ToHashSet();
                result.Add(outlineWallList.Key, inners);
            }
            return result;
        }

        /// <summary>
        /// 将层叠的多边形保留一个
        /// </summary>
        /// <param name="polylines"></param>
        public static HashSet<Polyline> DeleteOverlap(HashSet<Polyline> polylines)
        {
            Dictionary<Polyline, bool> visitPolylines = new Dictionary<Polyline, bool>();
            foreach (var polyline in polylines)
            {
                if (!visitPolylines.ContainsKey(polyline))
                {
                    visitPolylines.Add(polyline, false);
                }
            }
            HashSet<Polyline> ans = new HashSet<Polyline>();
            var tmpVisLines = visitPolylines.Keys.ToList();
            foreach (var polylineA in tmpVisLines)
            {
                if (!visitPolylines[polylineA])
                {
                    ans.Add(polylineA);
                    visitPolylines[polylineA] = true;
                    foreach (var polylineB in tmpVisLines)
                    {
                        if (!visitPolylines[polylineB] && polylineA.Intersects(polylineB))
                        {
                            visitPolylines[polylineB] = true;
                        }
                    }
                }
            }
            return ans;
        }

        /// <summary>
        /// 将层叠的多边形合并
        /// </summary>
        /// <param name="polylines"></param>
        public static HashSet<Polyline> MergeOverlap(HashSet<Polyline> polylines)
        {
            return polylines.ToCollection().UnionPolygons().OfType<Polyline>().ToHashSet();
        }

        /// <summary>
        /// 单体墙在外围添加边框
        /// </summary>
        /// <param name="outline"></param>
        /// <param name="outlineWalls"></param>
        public static void AddOutline(Polyline outline, ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            if (!outlineWalls.ContainsKey(outline))
            {
                outlineWalls.Add(outline, new HashSet<Polyline>());
            }
            if (!outlineWalls[outline].Contains(outline))
            {
                outlineWalls[outline].Add(outline);
            }
        }

        public static void DicHashAdd(Dictionary<Polyline, HashSet<Polyline>> outlinePlColumns, ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            foreach (var outlinePlColumn in outlinePlColumns)
            {
                var outline = outlinePlColumn.Key;
                if (!outlineWalls.ContainsKey(outline))
                {
                    outlineWalls.Add(outline, new HashSet<Polyline>());
                }
                foreach (var plColumn in outlinePlColumn.Value)
                {
                    if (!outlineWalls[outline].Contains(plColumn))
                    {
                        outlineWalls[outline].Add(plColumn);
                    }
                }
            }
        }

        // 将多个mpolyline中存在的回字变成一个多边形
        public static List<Polyline> MPL2PL(List<MPolygon> mPolygons)
        {
            List<Polyline> polylines = new List<Polyline>();
            foreach(var mPolygon in mPolygons)
            {
                if(mPolygon.Holes().Count > 0)
                {
                    polylines.Add(SplitADonut(mPolygon));
                }
                else
                {
                    polylines.Add(mPolygon.Shell());
                }
            }
            return polylines;
        }

        //分割一个回字形区域
        public static Polyline SplitADonut(MPolygon mPolygon)
        {
            var plA = mPolygon.Shell();
            var plB = mPolygon.Holes()[0]; // 此处所取得的不准确，应该是所有holes中面积最大的一个
            var splitPtA = plA.GetPoint3dAt(0);
            var splitPtB = plB.GetClosestPointTo(splitPtA, false);
            var tuples = new List<Tuple<Point3d, Point3d>>();
            int numA = plA.NumberOfVertices;

            for (int i = 0; i < numA; ++i)
            {
                tuples.Add(new Tuple<Point3d, Point3d>(plA.GetPoint3dAt(i), plA.GetPoint3dAt((i + 1) % numA)));
            }

            tuples.Add(new Tuple<Point3d, Point3d>(splitPtA, splitPtB));
            tuples.Add(new Tuple<Point3d, Point3d>(splitPtB, splitPtA));

            int numB = plB.NumberOfVertices;
            bool skipFlag = false;
            for (int i = 0; i < numB; ++i)
            {
                if(skipFlag == false)
                {
                    var stPt = plB.GetPoint3dAt(i);
                    var edPt = plB.GetPoint3dAt((i + 1) % numB);
                    if (LineContainsPt(stPt, edPt, splitPtB))
                    {
                        skipFlag = true;
                        tuples.Add(new Tuple<Point3d, Point3d>(stPt, splitPtB));
                        tuples.Add(new Tuple<Point3d, Point3d>(splitPtB, edPt));
                    }
                }
                tuples.Add(new Tuple<Point3d, Point3d>(plB.GetPoint3dAt(i), plB.GetPoint3dAt((i + 1) % numB)));
            }
            return TypeConvertor.Tuples2Polyline(tuples);
        }

        //判断一个点是否在两个点组成的线上
        public static bool LineContainsPt(Point3d stPt, Point3d edPt, Point3d midPt)
        {
            var minus = midPt.DistanceTo(stPt) + midPt.DistanceTo(edPt) - stPt.DistanceTo(edPt);
            return minus < 1.0 || minus > -1.0;
        }
    }
}
