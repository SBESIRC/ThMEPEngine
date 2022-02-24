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
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LayoutParameter:IDisposable
    {
        public Polyline InitialWalls { get; set; }//初始外包框
        public Polyline OuterBoundary { get; set; }//最外包围框，不被disposal
        public List<int> AreaNumber { get; set; }//区域索引，从0开始
        public DBObjectCollection BuildingBlocks { get; set; }//所有障碍物
        public List<Line> SegLines { get; set; }//所有分割线
        public List<Polyline> Areas { get; set; }//所有区域包围框
        public Dictionary<int, Polyline> Id2AllSubAreaDic { get; set; }//区域包围框
        public Dictionary<int, List<Polyline>> SubAreaId2OuterWallsDic { get; set; }//区域墙线
        public Dictionary<int, List<Line>> SubAreaId2SegsDic { get; set; }//区域分割线
        public Dictionary<int, List<BlockReference>> SubAreaId2BuildingBlockDic { get; set; }//区域内的障碍物块
        public Dictionary<int, List<List<Polyline>>> SubAreaId2ShearWallsDic { get; set; }//区域内的障碍物线,剪力墙等
        public Dictionary<int, List<Polyline>> BuildingBoxes { get; set; }//区域内的障碍物boundingbox
        public Dictionary<int, List<Line>> Id2AllSegLineDic { get; set; }//区域边界分割线, key表示区域index
        public Dictionary<int, Line> SegLineIndexDic { get; set; }//区域边界分割线, key表示直线本身的index
        public Dictionary<int, List<int>> AreaSegLineDic { get; set; }//key表示区域索引，value表示线索引
        public ThCADCoreNTSSpatialIndex BuildingBlockSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引
        public Dictionary<int, List<int>> SeglineNeighborIndexDic { get; set; }//分割线临近线
        public List<Ramps> RampList { get; set; }//坡道
        public int SegAreasCnt { get; set; }//初始分割线数目
        public bool UsePline { get; set; }//建筑物框线，true使用polyline，false使用hatch
        private Serilog.Core.Logger Logger { get; set; }

        private ThCADCoreNTSSpatialIndex _AllShearwallSpatialIndex = null;

        public ThCADCoreNTSSpatialIndex AllShearwallsSpatialIndex
        {
            get 
            { 
                if(_AllShearwallSpatialIndex == null)
                {
                    var allCuttersList = new List<List<Polyline>>();//临时建筑物框线
                    foreach (BlockReference buildingBlock in BuildingBlocks)
                    {
                        var cuttersInBuilding = buildingBlock.GetCutters(UsePline, Logger);

                        allCuttersList.Add(cuttersInBuilding);
                    }
                    var allCutters = allCuttersList.SelectMany(c => c).ToCollection();
                    _AllShearwallSpatialIndex = new ThCADCoreNTSSpatialIndex(allCutters);
                }
                return _AllShearwallSpatialIndex;
            }
        }

        private ThCADCoreNTSSpatialIndex _AllCuttersMPolygonSpatialIndex = null;

        public ThCADCoreNTSSpatialIndex AllShearwallsMPolygonSpatialIndex
        {
            get 
            { 
                if(_AllCuttersMPolygonSpatialIndex == null)
                {
                    var Cutters = AllShearwallsSpatialIndex.SelectAll();
                    var mpCutters = new List<MPolygon>();
                    foreach(Polyline c in Cutters)
                    {
                       var mp = c.ToNTSPolygon().ToDbMPolygon();
                       mpCutters.Add(mp);
                    }
                    _AllCuttersMPolygonSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
                }
                return _AllCuttersMPolygonSpatialIndex;
            }
        }

        public LayoutParameter()
        {
        }

        public LayoutParameter(OuterBrder outerBrder, Dictionary<int, List<int>> ptDic, Dictionary<int, List<int>> seglineNeighborIndexDic = null, 
            int segAreasCnt = 0, bool usePline = true, Serilog.Core.Logger logger = null)
        {
            var outerBoundary = outerBrder.WallLine;
            InitialWalls = outerBoundary.Clone() as Polyline;
            OuterBoundary = outerBoundary;
            BuildingBlocks = outerBrder.BuildingObjs;
            SegLines = outerBrder.SegLines;
            RampList = outerBrder.RampLists;
            AreaNumber = new List<int>();
            Areas = new List<Polyline>();
            Areas.Add(InitialWalls);
            Id2AllSubAreaDic = new Dictionary<int, Polyline>();
            SubAreaId2OuterWallsDic = new Dictionary<int, List<Polyline>>();
            SubAreaId2SegsDic = new Dictionary<int, List<Line>>();
            SubAreaId2BuildingBlockDic = new Dictionary<int, List<BlockReference>>();
            SubAreaId2ShearWallsDic = new Dictionary<int, List<List<Polyline>>>();
            BuildingBoxes = new Dictionary<int, List<Polyline>>();
            Id2AllSegLineDic = new Dictionary<int, List<Line>>();
            BuildingBlockSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingObjs);
            SegLineIndexDic = new Dictionary<int, Line>();
            AreaSegLineDic = new Dictionary<int, List<int>>();
            SeglineNeighborIndexDic = seglineNeighborIndexDic;
            SegAreasCnt = segAreasCnt;
            UsePline = usePline;
            Logger = logger;
        }

        public void Clear()
        {
            AreaNumber.Clear();

            SegLines.ForEach(e => e.Dispose());
            SegLines.Clear();

            Areas.ForEach(e => e.Dispose());
            Areas.Clear();

            Id2AllSubAreaDic.ForEach(e => e.Value.Dispose());
            Id2AllSubAreaDic.Clear();

            SubAreaId2BuildingBlockDic.Clear();

            foreach (var lines in Id2AllSegLineDic.Values)
            {
                foreach (var line in lines)
                {
                    line.Dispose();
                }
                lines.Clear();
            }
            Id2AllSegLineDic.Clear();

            AreaSegLineDic.Clear();

            foreach (var plines in SubAreaId2OuterWallsDic.Values)
            {
                foreach (var pline in plines)
                {
                    pline.Dispose();
                }
                plines.Clear();
            }
            SubAreaId2OuterWallsDic.Clear();

            foreach (var lines in SubAreaId2SegsDic.Values)
            {
                foreach (var line in lines)
                {
                    line.Dispose();
                }
                lines.Clear();
            }
            SubAreaId2SegsDic.Clear();

            foreach (var plines in BuildingBoxes.Values)
            {
                foreach (var pline in plines)
                {
                    pline.Dispose();
                }
                plines.Clear();
            }
            BuildingBoxes.Clear();

            SubAreaId2ShearWallsDic.Clear();

            SegLineIndexDic.ForEach(e => e.Value.Dispose());
            SegLineIndexDic.Clear();
        }

        public bool IsVaildGenome(List<Gene> Genome, ParkingStallArrangementViewModel parameterViewModel)
        {
            var tmpBoundary = OuterBoundary.Clone() as Polyline;
            var tmpSegLines = new List<Line>();
            var tmpSegLineIndexDic = new Dictionary<int, Line>();
            List<Polyline> areas = new List<Polyline>();
            try
            {
                for (int i = 0; i < Genome.Count; i++)
                {
                    Gene gene = Genome[i];
                    var line = gene.ToLine();
                    tmpSegLines.Add(line);
                    tmpSegLineIndexDic.Add(i, line);
                }
                
                //If in manual mode
                areas = WindmillSplit.Split(tmpBoundary, tmpSegLineIndexDic, BuildingBlockSpatialIndex, SeglineNeighborIndexDic);

                if (!IsReasonableAns(areas, tmpBoundary, tmpSegLines))
                {
                    return false;
                }

                foreach (var area in areas)
                {
                    //var areaSPIdx = new ThCADCoreNTSSpatialIndex(new List<Polyline> { area });
                    // 求area和building的交集
                    var buildLines = BuildingBlockSpatialIndex.SelectCrossingPolygon(area);
                    var pts = area.Intersect(OuterBoundary, Intersect.OnBothOperands);

                    if (buildLines.Count == 0)// 区域内没有建筑
                    {
                        if (area.Area < 0.25 * (parameterViewModel.RoadWidth * parameterViewModel.RoadWidth))//区域面积小于车道宽的平方
                        {
                            return false;
                        }
                        if (pts.Count == 0)//空腔
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);               
            }
            finally
            {
                tmpBoundary?.Dispose();
                tmpSegLines?.ForEach(l => l.Dispose());
                areas?.ForEach(a => a.Dispose());
                tmpSegLineIndexDic.Clear();
                tmpSegLineIndexDic = null;
            }

            return true;
        }

        public bool Set(List<Gene> genome)
        {
            Clear();//清空所有参数

            var tmpBoundary = OuterBoundary.Clone() as Polyline;

            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                var line = GetSegLine(gene);
                SegLines.Add(line);
                SegLineIndexDic.Add(i, line);
            }
            
            var areas = WindmillSplit.Split(tmpBoundary, SegLineIndexDic, BuildingBlockSpatialIndex, SeglineNeighborIndexDic);

            if(!IsReasonableAns(areas, tmpBoundary))
            {
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"Line count:{SegLines.Count}");
            System.Diagnostics.Debug.WriteLine($"Area count:{areas.Count}");

            try
            {
                SegLineSpatialIndex = new ThCADCoreNTSSpatialIndex(SegLines.ToCollection());
                Areas.AddRange(areas);
            }
            catch (Exception)
            {
            }
            finally
            {
                tmpBoundary.Dispose();
            }

            for (int i = 0; i < Areas.Count; i++)
            {
                AreaNumber.Add(i);
                Id2AllSubAreaDic.Add(i, Areas[i]);

                //todo: optimize
                var buildingBlocksInSubArea = GetBuildings(Areas[i]);

                SubAreaId2BuildingBlockDic.Add(i, buildingBlocksInSubArea);
                var segLines = GetSegLines(Areas[i], out List<int> lineNums);

                Id2AllSegLineDic.Add(i, segLines);
                AreaSegLineDic.Add(i, lineNums);
                SubAreaId2SegsDic.Add(i, GetAreaSegs(Areas[i], Id2AllSegLineDic[i], out List<Polyline> areaWall));
                SubAreaId2OuterWallsDic.Add(i, areaWall);

                var bdBoxes = new List<Polyline>();//临时建筑物外包线
                var allCuttersInSubArea = new List<List<Polyline>>();//临时建筑物框线
                foreach(var build in buildingBlocksInSubArea)
                {
                    var rect = build.GetRect();
                    var cuttersInBuilding = AllShearwallsSpatialIndex.SelectCrossingPolygon(rect).Cast<Polyline>().ToList();
                    bdBoxes.Add(rect);
                    allCuttersInSubArea.Add(cuttersInBuilding);
                }
                BuildingBoxes.Add(i, bdBoxes);
                SubAreaId2ShearWallsDic.Add(i, allCuttersInSubArea);

                if(bdBoxes.Count == 0)//没有建筑物
                {
                    if(areaWall.Count == 0)//没有墙线
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool DirectlyArrangementSetParameter(List<Gene> genome)
        {
            Clear();//清空所有参数
            var tmpBoundary = OuterBoundary.Clone() as Polyline;

            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                var line = GetSegLine(gene);
                SegLines.Add(line);
                SegLineIndexDic.Add(i, line);
            }

            //If in manual mode
            var areas = WindmillSplit.Split(tmpBoundary, SegLineIndexDic, BuildingBlockSpatialIndex, SeglineNeighborIndexDic);

            System.Diagnostics.Debug.WriteLine($"Line count:{SegLines.Count}");
            System.Diagnostics.Debug.WriteLine($"Area count:{areas.Count}");

            SegLineSpatialIndex = new ThCADCoreNTSSpatialIndex(SegLines.ToCollection());
            Areas.AddRange(areas);

            for (int i = 0; i < Areas.Count; i++)
            {
                AreaNumber.Add(i);
                Id2AllSubAreaDic.Add(i, Areas[i]);

                //todo: optimize
                var buildingBlocksInSubArea = GetBuildings(Areas[i]);

                SubAreaId2BuildingBlockDic.Add(i, buildingBlocksInSubArea);
                var segLines = GetSegLines(Areas[i], out List<int> lineNums);

                Id2AllSegLineDic.Add(i, segLines);
                AreaSegLineDic.Add(i, lineNums);
                SubAreaId2SegsDic.Add(i, GetAreaSegs(Areas[i], Id2AllSegLineDic[i], out List<Polyline> areaWall));
                SubAreaId2OuterWallsDic.Add(i, areaWall);

                var bdBoxes = new List<Polyline>();//临时建筑物外包线
                var allCuttersInSubArea = new List<List<Polyline>>();//临时建筑物框线
                foreach (var build in buildingBlocksInSubArea)
                {
                    var rect = build.GetRect();
                    var cuttersInBuilding = AllShearwallsSpatialIndex.SelectCrossingPolygon(rect).Cast<Polyline>().ToList();
                    bdBoxes.Add(rect);
                    allCuttersInSubArea.Add(cuttersInBuilding);
                }
                BuildingBoxes.Add(i, bdBoxes);
                SubAreaId2ShearWallsDic.Add(i, allCuttersInSubArea);
            }
            return true;
        }

        private bool IsReasonableAns(List<Polyline> areas, Polyline tmpBoundary, List<Line> tmpSeglines = null)
        {
            if (areas.Count != SegAreasCnt)//分割得到的区域数!=原始区域数
            {
                return false;
            }
            if(tmpSeglines == null)
            {
                if (IsInCorrectSegLine(tmpBoundary, SegLines))
                {
                    return false;
                }
            }
            else
            {
                if (IsInCorrectSegLine(tmpBoundary, tmpSeglines))
                {
                    return false;
                }
            }

            double areaTolerance = 1.0;//面积容差
            double areasTotalArea = 0;//分割后区域总面积
            areas.ForEach(a => areasTotalArea += a.Area);
            if (areasTotalArea - areaTolerance > OuterBoundary.Area)
            {
                return false;//分割后的总面积不能大于原始面积
            }
            return true;
        }

        private bool IsInCorrectSegLine(Polyline area, List<Line> seglines)
        {
            var halfLaneWidth = 2750;
            double carWidth = 4800;
            var lines = area.ToLines();
            var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            foreach(var l in seglines)
            {
                var pts = l.Intersect(area, 0);
                Line validL = new Line();
                Line extendL = new Line();
                if (pts.Count == 2)
                {
                    validL = new Line(pts[0], pts[1]);
                    extendL = validL.ExtendLineEx(-carWidth, 3);
                }
                else if(pts.Count == 1)
                {
                    var spt = l.StartPoint;
                    var ept = l.EndPoint;
                    if (area.Contains(spt))
                    {
                        validL = new Line(spt, pts[0]);
                        extendL = validL.ExtendLineEx(-carWidth, 2);
                    }
                    else
                    {
                        validL = new Line(ept, pts[0]);
                        extendL = validL.ExtendLineEx(-carWidth, 2);
                    }
                }
                else if(pts.Count == 0)
                {
                    validL = l;
                    extendL = validL;
                }
                else
                {
                    return true;
                }
                var rect = extendL.Buffer(halfLaneWidth);
                var rst = lineSpatialIndex.SelectCrossingPolygon(rect);
                if (rst.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private Line GetSegLine(Gene gene)
        {
            Point3d spt, ept;
            if(gene.VerticalDirection)
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
            var rstSplitByOriginalLine = AreaSplit.SplitAreasByOrgiginLine(line, ref areas);//
            if(!rstSplitByOriginalLine)
            {
                AreaSplit.SplitAreasByExtentedLine(line, ref areas);
            }
            line.Dispose();
        }

        private List<BlockReference> GetBuildings(Polyline area)
        {
            var dbObjs = BuildingBlockSpatialIndex.SelectCrossingPolygon(area);
            return dbObjs.Cast<BlockReference>().ToList();
        }
 
        private List<Line> GetSegLines(Polyline area, out List<int> lineNums)
        {
            var segLines = new List<Line>();
            lineNums = new List<int>();
            var dbObjs = SegLineSpatialIndex.SelectCrossingPolygon(area);
            var areaColl = new List<Polyline>() { area };
            areaColl.Clear();
            foreach (var db in dbObjs)
            {
                var line = db as Line;
                if(line.IsBoundOf(area))
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
            var lines = new HashSet<Line>();
            for(int i = 0; i < area.NumberOfVertices-1; i++)
            {
                var line = new Line(area.GetPoint3dAt(i), area.GetPoint3dAt(i+1));
                lines.Add(line);
            }
            var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            foreach (var seg in allSegs)
            {
                var rst = lineSpatialIndex.SelectFence(seg);
                if(rst.Count > 0)
                {
                    foreach (var r in rst)
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
                line.Dispose();

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

            //获取分割线切割后的墙线
            var plines = new List<List<Point2d>>();
            var visited = new HashSet<Point3dEx>();
            
            while (termPts.Count() > 0)
            {
                var pts = new List<Point2d>();
                var spt = termPts[0];
                termPts.RemoveAt(0);
                int depthFactor = 0;
                Plines.Dfs(spt, ref termPts, ref pts, ref visited, ptDic, ref depthFactor);
                plines.Add(pts);
            }
            areaWalls = new List<Polyline>();
            foreach (var pts in plines)
            {
                var pline = new Polyline();
                pline.CreatePolyline(pts.ToCollection());
                pts.Clear();
                areaWalls.Add(pline);
            }

            plines.Clear();
            visited.Clear();
            return segLines;
        }

        public void Dispose()
        {
            InitialWalls.Dispose();
            AreaNumber.Clear();
            SegLines.ForEach(e => e.Dispose());
            SegLines.Clear();
            Areas.ForEach(e => e.Dispose());
            Areas.Clear();
            Id2AllSubAreaDic.ForEach(e => e.Value.Dispose());
            Id2AllSubAreaDic.Clear();
            SubAreaId2OuterWallsDic.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            SubAreaId2OuterWallsDic.Clear();
            SubAreaId2SegsDic.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            SubAreaId2SegsDic.Clear();
            SubAreaId2BuildingBlockDic.Clear();
            SubAreaId2ShearWallsDic.ForEach(e => e.Value.ForEach(e1 => e1.ForEach(e2 => e2.Dispose())));
            SubAreaId2ShearWallsDic.Clear();
            BuildingBoxes.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            BuildingBoxes.Clear();
            Id2AllSegLineDic.ForEach(e => e.Value.ForEach(e1 => e1.Dispose()));
            Id2AllSegLineDic.Clear();
            SegLineIndexDic.ForEach(e => e.Value.Dispose());
            SegLineIndexDic.Clear();
            AreaSegLineDic.Clear();
            BuildingBlockSpatialIndex.Dispose();
            SegLineSpatialIndex?.Dispose();
            AllShearwallsSpatialIndex?.Dispose();
        }
    }

    public static class Plines
    {
        /// <summary>
        /// 基于深度优先搜索切割后的多段线
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="termPts"></param>
        /// <param name="pts"></param>
        /// <param name="visited"></param>
        /// <param name="ptDic"></param>
        public static void Dfs(Point3dEx cur, ref List<Point3dEx> termPts, ref List<Point2d> pts, ref HashSet<Point3dEx> visited,
            Dictionary<Point3dEx, List<Point3dEx>> ptDic, ref int depthFactor)
        {
            depthFactor++;
            if (depthFactor > 200)
            {
                termPts.Remove(cur);
                pts.Add(new Point2d(cur.X, cur.Y));
                visited.Clear();
                return;
            }
            if (termPts.Contains(cur))
            {
                termPts.Remove(cur);
                pts.Add(new Point2d(cur.X, cur.Y));
                visited.Clear();
                return;
            }
            pts.Add(new Point2d(cur.X, cur.Y));
            if (!visited.Contains(cur))
                visited.Add(cur);
            var neighbor = ptDic[cur];
            if (neighbor != null)
            {
                foreach (var pt in neighbor)
                {
                    if (visited.Contains(pt)) continue;
                    cur = pt;
                }
            }
            Dfs(cur, ref termPts, ref pts, ref visited, ptDic, ref depthFactor);
        }

        public static List<Polyline> GetCutters(this BlockReference br, bool usePline = true, Serilog.Core.Logger Logger = null)
        {
            double closedTor = 5.0;
            var plines = new List<Polyline>();
            var dbObjs = new DBObjectCollection();
            br.Explode(dbObjs);

            if(usePline)
            {
                foreach (var obj in dbObjs)
                {
                    if (obj is Polyline pline)
                    {
                        if(pline.GetPoints().Count() <= 2)
                        {
                            continue;
                        }

                        var closedPline = ThMEPFrameService.NormalizeEx(pline, closedTor);
                        if (closedPline.Closed)
                        {
                            plines.Add(closedPline);
                        }
                        else
                        {
                            Logger?.Information("存在不闭合的多段线！");
                        }
                    }
                }
            }
            else
            {
                foreach (var obj in dbObjs)
                {
                    //use hatch
                    if (obj is Hatch hatch)
                    {
                        var pl = (Polyline)hatch.Boundaries()[0];
                        var plrec = pl.GeometricExtents;
                        var rec = hatch.GeometricExtents;
                        if (plrec.GetCenter().DistanceTo(rec.GetCenter()) > 1)
                        {
                            pl.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, 0, 0), new Point3d(0, 1, 0))));
                            plrec = pl.GeometricExtents;
                            var vec = new Vector3d(rec.MinPoint.X - plrec.MinPoint.X, rec.MinPoint.Y - plrec.MinPoint.Y, 0);
                            pl.TransformBy(Matrix3d.Displacement(vec));
                        }
                        plines.Add(pl);
                    }
                }
            }
            
            return plines;
        }
    }
}
