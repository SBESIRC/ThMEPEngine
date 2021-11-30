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
    class DataProcess
    {
        /// <summary>
        /// 将多边形无差别的插入结构outlineWalls中
        /// </summary>
        /// <param name="groupDict"></param>
        /// <param name="outlineWalls"></param>
        public static void PolylineAddToOutlineWalls(Dictionary<Entity, HashSet<Entity>> groupDict, Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
        {
            foreach (var group in groupDict)
            {
                if (group.Key is Polyline houseOutline)
                {
                    houseOutline.Closed = true;
                    if (!outlineWalls.ContainsKey(houseOutline))
                    {
                        outlineWalls.Add(houseOutline, new HashSet<Polyline>());
                    }
                    foreach (var wall in group.Value)
                    {
                        if (wall is Polyline polyline)
                        {
                            polyline.Closed = true;
                            if (!outlineWalls[houseOutline].Contains(polyline))
                            {
                                outlineWalls[houseOutline].Add(polyline);
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

        public static void DicHashAdd(Dictionary<Polyline, HashSet<Polyline>> outlinePlColumns, Dictionary<Polyline, HashSet<Polyline>> outlineWalls)
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
    }
}
