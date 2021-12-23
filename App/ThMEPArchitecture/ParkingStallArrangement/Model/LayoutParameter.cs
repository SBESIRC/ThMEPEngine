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
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LayoutParameter:IDisposable
    {
        public Polyline InitialWalls { get; set; }//初始外包框，不被disposal
        public Polyline OuterBoundary { get; set; }//最外包围框
        public List<int> AreaNumber { get; set; }//区域索引，从0开始
        public List<string> SubAreaNumber { get; set; }//子区域索引，从a开始
        public DBObjectCollection Obstacles { get; set; }//所有障碍物
        public List<Line> SegLines { get; set; }//所有分割线
        public List<Polyline> Areas { get; set; }//所有区域包围框
        public Dictionary<int, Polyline> AreaDic { get; set; }//区域包围框
        public Dictionary<int, List<Polyline>> AreaWalls { get; set; }//区域墙线
        public Dictionary<int, List<Line>> AreaSegs { get; set; }//区域分割线
        public Dictionary<int, List<BlockReference>> ObstacleDic { get; set; }//区域内的障碍物块
        public Dictionary<int, List<List<Polyline>>> ObstaclesList { get; set; }//区域内的障碍物线
        public Dictionary<int, List<Polyline>> BuildingBoxes { get; set; }//区域内的障碍物boundingbox
        public Dictionary<int, List<Line>> SegLineDic { get; set; }//区域边界分割线, key表示区域index
        public Dictionary<int, Line> SegLineIndexDic { get; set; }//区域边界分割线, key表示直线本身的index
        public Dictionary<int, List<int>> AreaSegLineDic { get; set; }//key表示区域索引，value表示线索引
        public Dictionary<int, bool> PtDirectionList { get; set; }//交点的方向
        public Dictionary<int, List<int>> PtDic { get; set; }//交点的线索引
        public Dictionary<string, Polyline> SubAreaDic { get; set; }//子区域，"1a"表示区域1的a子区域，且a表示包含建筑物
        public Dictionary<string, Polyline> SubAreaWallLineDic { get; set; }//子区域的墙线
        public Dictionary<string, Polyline> SubAreaSegLineDic { get; set; }//子区域的车道线
        public List<Point3d> IntersectPt { get; set; }//交点列表
        public Dictionary<LinePairs, int> LinePtDic { get; set; }
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引

        public LayoutParameter(Polyline outerBoundary, DBObjectCollection obstacles, List<Line> segLines, Dictionary<int, List<int>> ptDic, Dictionary<int, bool> directionList,
            Dictionary<LinePairs, int> linePtDic)
        {
            InitialWalls = outerBoundary.Clone() as Polyline;
            OuterBoundary = outerBoundary;
            Obstacles = obstacles;
            SegLines = segLines;
            AreaNumber = new List<int>();
            SubAreaNumber = new List<string>() { "a", "b", "c", "d", "e" };
            Areas = new List<Polyline>();
            Areas.Add(outerBoundary);
            AreaDic = new Dictionary<int, Polyline>();
            AreaWalls = new Dictionary<int, List<Polyline>>();
            AreaSegs = new Dictionary<int, List<Line>>();
            ObstacleDic = new Dictionary<int, List<BlockReference>>();
            ObstaclesList = new Dictionary<int, List<List<Polyline>>>();
            BuildingBoxes = new Dictionary<int, List<Polyline>>();
            SegLineDic = new Dictionary<int, List<Line>>();
            SubAreaDic = new Dictionary<string, Polyline>();
            SubAreaWallLineDic = new Dictionary<string, Polyline>();
            SubAreaSegLineDic = new Dictionary<string, Polyline>();
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacles);
            PtDic = ptDic;
            PtDirectionList = directionList;
            IntersectPt = new List<Point3d>();
            SegLineIndexDic = new Dictionary<int, Line>();
            AreaSegLineDic = new Dictionary<int, List<int>>();
            LinePtDic = linePtDic;
        }

        public void Clear()
        {
            AreaNumber.Clear();

            foreach (var line in SegLines)
            {
                line.Dispose();
            }
            SegLines.Clear();

            foreach(var pline in Areas)
            {
                pline.Dispose();
            }
            Areas.Clear();

            foreach(var pline in AreaDic.Values)
            {
                pline.Dispose();
            }
            AreaDic.Clear();

            foreach(var blocks in ObstacleDic.Values)
            {
                foreach(var block in blocks)
                {
                    block.Dispose();
                }
                blocks.Clear();
            }
            ObstacleDic.Clear();

            foreach (var lines in SegLineDic.Values)
            {
                foreach (var line in lines)
                {
                    line.Dispose();
                }
                lines.Clear();
            }
            SegLineDic.Clear();

            foreach (var plines in AreaWalls.Values)
            {
                foreach (var pline in plines)
                {
                    pline.Dispose();
                }
                plines.Clear();
            }
            AreaWalls.Clear();

            foreach (var lines in AreaSegs.Values)
            {
                foreach (var line in lines)
                {
                    line.Dispose();
                }
                lines.Clear();
            }
            AreaSegs.Clear();

            foreach (var plines in BuildingBoxes.Values)
            {
                foreach (var pline in plines)
                {
                    pline.Dispose();
                }
                plines.Clear();
            }
            BuildingBoxes.Clear();

            foreach (var plinesList in ObstaclesList.Values)
            {
                foreach(var plines in plinesList)
                {
                    foreach (var pline in plines)
                    {
                        pline.Dispose();
                    }
                    plines.Clear();
                }
                plinesList.Clear();
            }
            ObstaclesList.Clear();
        }
        public void Clear2()
        {
            AreaNumber.Clear();
            SegLines.Clear();
            Areas.Clear();
            AreaDic.Clear();
            ObstacleDic.Clear();
            SegLineDic.Clear();
            AreaSegLineDic.Clear();
            AreaWalls.Clear();
            AreaSegs.Clear();
            BuildingBoxes.Clear();
            ObstaclesList.Clear();
            SubAreaDic.Clear();
            IntersectPt.Clear();
            SegLineIndexDic.Clear();
        }
        public void Set(List<Gene> genome)
        {
            var areas = new List<Polyline>();
            areas.Add(InitialWalls);
            Clear2();//清空所有参数
            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                Split(gene, ref areas);
                var line = GetSegLine(gene);
                SegLines.Add(line);
                SegLineIndexDic.Add(i, line);
            }
            SegLineSpatialIndex = new ThCADCoreNTSSpatialIndex(SegLines.ToCollection());

            Areas.AddRange(areas);

            for (int i = 0; i < PtDic.Count; i++)
            {
                var line1 = SegLines[PtDic[i][0]];//拿到第一根分割线
                var line2 = SegLines[PtDic[i][1]];//拿到第二根分割线
                var pt = line1.Intersect(line2, Intersect.ExtendBoth).First();
                IntersectPt.Add(pt);//交点添加
            }

            for (int i = 0; i < Areas.Count; i++)
            {
                AreaNumber.Add(i);
                AreaDic.Add(i, Areas[i]);
                var buildings = GetObstacles(Areas[i]);
                ObstacleDic.Add(i, buildings);
                SegLineDic.Add(i, GetSegLines(Areas[i], out List<int> lineNums));
                AreaSegLineDic.Add(i, lineNums);
                AreaSegs.Add(i, GetAreaSegs(Areas[i], SegLineDic[i], out List<Polyline> areaWall));
                AreaWalls.Add(i, areaWall);

                var bdBoxes = new List<Polyline>();//临时建筑物外包线
                var obstacles = new List<List<Polyline>>();//临时建筑物框线
                foreach(var build in buildings)
                {
                    var rect = build.GetRect();
                    var obstacle = build.GetPlines();
                    bdBoxes.Add(rect);
                    obstacles.Add(obstacle);
                }
                BuildingBoxes.Add(i, bdBoxes);
                ObstaclesList.Add(i, obstacles);

                var pointNums = new List<int>();
                var directions = new List<int>();
                if (lineNums.Count > 1)//存在两根以上的分割线
                {
                    for (int k = 0; k < lineNums.Count - 1; k++)
                    {
                        for (int j = 1; j < lineNums.Count; j++)
                        {
                            var linePair = new LinePairs(lineNums[k], lineNums[j]);
                            if (LinePtDic.ContainsKey(linePair))
                            {
                                var ptNum = LinePtDic[linePair];//拿到点的编号
                                var ptDir = Convert.ToInt32(PtDirectionList[ptNum]);//拿到点的方向
                                if (!pointNums.Contains(ptNum))
                                {
                                    pointNums.Add(ptNum);
                                    directions.Add(ptDir);
                                }
                            }
                        }
                    }
                }
                {
                    
                    //var subArea = PointAreaSeg.PtAreaSeg(areas[i], pointNums, directions, bdBoxes, IntersectPt);//子区域分割

                    //if (bdBoxes.Count == 0)//这个区域没有建筑物
                    //{
                    //    SubAreaDic.Add(Convert.ToString(i) + "a", null);
                    //    for (int k = 0; k < subArea.Count; k++)//子区域遍历
                    //    {
                    //        var sub = subArea[k];
                    //        SubAreaDic.Add(Convert.ToString(i) + SubAreaNumber[k + 1], sub);
                    //    }
                    //}
                    //else
                    //{
                    //    var bdBoxesSpatialIndex = new ThCADCoreNTSSpatialIndex(bdBoxes.ToCollection());//创建建筑物的空间索引
                    //    var index = 0;
                    //    for (int k = 0; k < subArea.Count; k++)//子区域遍历
                    //    {
                    //        var sub = subArea[k];
                    //        var rst = bdBoxesSpatialIndex.SelectCrossingPolygon(sub);
                    //        if (rst.Count > 0)//当前子区域包含建筑物
                    //        {
                    //            SubAreaDic.Add(Convert.ToString(i) + "a", sub);
                    //            index = k;
                    //            break;
                    //        }
                    //    }
                    //    var curIndex = 1;
                    //    for (int k = 0; k < subArea.Count; k++)//子区域遍历
                    //    {
                    //        if (k == index)
                    //        {
                    //            continue;
                    //        }
                    //        var sub = subArea[k];
                    //        SubAreaDic.Add(Convert.ToString(i) + SubAreaNumber[curIndex], sub);
                    //        curIndex++;
                    //    }
                    //}
                }
            }

            //foreach (var area in SubAreaDic.Values)
            //{
            //    using (AcadDatabase currentDb = AcadDatabase.Active())
            //    {

            //        currentDb.CurrentSpace.Add(area);
            //    }
            //}
        }
        private Line GetSegLine(Gene gene)
        {
            Point3d spt, ept;
            if(gene.Direction)
            {
                spt = new Point3d(gene.Value, gene.StartValue, 0);
                ept = new Point3d(gene.Value, gene.EndValue, 0);
            }
            else
            {
                spt = new Point3d(gene.StartValue, gene.Value, 0);
                ept = new Point3d(gene.EndValue, gene.Value, 0);
            }
            return new Line(spt, ept);
        }
        private void Split(Gene gene, ref List<Polyline> areas)
        {
            var line = gene.ToLine();//对于每一条线
            if(AreaSplit.IsCorrectSegLines(line, ref areas))
            {
                return;
            }
            else
            {
                AreaSplit.IsCorrectSegLines2(line, ref areas);
            }
        }
        private List<BlockReference> GetObstacles(Polyline area)
        {
            var obstacles = new List<BlockReference>();
            var dbObjs = ObstacleSpatialIndex.SelectCrossingPolygon(area);
            dbObjs.Cast<BlockReference>()
                .ForEach(e => obstacles.Add(e));
            return obstacles;
        }
        private List<Line> GetSegLines(Polyline area)
        {
            var segLines = new List<Line>();
            try
            {
                var newArea = area.Buffer(1.0).OfType<Polyline>().OrderByDescending(p => p.Area).First();
                var dbObjs = SegLineSpatialIndex.SelectCrossingPolygon(newArea);
                dbObjs.Cast<Entity>()
                    .ForEach(e => segLines.Add(e as Line));

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return segLines;
        }
        private List<Line> GetSegLines(Polyline area, out List<int> lineNums)
        {
            var segLines = new List<Line>();
            lineNums = new List<int>();
            var pts = area.GetPoints();
            var dbObjs = SegLineSpatialIndex.SelectCrossingPolygon(area);
            var areaColl = new List<Polyline>() { area };
            var areaIndex = new ThCADCoreNTSSpatialIndex(areaColl.ToCollection());
            foreach (var db in dbObjs)
            {
                
                var line = db as Line;
                var extendLine = line.ExtendLine(-10.0);
                
                var rect = extendLine.Buffer(1.0);
                var rst = areaIndex.SelectCrossingPolygon(rect);
                if(rst.Count > 0)
                {
                    segLines.Add(line);
                }
            }
            for (int i = 0; i < SegLineIndexDic.Count; i++)
            {
                var line = SegLineIndexDic[i];
                for (int j = 0; j < segLines.Count; j++)
                {
                    if (line.EqualsTo(segLines[j]))
                    {
                        lineNums.Add(i);
                    }
                }
            }

            return segLines;
        }
        private List<Line> GetAreaSegs(Polyline area, List<Line> allSegs, out List<Polyline> areaWalls)
        {
            var segLines = new List<Line>();
            var lines = new List<Line>();
            for(int i = 0; i < area.NumberOfVertices-1; i++)
            {
                var line = new Line(area.GetPoint3dAt(i), area.GetPoint3dAt(i+1));
                lines.Add(line);
            }
            var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            foreach (var seg in allSegs)
            {
                var rst = lineSpatialIndex.SelectCrossingPolygon(seg.Buffer(1.0));
                if(rst.Count == 0)
                {
                    ;
                }
                else
                {
                    foreach(var r in rst)
                    {
                        var l = r as Line;
                        if (l.Angle.IsParallel(seg.Angle))
                        {
                            segLines.Add(l);//把分割线添加进去
                            lines.Remove(l);
                        }
                    }
                }
            }
            var ptDic = new Dictionary<Point3dEx, List<Point3dEx>>();
            foreach(var line in lines)
            {
                var spt = new Point3dEx(line.StartPoint);
                var ept = new Point3dEx(line.EndPoint);
                if(ptDic.ContainsKey(spt))
                {
                    ptDic[spt].Add(ept);
                }
                else
                {
                    ptDic.Add(spt, new List<Point3dEx>() { ept });
                }

                if (ptDic.ContainsKey(ept))
                {
                    ptDic[ept].Add(spt);
                }
                else
                {
                    ptDic.Add(ept, new List<Point3dEx>() { spt });
                }
            }
            var termPts = new List<Point3dEx>();
            foreach(var pt in ptDic.Keys)
            {
                if(ptDic[pt].Count == 1)
                {
                    termPts.Add(pt);
                }
            }

            var plines = new List<List<Point2d>>();
            var visited = new HashSet<Point3dEx>();
            while (termPts.Count() > 0)
            {
                var pts = new List<Point2d>();
                var spt = termPts[0];
                termPts.RemoveAt(0);
                Dfs(spt, ref termPts, ref pts, ref visited, ptDic);
                plines.Add(pts);
            }

            areaWalls = new List<Polyline>();
            foreach (var pts in plines)
            {
                var pline = new Polyline();
                pline.CreatePolyline(pts.ToCollection());
                areaWalls.Add(pline);
            }

            return segLines;
        }
        private void Dfs(Point3dEx cur, ref List<Point3dEx> termPts, ref List<Point2d> pts, ref HashSet<Point3dEx> visited, Dictionary<Point3dEx, List<Point3dEx>> ptDic)
        {
            if(termPts.Contains(cur))
            {
                termPts.Remove(cur);
                pts.Add(new Point2d(cur.X, cur.Y));
                visited.Clear();
                return;
            }
            pts.Add(new Point2d(cur.X, cur.Y));
            visited.Add(cur);
            var neighbor = ptDic[cur];
            if(neighbor != null)
            {
                foreach(var pt in neighbor)
                {
                    if (visited.Contains(pt)) continue;
                    cur = pt;
                }
            }
            Dfs(cur, ref termPts, ref pts, ref visited, ptDic);
        }

        public void Dispose()
        {
            InitialWalls.Dispose();
            OuterBoundary.Dispose();
            AreaNumber.Clear();
            SubAreaNumber.Clear();
            Obstacles.Dispose();
            SegLines.ForEach(e => e.Dispose());
            SegLines.Clear();
            Areas.ForEach(e => e.Dispose());
            Areas.Clear();
            AreaDic.ForEach(e => e.Value.Dispose());
            AreaDic.Clear();
            AreaWalls.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            AreaWalls.Clear();
            AreaSegs.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            AreaSegs.Clear();
            ObstacleDic.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            ObstacleDic.Clear();
            ObstaclesList.ForEach(e => e.Value.ForEach(e1 => e1.ForEach(e2 => e2.Dispose())));
            ObstaclesList.Clear();
            BuildingBoxes.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            BuildingBoxes.Clear();
            SegLineDic.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            SegLineDic.Clear();
            SegLineIndexDic.ForEach(e => e.Value.Dispose());
            SegLineIndexDic.Clear();
            AreaSegLineDic.Clear();
            PtDirectionList.Clear();
            PtDic.Clear();
            SubAreaDic.ForEach(e => e.Value.Dispose());
            SubAreaDic.Clear();
            SubAreaWallLineDic.ForEach(e => e.Value.Dispose());
            SubAreaWallLineDic.Clear();
            SubAreaSegLineDic.ForEach(e => e.Value.Dispose());
            SubAreaSegLineDic.Clear();
            IntersectPt.Clear();
            LinePtDic.Clear();
            ObstacleSpatialIndex.Dispose();
            SegLineSpatialIndex.Dispose();
        }
    }

    public static class Plines
    {
        public static List<Polyline> GetPlines(this BlockReference br)
        {
            var plines = new List<Polyline>();
            var objs = new DBObjectCollection();
            br.Explode(objs);
            foreach (var obj in objs)
            {
                if (obj is Polyline pline)
                {
                    plines.Add(pline);
                }
            }
            return plines;
        }
    }
}
