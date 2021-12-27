﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;
using ThMEPEngineCore.UCSDivisionService.Utils;

namespace ThMEPEngineCore.UCSDivisionService.DivisionMethod
{
    public class GridDivision
    {
        ThCADCoreNTSSpatialIndex thWallNTSSpatialIndex;
        public List<Polyline> Division(List<List<Curve>> grids, List<Polyline> walls)
        {
            thWallNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());

            //将所有轴网打成line
            var gridLines = grids.Select(x => GeoUtils.ConvertToLine(x, 500)).ToList();

            //获得所有轴网的网格区域
            var gridAreas = GetGridArea(gridLines.SelectMany(x => x).ToList());

            //获得每个轴网的凸包
            var gridHulls = gridLines.ToDictionary(x => x, y => GetGridHull(y));

            //获得每个轴网本来所占区域
            var gridRegion = gridLines.ToDictionary(x => x, y => GetGridRegion(y));

            //划分轴网属于哪个区域
            var gridDics = ClassifyGridArea(gridAreas, gridHulls, gridRegion);

            //还原出ucs的polygon
            var polygons = GetUCSPolygons(gridDics, grids.SelectMany(x => x).ToList());

            return polygons;//gridRegion.Values.ToList();//
        }

        /// <summary>
        /// 计算出ucs区域
        /// </summary>
        /// <param name="gridDics"></param>
        /// <param name="curves"></param>
        /// <returns></returns>
        private List<Polyline> GetUCSPolygons(Dictionary<List<Line>, DBObjectCollection> gridDics, List<Curve> curves)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatial = new ThCADCoreNTSSpatialIndex(curves.ToCollection());
            var ucsPolys = new List<Polyline>();
            foreach (var dics in gridDics)
            {
                var bufferPolys = dics.Value.Cast<Polyline>().SelectMany(x => x.Buffer(10).Cast<Polyline>()).ToList();
                var polygons = bufferPolys.ToCollection().UnionPolygons().Cast<Polyline>().SelectMany(x => x.Buffer(-10).Cast<Polyline>()).Where(x => x.Area > 10).ToList();
                foreach (var poly in polygons)
                {
                    var ucsPolygon = poly.ResetArcPolygon(curves);
                    ucsPolys.Add(ucsPolygon);
                }
            }

