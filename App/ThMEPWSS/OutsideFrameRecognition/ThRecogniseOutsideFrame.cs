using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.OutsideFrameRecognition
{
    public class ThRecogniseOutsideFrame
    {
        public void DisplayOutsideFrameForTest()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                List<Polyline> plys = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    try
                    {
                        var frame = adb.Element<Polyline>(obj);
                        if (frame.Layer == "AI-房间框线")
                            plys.Add(ThMEPFrameService.Normalize(frame.Clone() as Polyline));
                    }
                    catch { }
                }
                plys = GetOutsideFrame(plys);
                foreach (var p in plys)
                {
                    p.ColorIndex = 13;
                    p.AddToCurrentSpace();
                }
            }
        }

        /// <summary>
        /// 获取房间框线集的外包络框
        /// roomframes:房间框线
        /// tol_grouped:房间框线分组距离值
        /// concave_param:计算外包框距离调节参数
        /// </summary>
        /// <param name="roomframes"></param>
        /// <param name="tol_grouped"></param>
        /// <param name="concave_param"></param>
        /// <returns></returns>
        public static List<Polyline> GetOutsideFrame(List<Polyline> roomframes, double tol_grouped = 1500, double concave_param = 1000)
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var outsideframes = new List<Polyline>();
                if (roomframes.Count < 1) return roomframes;
                roomframes = SortPolylineBasedArea(roomframes);
                roomframes.Reverse();
                var plys = new List<Polyline>();
                roomframes.ForEach(p => plys.Add(p));
                var grouped_plys = new List<List<Polyline>>();
                group_polylines_optimizeindex(plys, ref grouped_plys, tol_grouped);
                List<DBObjectCollection> objs_lists = new();
                for (int i = 0; i < grouped_plys.Count; i++)
                {
                    DBObjectCollection objs = new();
                    grouped_plys[i].ForEach(p => objs.Add(p));
                    objs_lists.Add(objs);
                }
                foreach (var objs in objs_lists)
                {
                    var concaveBuilder = new ThMEPConcaveBuilder(objs, concave_param);
                    var objConcaveHull = concaveBuilder.Build();
                    outsideframes.AddRange(objConcaveHull.Cast<Polyline>().ToList());
                }
                return outsideframes;
            }
        }

        /// <summary>
        /// 将房间框线按照距离分组
        /// </summary>
        /// <param name="plys"></param>
        /// <param name="grouped_plys"></param>
        /// <param name="tol"></param>
        private static void group_polylines_optimizeindex(List<Polyline> plys, ref List<List<Polyline>> grouped_plys, double tol)
        {
            if (plys.Count == 0) return;
            for (int i = 0; i < plys.Count; i++)
            {
                if (plys[i].Buffer(tol).Count == 0)
                {
                    plys.RemoveAt(i);
                    i--;
                }
            }
            grouped_plys = new();
            grouped_plys.Add(new List<Polyline>() { plys[0] });
            var ptcolls = new List<Point3dCollection>();
            ptcolls.Add(plys[0].Buffer(tol).Cast<Polyline>().First().EntityVertices());
            var collected_plys = new List<Polyline>();
            collected_plys.Add(plys[0]);
            var objs = new DBObjectCollection();
            plys.ForEach(p => objs.Add(p));
            plys.RemoveAt(0);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var tmp_plys = new List<List<Polyline>>();
            while (plys.Count > 0)
            {
                bool found_crossed = false;
                foreach (var ptcoll in ptcolls)
                {
                    var crossedplys = spatialIndex.SelectCrossingPolygon(ptcoll).Cast<Polyline>().ToList();
                    crossedplys = crossedplys.Where(c => !collected_plys.Contains(c)).ToList();
                    if (crossedplys.Count() > 0)
                    {
                        grouped_plys[grouped_plys.Count - 1].AddRange(crossedplys);
                        grouped_plys[grouped_plys.Count - 1] = grouped_plys[grouped_plys.Count - 1].Distinct().ToList();
                        tmp_plys.Add(crossedplys.ToList());
                        found_crossed = true;
                        crossedplys.ForEach(e => plys.Remove(e));
                    }
                }
                if (!found_crossed)
                {
                    grouped_plys.Add(new List<Polyline>() { plys[0] });
                    ptcolls = new();
                    ptcolls.Add(plys[0].Buffer(tol).Cast<Polyline>().First().EntityVertices());
                    collected_plys.Add(plys[0]);
                    plys.RemoveAt(0);
                }
                else
                {
                    ptcolls = new();
                    foreach (var plslist in tmp_plys)
                    {
                        foreach (var pls in plslist)
                        {
                            var pl = pls.Buffer(tol).Cast<Polyline>().OrderByDescending(p => p.Area).First();
                            ptcolls.Add(pl.EntityVertices());
                            if (!collected_plys.Contains(pls))
                                collected_plys.Add(pls);
                        }
                    }
                    tmp_plys = new();
                }
            }
        }

        public static MPolygon CreatMpolygonFromBoundaryToInfinity(List<Polyline> plys, double buffer_distance = 30000)
        {
            List<Point3d> oripoints = new List<Point3d>();
            plys.ForEach(p => oripoints.AddRange(p.EntityVertices().Cast<Point3d>().ToList()));
            List<Point3d> convex_points = Algorithms.GetConvexHull(oripoints);
            Polyline ply = new Polyline();
            for (int i = 0; i < convex_points.Count; i++)
            {
                ply.AddVertexAt(i, convex_points[i].ToPoint2d(), 0, 0, 0);
            }
            ply.AddVertexAt(ply.Vertices().Count, ply.Vertices()[0].ToPoint2d(), 0, 0, 0);
            if (ply.BufferPL(buffer_distance).Cast<Polyline>().ToList().Count > 0)
            {
                ply = ply.BufferPL(buffer_distance).Cast<Polyline>().First();
            }
            List<Curve> crvs = plys.Cast<Curve>().ToList();
            var m= ThMPolygonTool.CreateMPolygon(ply, crvs);
            return ThMPolygonTool.CreateMPolygon(ply, crvs);
        }

        private static List<Polyline> SortPolylineBasedArea(List<Polyline> plys)
        {
            var comparer = new PolyComparer();
            plys.Sort(comparer);
            return plys;
        }

        private class PolyComparer : IComparer<Polyline>
        {
            public PolyComparer()
            {
            }
            public int Compare(Polyline a, Polyline b)
            {
                if (a.Area == b.Area) return 0;
                else if (a.Area < b.Area) return -1;
                else return 1;
            }
        }
    }
}