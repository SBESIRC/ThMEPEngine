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
using ThMEPArchitecture.PartitionLayout;
using ThMEPArchitecture.ViewModel;
namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class LayoutParameter:IDisposable
    {
        public Polyline InitialWalls { get; set; }//初始外包框
        public Polyline OuterBoundary { get; set; }//最外包围框，不被disposal
        public List<int> AreaNumber { get; set; }//区域索引，从0开始
        public List<string> SubAreaNumber { get; set; }//子区域索引，从a开始
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
        public Dictionary<int, bool> PtDirectionList { get; set; }//交点的方向
        public Dictionary<int, List<int>> PtDic { get; set; }//交点的线索引
        public Dictionary<string, Polyline> SubAreaDic { get; set; }//子区域，"1a"表示区域1的a子区域，且a表示包含建筑物
        public Dictionary<string, Polyline> SubAreaWallLineDic { get; set; }//子区域的墙线
        public Dictionary<string, Polyline> SubAreaSegLineDic { get; set; }//子区域的车道线
        public List<Point3d> IntersectPt { get; set; }//交点列表
        public Dictionary<LinePairs, int> LinePtDic { get; set; }
        public ThCADCoreNTSSpatialIndex BuildingBlockSpatialIndex { get; set; }//所有障碍物索引
        public ThCADCoreNTSSpatialIndex SegLineSpatialIndex { get; set; }//所有分割线索引
        public Dictionary<int, List<int>> SeglineNeighborIndexDic { get; set; }//分割线临近线

        public int SegAreasCnt { get; set; }//初始分割线
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
//#if DEBUG
//                        using (AcadDatabase currentDb = AcadDatabase.Active())
//                        {
//                            foreach (var pline in cuttersInBuilding)
//                            {
//                                currentDb.CurrentSpace.Add(pline);
//                            }
//                        }
//#endif
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
        public LayoutParameter(Polyline outerBoundary, DBObjectCollection buildingBlocks, List<Line> segLines, Dictionary<int, List<int>> ptDic, Dictionary<int, bool> directionList,
            Dictionary<LinePairs, int> linePtDic, Dictionary<int, List<int>> seglineNeighborIndexDic = null, 
            int segAreasCnt = 0, bool usePline = true, Serilog.Core.Logger logger = null)
        {
            InitialWalls = outerBoundary.Clone() as Polyline;
            OuterBoundary = outerBoundary;
            BuildingBlocks = buildingBlocks;
            SegLines = segLines;
            AreaNumber = new List<int>();
            SubAreaNumber = new List<string>() { "a", "b", "c", "d", "e" };
            Areas = new List<Polyline>();
            Areas.Add(InitialWalls);
            Id2AllSubAreaDic = new Dictionary<int, Polyline>();
            SubAreaId2OuterWallsDic = new Dictionary<int, List<Polyline>>();
            SubAreaId2SegsDic = new Dictionary<int, List<Line>>();
            SubAreaId2BuildingBlockDic = new Dictionary<int, List<BlockReference>>();
            SubAreaId2ShearWallsDic = new Dictionary<int, List<List<Polyline>>>();
            BuildingBoxes = new Dictionary<int, List<Polyline>>();
            Id2AllSegLineDic = new Dictionary<int, List<Line>>();
            SubAreaDic = new Dictionary<string, Polyline>();
            SubAreaWallLineDic = new Dictionary<string, Polyline>();
            SubAreaSegLineDic = new Dictionary<string, Polyline>();
            BuildingBlockSpatialIndex = new ThCADCoreNTSSpatialIndex(buildingBlocks);
            PtDic = ptDic;
            PtDirectionList = directionList;
            IntersectPt = new List<Point3d>();
            SegLineIndexDic = new Dictionary<int, Line>();
            AreaSegLineDic = new Dictionary<int, List<int>>();
            LinePtDic = linePtDic;
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

            SubAreaDic.ForEach(e => e.Value.Dispose());
            SubAreaDic.Clear();

            IntersectPt.Clear();

            SegLineIndexDic.ForEach(e => e.Value.Dispose());
            SegLineIndexDic.Clear();
        }

        public bool IsVaildGenome(List<Gene> Genome, ParkingStallArrangementViewModel parameterViewModel)
        {
            var tmpBoundary = OuterBoundary.Clone() as Polyline;
            var tmpSegLines = new List<Line>();
            var tmpSegLineIndexDic = new Dictionary<int, Line>();
            List<Polyline> areas = null;
            try
            {
                for (int i = 0; i < Genome.Count; i++)
                {
                    Gene gene = Genome[i];
                    var line = gene.ToLine();
                    tmpSegLines.Add(line);
                    tmpSegLineIndexDic.Add(i, line);
                }
                if (SeglineNeighborIndexDic is null)
                {
                    //If in automation mode
                    //
                    areas.Add(tmpBoundary);

                    //init splitting lines
                    for (int i = 0; i < Genome.Count; i++)
                    {
                        Gene gene = Genome[i];
                        Split(gene, ref areas);
                    }
                }
                else
                {
                    //If in manual mode
                    //
                    areas = WindmillSplit.Split(tmpBoundary, tmpSegLineIndexDic, BuildingBlockSpatialIndex, SeglineNeighborIndexDic);
                }

                if (areas.Count != SegAreasCnt)//分割得到的区域数!=原始区域数
                {
                    return false;//必定是个不合理的解
                }

                double areaTolerance = 1.0;//面积容差
                double areasTotalArea = 0;//分割后区域总面积
                areas.ForEach(a => areasTotalArea += a.Area);
                if (areasTotalArea - areaTolerance > OuterBoundary.Area)
                {
                    return false;//分割后的总面积不能大于原始面积
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

            var areas = new List<Polyline>();
            var tmpBoundary = OuterBoundary.Clone() as Polyline;

            for (int i = 0; i < genome.Count; i++)
            {
                Gene gene = genome[i];
                var line = GetSegLine(gene);
                SegLines.Add(line);
                SegLineIndexDic.Add(i, line);
            }
            
            if(SeglineNeighborIndexDic is null)
            {
                //If in automation mode
                //
                areas.Add(tmpBoundary);

                //init splitting lines
                for (int i = 0; i < genome.Count; i++)
                {
                    Gene gene = genome[i];
                    Split(gene, ref areas);
                }
            }
            else
            {
                //If in manual mode
                //
                areas = WindmillSplit.Split(tmpBoundary, SegLineIndexDic, BuildingBlockSpatialIndex, SeglineNeighborIndexDic);
            }

            if (areas.Count != SegAreasCnt)//分割得到的区域数!=原始区域数
            {
                return false;//必定是个不合理的解
            }

            double areaTolerance = 1.0;//面积容差
            double areasTotalArea = 0;//分割后区域总面积
            areas.ForEach(a => areasTotalArea += a.Area);
            if (areasTotalArea - areaTolerance > OuterBoundary.Area)
            {
                return false;//分割后的总面积不能大于原始面积
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

            // update other parameters
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

            // update other parameters
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

        private void GetPtNumAndDir(List<int> lineNums, out List<int> pointNums, out List<int> directions)
        {
            pointNums = new List<int>();
            directions = new List<int>();
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
        }

        private void SubAreaSeg(int i, List<Polyline> areas, List<int> pointNums, List<int> directions, List<Polyline> bdBoxes)
        {
            var subArea = PointAreaSeg.PtAreaSeg(areas[i], pointNums, directions, bdBoxes, IntersectPt);//子区域分割

            if (bdBoxes.Count == 0)//这个区域没有建筑物
            {
                SubAreaDic.Add(Convert.ToString(i) + "a", null);
                for (int k = 0; k < subArea.Count; k++)//子区域遍历
                {
                    var sub = subArea[k];
                    SubAreaDic.Add(Convert.ToString(i) + SubAreaNumber[k + 1], sub);
                }
            }
            else
            {
                var bdBoxesSpatialIndex = new ThCADCoreNTSSpatialIndex(bdBoxes.ToCollection());//创建建筑物的空间索引
                var index = 0;
                for (int k = 0; k < subArea.Count; k++)//子区域遍历
                {
                    var sub = subArea[k];
                    var rst = bdBoxesSpatialIndex.SelectCrossingPolygon(sub);
                    if (rst.Count > 0)//当前子区域包含建筑物
                    {
                        SubAreaDic.Add(Convert.ToString(i) + "a", sub);
                        index = k;
                        break;
                    }
                }
                var curIndex = 1;
                for (int k = 0; k < subArea.Count; k++)//子区域遍历
                {
                    if (k == index)
                    {
                        continue;
                    }
                    var sub = subArea[k];
                    SubAreaDic.Add(Convert.ToString(i) + SubAreaNumber[curIndex], sub);
                    curIndex++;
                }
                }

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
            if (AreaSplit.SplitAreasByOrgiginLine(line, ref areas))
            {
                ;
            }
            else
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
                if (rst.Count == 0)
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
                Dfs(spt, ref termPts, ref pts, ref visited, ptDic, ref depthFactor);
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
        /// <summary>
        /// 基于深度优先搜索切割后的多段线
        /// </summary>
        /// <param name="cur"></param>
        /// <param name="termPts"></param>
        /// <param name="pts"></param>
        /// <param name="visited"></param>
        /// <param name="ptDic"></param>
        private void Dfs(Point3dEx cur, ref List<Point3dEx> termPts, ref List<Point2d> pts, ref HashSet<Point3dEx> visited, 
            Dictionary<Point3dEx, List<Point3dEx>> ptDic, ref int depthFactor)
        {
            depthFactor++;
            if(depthFactor > 200)
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
            if(!visited.Contains(cur)) 
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
            Dfs(cur, ref termPts, ref pts, ref visited, ptDic, ref depthFactor);
        }

        public void Dispose()
        {
            InitialWalls.Dispose();
            AreaNumber.Clear();
            SubAreaNumber.Clear();
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
            BuildingBlockSpatialIndex.Dispose();
            SegLineSpatialIndex?.Dispose();
            AllShearwallsSpatialIndex?.Dispose();
            AllShearwallsMPolygonSpatialIndex?.Dispose();
        }
    }

    public static class Plines
    {
        public static List<Polyline> GetCutters(this BlockReference br, bool usePline = true, Serilog.Core.Logger Logger = null)
        {
            var plines = new List<Polyline>();
            var dbObjs = new DBObjectCollection();
            br.Explode(dbObjs);
            foreach (var obj in dbObjs)
            {
                if(usePline)
                {
                    if (obj is Polyline pline)
                    {
                        if (pline.Closed)
                        {
                            plines.Add(pline);
                        }
                        else
                        {
                            Logger?.Information("存在不闭合的多段线！");
                        }
                    }
                }
                else
                {
                    //use hatch
                    if(obj is Hatch hatch)
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