            return ucsPolys;
        }

        /// <summary>
        /// 分类分割区域属于哪个轴网
        /// </summary>
        /// <param name="areas"></param>
        /// <param name="gridHulls"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        private Dictionary<List<Line>, DBObjectCollection> ClassifyGridArea(List<Polyline> areas, Dictionary<List<Line>, Polyline> gridHulls, Dictionary<List<Line>, Polyline> gridRegions)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(areas.ToCollection());
            Dictionary<List<Line>, DBObjectCollection> areaDic = new Dictionary<List<Line>, DBObjectCollection>();
            foreach (var region in gridRegions)
            {
                var containsAreas = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(region.Value);
                areaDic.Add(region.Key, containsAreas);
            }

            foreach (var area in areas)
            {
                var dicInfo = areaDic.Where(x => x.Value.Contains(area)).ToDictionary(x => x.Key, y => y.Value);
                if (dicInfo.Count == 1)
                {
                    continue;
                }

                foreach (var dic in dicInfo)
                {
                    areaDic[dic.Key].Remove(area);
                }
                var intersectHulls = dicInfo.ToDictionary(x => x.Key, y => gridRegions[y.Key]);
                if (intersectHulls.Count <= 0)
                {
                    intersectHulls = gridHulls.Where(x => x.Value.Intersects(area)).ToDictionary(x => x.Key, y => y.Value);
                }
                if (intersectHulls.Count() > 1)
                {
                    var belongPoly = CheckAreaBelong(area, intersectHulls);
                    areaDic[belongPoly].Add(area);
                }
                else if (intersectHulls.Count == 1)
                {
                    areaDic[intersectHulls.First().Key].Add(area);
                }
            }

            return areaDic;
        }

        /// <summary>
        /// 计算当前分割区域属于哪个轴网
        /// </summary>
        /// <param name="area"></param>
        /// <param name="gridArea"></param>
        /// <returns></returns>
        private List<Line> CheckAreaBelong(Polyline area, Dictionary<List<Line>, Polyline> gridArea)
        {
            var walls = thWallNTSSpatialIndex.SelectCrossingPolygon(area);
            if (walls.Count > 0)
            {
                walls = area.Intersection(walls);
                var dir = GetWallDir(walls.Cast<Polyline>().ToList());
                foreach (var gArea in gridArea)
                {
                    var gAreaDir = GetAreaDir(gArea.Value);
                    if (dir.IsParallelTo(gAreaDir, new Tolerance(0.01, 0.01)) || Math.Abs(gAreaDir.DotProduct(dir)) < 0.01)  //优先归入与墙方向一致的区域
                    {
                        return gArea.Key;
                    }
                }
            }

            var areaDir = GetAreaDir(area);
            foreach (var gArea in gridArea)   //后归入方向将近一致的区域
            {
                var gAreaDir = GetAreaDir(gArea.Value);
                if (areaDir.IsParallelTo(gAreaDir, new Tolerance(0.1, 0.1)) || Math.Abs(gAreaDir.DotProduct(areaDir)) < 0.1)
                {
                    return gArea.Key;
                }
            }

            Dictionary<List<Line>, double> belongDic = new Dictionary<List<Line>, double>();
            foreach (var gArea in gridArea)   //最后归入占比最多的区域
            {
                var intersectArea = area.GeometryIntersection(gArea.Value).Cast<Polyline>().ToList();
                belongDic.Add(gArea.Key, intersectArea.Sum(x => x.Area) / area.Area);
            }
            var belongPoly = belongDic.OrderBy(x => x.Value).First().Key;
            return belongPoly;
        }

        /// <summary>
        /// 计算墙方向
        /// </summary>
        /// <param name="wall"></param>
        /// <returns></returns>
        private Vector3d GetWallDir(List<Polyline> walls)
        {
            var wallLines = walls.SelectMany(x => x.GetLinesByPolyline(500)).ToList();
            var lineDic = new Dictionary<Vector3d, List<Line>>();
            foreach (var line in wallLines)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                if (lineDic.Keys.Any(x => dir.IsParallelTo(x, new Tolerance(0.01, 0.01))) || lineDic.Keys.Any(x => Math.Abs(x.DotProduct(dir)) < 0.01))
                {
                    lineDic[dir].Add(line);
                }
                else
                {
                    lineDic.Add(dir, new List<Line>() { line });
                }
            }

            lineDic = lineDic.OrderBy(x => x.Value.Sum(y => y.Length)).ToDictionary(x => x.Key, y => y.Value);
            return lineDic.First().Key;
        }

        /// <summary>
        /// 计算区域方向
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        private Vector3d GetAreaDir(Polyline area)
        {
            var allLines = area.GetLinesByPolyline(500);
            var lineDic = new Dictionary<Vector3d, List<Line>>();
            foreach (var line in allLines)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var defaultKey = lineDic.Keys.FirstOrDefault(x => dir.IsParallelTo(x, new Tolerance(0.01, 0.01)) || Math.Abs(x.DotProduct(dir)) < 0.01);
                if (default(Vector3d) != defaultKey)
                {
                    lineDic[defaultKey].Add(line);
                }
                else
                {
                    lineDic.Add(dir, new List<Line>() { line });
                }
            }

            lineDic = lineDic.OrderBy(x => x.Value.Sum(y => y.Length)).ToDictionary(x => x.Key, y => y.Value);
            return lineDic.First().Key;
        }

        /// <summary>
        /// 计算轴网区域
        /// </summary>
        /// <param name="gridLines"></param>
        /// <returns></returns>
        private List<Polyline> GetGridArea(List<Line> gridLines)
        {
            return gridLines.ToCollection().PolygonsEx().Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 计算轴网外包框
        /// </summary>
        /// <param name="gridLines"></param>
        /// <returns></returns>
        private Polyline GetGridHull(List<Line> gridLines)
        {
            var gridPts = StructUtils.GetCurvePoints(gridLines.Cast<Curve>().ToList());
            return ThCADCoreNTSPoint3dCollectionExtensions.ConvexHull(gridPts).ToDbCollection().Cast<Polyline>().OrderBy(x => x.Area).First();
        }

        /// <summary>
        /// 计算单个轴网原本的区域
        /// </summary>
        /// <param name="gridLines"></param>
        /// <returns></returns>
        private Polyline GetGridRegion(List<Line> gridLines)
        {
            return gridLines.ToCollection().PolygonsEx().UnionPolygons().Cast<Polyline>().First().Buffer(-10)[0] as Polyline;
        }
    }
}
