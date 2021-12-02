﻿using Autodesk.AutoCAD.DatabaseServices;
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
        public List<Polyline> Obstacles { get; set; }//所有障碍物
        public List<Line> SegLines { get; set; }//所有分割线
        public List<Polyline> Areas { get; set; }//所有区域包围框
        public Dictionary<int, Polyline> AreaDic { get; set; }//区域包围框
        public Dictionary<int, List<Polyline>> AreaWalls { get; set; }//区域墙线
        public Dictionary<int, List<Line>> AreaSegs { get; set; }//区域分割线
        public Dictionary<int, List<Polyline>> ObstacleDic { get; set; }//区域内的障碍物
        public Dictionary<int, List<Line>> SegLineDic { get; set; }//区域边界分割线
        public ThCADCoreNTSSpatialIndex ObstacleSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引

        public LayoutParameter(Polyline outerBoundary, List<Polyline> obstacles, List<Line> segLines)
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
            ObstacleDic = new Dictionary<int, List<Polyline>>();
            SegLineDic = new Dictionary<int, List<Line>>();
            ObstacleSpatialIndex = new ThCADCoreNTSSpatialIndex(obstacles.ToCollection());
        }

        public void Set(List<Gene> genome)
        {
            var areas = new List<Polyline>();
            areas.Add(OuterBoundary);
            SegLines.Clear();
            if(genome.Count > 3)
            {
                ;
            }
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


            Areas.AddRange(areas);
            for (int i = 0; i < areas.Count; i++)
            {
                AreaNumber.Add(i);
                AreaDic.Add(i, areas[i]);
                ObstacleDic.Add(i, GetObstacles(areas[i]));
                SegLineDic.Add(i, GetSegLines(areas[i]));
                AreaSegs.Add(i, GetAreaSegs(areas[i], SegLineDic[i], out List<Polyline> areaWall));
                AreaWalls.Add(i, areaWall);

                ;
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

        private List<Polyline> GetObstacles(Polyline area)
        {
            var obstacles = new List<Polyline>();
            var dbObjs = ObstacleSpatialIndex.SelectCrossingPolygon(area);
            dbObjs.Cast<Entity>()
                .ForEach(e => obstacles.Add(e as Polyline));
            return obstacles;
        }

        private List<Line> GetSegLines(Polyline area)
        {
            var segLines = new List<Line>();
            var newArea = area.Buffer(1.0).OfType<Polyline>().OrderByDescending(p => p.Area).First();
            var dbObjs = SegLineSpatialIndex.SelectCrossingPolygon(newArea);
            dbObjs.Cast<Entity>()
                .ForEach(e => segLines.Add(e as Line));
            if(segLines.Count == 0)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    acadDatabase.CurrentSpace.Add(area);
                }
            }
            return segLines;
        }

        private List<Polyline> GetWallLines(Polyline area)
        {
            var wallLines = new List<Polyline>();

            return wallLines;
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

            //for(int i = 0; i < lines.Count; i++)
            //{
            //    if(i==0)
            //    {
            //        var pts = new List<Point2d>();
            //        pts.Add(lines[i].StartPoint.ToPoint2D());
            //        pts.Add(lines[i].EndPoint.ToPoint2D());
            //        plines.Add(pts);
            //        continue;
            //    }
            //    var line = lines[i];
            //    var spt = line.StartPoint.ToPoint2D();
            //    var ept = line.EndPoint.ToPoint2D();
            //    foreach(var pts in plines)
            //    {
            //        var pt1 = pts[0];
            //        var pt2 = pts[pts.Count - 1];
            //        if(spt.GetDistanceTo(pt1) < 1)
            //        {
            //            var temp = new List<Point2d>();
            //            temp.Add(ept);
            //            temp.AddRange(pts);
            //            pts.Clear();
            //            pts.AddRange(temp);
            //            break;
            //        }
            //        if (spt.GetDistanceTo(pt1) < 1)
            //        {
            //            var temp = new List<Point2d>();
            //            temp.Add(spt);
            //            temp.AddRange(pts);
            //            pts.Clear();
            //            pts.AddRange(temp);
            //            break;
            //        }
            //        if (spt.GetDistanceTo(pt2) < 1)
            //        {
            //            pts.Add(ept);
            //            break;
            //        }
            //        if (ept.GetDistanceTo(pt2) < 1)
            //        {
            //            pts.Add(spt);
            //            break;
            //        }
            //    }
            //    var pts2 = new List<Point2d>();
            //    pts2.Add(spt);
            //    pts2.Add(ept);
            //    plines.Add(pts2);
            //}
            //if(plines.Count >= 2)//多段线数目大于2，尝试合并
            //{
            //    for (int i = 0; i < plines.Count - 1; i++)
            //    {
            //        for(int j = i+1; j < plines.Count; j++)
            //        {

            //        }
            //    }
            //}
            areaWalls = new List<Polyline>();
            foreach (var pts in plines)
            {
                var pline = new Polyline();
                pline.CreatePolyline(pts.ToCollection());
;               areaWalls.Add(pline);
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
}
