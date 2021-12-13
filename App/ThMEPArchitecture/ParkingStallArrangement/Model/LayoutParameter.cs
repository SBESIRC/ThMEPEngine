using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LayoutParameter
    {
        public Polyline OuterBoundary { get; set; }//最外包围框
        public List<int> AreaNumber { get; set; }//区域索引，从0开始
        public DBObjectCollection Obstacles { get; set; }//所有障碍物
        public List<Line> SegLines { get; set; }//所有分割线
        public List<Polyline> Areas { get; set; }//所有区域包围框
        public Dictionary<int, Polyline> AreaDic { get; set; }//区域包围框
        public Dictionary<int, List<Polyline>> AreaWalls { get; set; }//区域墙线
        public Dictionary<int, List<Line>> AreaSegs { get; set; }//区域分割线
        public Dictionary<int, List<BlockReference>> ObstacleDic { get; set; }//区域内的障碍物块

        public Dictionary<int, List<List<Polyline>>> ObstaclesList { get; set; }//区域内的障碍物线
        public Dictionary<int, List<Polyline>> BuildingBoxes { get; set; }//区域内的障碍物boundingbox
        public Dictionary<int, List<Line>> SegLineDic { get; set; }//区域边界分割线
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引

        public LayoutParameter(Polyline outerBoundary, DBObjectCollection obstacles, List<Line> segLines)
        {
            OuterBoundary = outerBoundary;
            Obstacles = obstacles;
            SegLines = segLines;
            AreaNumber = new List<int>();
            Areas = new List<Polyline>();
            Areas.Add(outerBoundary);
            AreaDic = new Dictionary<int, Polyline>();
            AreaWalls = new Dictionary<int, List<Polyline>>();
            AreaSegs = new Dictionary<int, List<Line>>();
            ObstacleDic = new Dictionary<int, List<BlockReference>>();
            ObstaclesList = new Dictionary<int, List<List<Polyline>>>();
            BuildingBoxes = new Dictionary<int, List<Polyline>>();
            SegLineDic = new Dictionary<int, List<Line>>();
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacles);
        }

        public void Set(List<Gene> genome)
        {
            var areas = new List<Polyline>();
            areas.Add(OuterBoundary);
            SegLines.Clear();

            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                Split(gene, ref areas);
                SegLines.Add(GetSegLine(gene));
            }
            SegLineSpatialIndex = new ThCADCoreNTSSpatialIndex(SegLines.ToCollection());
            AreaNumber.Clear();
            Areas.Clear();
            AreaDic.Clear();
            ObstacleDic.Clear();
            SegLineDic.Clear();
            AreaWalls.Clear();
            AreaSegs.Clear();
            BuildingBoxes.Clear();
            ObstaclesList.Clear();

            Areas.AddRange(areas);
            for (int i = 0; i < areas.Count; i++)
            {
                AreaNumber.Add(i);
                AreaDic.Add(i, areas[i]);
                var buildings = GetObstacles(areas[i]);
                ObstacleDic.Add(i, buildings);
                SegLineDic.Add(i, GetSegLines(areas[i]));
                AreaSegs.Add(i, GetAreaSegs(areas[i], SegLineDic[i], out List<Polyline> areaWall));
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
            }
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
                if (segLines.Count == 0)
                {
                    //using (AcadDatabase acadDatabase = AcadDatabase.Active())
                    //{
                    //    acadDatabase.CurrentSpace.Add(area);
                    //}
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                        if (IsParallel(l.Angle, seg.Angle))
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
        private bool IsParallel(double ang1, double ang2, double tor = 0.035)
        {
            var minAngle = Math.Min(ang1, ang2);
            var maxAngle = Math.Max(ang1, ang2);
            while(minAngle < maxAngle + tor)
            {
                if(Math.Abs(minAngle - maxAngle) < tor)
                {
                    return true;
                }
                minAngle += Math.PI;
            }
            return false;
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
