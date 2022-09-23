using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.OTools;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Geometries.Utilities;

namespace ThParkingStall.Core.OInterProcess
{
    public static class OInterParameter
    {
        private static Polygon _TotalArea;//总区域，边界为外包框
        public static Polygon TotalArea { get { return _TotalArea; } }//总区域，边界为外包框

        private static List<LineSegment> _BorderLines;//可动边界线
        public static List<LineSegment> BorderLines { get { return _BorderLines; } }//可动边界线

        private static List<SegLine> _InitSegLines;//所有初始分区线
        public static List<SegLine> InitSegLines { get { return _InitSegLines; } }//所有初始分区线

        public static List<LineSegment> _SegLines;

        private static List<Polygon> _Buildings;// 所有障碍物，包含坡道
        public static List<Polygon> Buildings { get { return _Buildings; } }// 所有障碍物，包含坡道

        public static List<ORamp> _Ramps;// 坡道
        public static List<ORamp> Ramps { get{return _Ramps;} }// 坡道

        //public static Polygon _BaseLineBoundary;
        //public static Polygon BaseLineBoundary { get { return _BaseLineBoundary; } } //基线边界（包含内部孔），基线边界内的分割线的部分用来求基线

        private static MNTSSpatialIndex _BuildingSpatialIndex;//所有障碍物，包含坡道的spatialindex
        public static MNTSSpatialIndex BuildingSpatialIndex { get { return _BuildingSpatialIndex; } }//所有障碍物，包含坡道的spatialindex


        private static MNTSSpatialIndex _BoundarySpatialIndex;//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）
        public static MNTSSpatialIndex BoundarySpatialIndex { get { return _BoundarySpatialIndex; } }//边界打成断线 + 障碍物 + 坡道的spatialindex(所有边界）

        public static List<(List<int>, List<int>)> SeglineIndex;//分区线（起始终止点连接关系），数量为0则连到边界，其余为其他分区线的index

        private static List<Polygon> _BuildingBounds;
        public static List<Polygon> BuildingBounds { get { return _BuildingBounds; } }//建筑物外包框
        private static MNTSSpatialIndex _BuildingBoundSPIndex;
        public static MNTSSpatialIndex BuildingBoundSPIndex { get { return _BuildingBoundSPIndex; } }
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);

        private static double[] _MaxMoveDistances;
        public static double[] MaxMoveDistances { get { return _MaxMoveDistances; } }//扇形移动最大距离
        private static Coordinate _Center;
        public static Coordinate Center { get { return _Center; } }//点集移动中心

        private static List<Polygon> _MovingBounds;
        public static List<Polygon> MovingBounds { get { return _MovingBounds; } }//可动建筑框线

        public static List<OSubArea> dynamicSubAreas;//用于障碍物移位的子区域列表
        public static void Init(DataWraper dataWraper)
        {
            var oWarper = dataWraper.oParamWraper;
            _TotalArea = oWarper.TotalArea;
            _InitSegLines = oWarper.SegLines;
            _Buildings = oWarper.Buildings;
            _BuildingSpatialIndex = new MNTSSpatialIndex(Buildings);
            _Ramps = oWarper.Ramps;
            _MovingBounds = oWarper.MovingBounds;
            var buildingtol = 3000;
            //var bufferDistance = (VMStock.RoadWidth / 2) - OTools.SegLineEx.SegTol;
            _BuildingBounds = new MultiPolygon(new MultiPolygon(Buildings.ToArray()).Buffer(buildingtol, MitreParam).
                Union().Get<Polygon>(true).ToArray()).Buffer(-buildingtol, MitreParam).Get<Polygon>(true);//每一个polygong内部为一个建筑物
            _TotalArea = _TotalArea.Union(new MultiPolygon(BuildingBounds.ToArray())).Get<Polygon>(true).OrderBy(p=>p.Area).Last();

            //var bufferedWallLine = TotalArea.Buffer(-bufferDistance).Get<Polygon>(true).OrderBy(p => p.Area).Last();//边界内缩
            //_BaseLineBoundary = bufferedWallLine.Difference(new MultiPolygon(BuildingBounds.ToArray())).
            //    Get<Polygon>(false).OrderBy(p => p.Area).Last();//内缩后的边界 - 外扩后的建筑

            SeglineIndex = oWarper.seglineIndex;
            var allObjs = TotalArea.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            _BoundarySpatialIndex = new MNTSSpatialIndex(allObjs);
            _BorderLines = oWarper.borderLines;
            _MaxMoveDistances = oWarper.MaxMoveDistances;
            _Center = oWarper.Center;
            _BuildingBoundSPIndex = new MNTSSpatialIndex(BuildingBounds);
        }
        public static void Init(Polygon totalArea,List<SegLine> segLines,List<Polygon> buildings,List<ORamp> ramps,
             List<(List<int>, List<int>)> seglineIndex,List<LineSegment> borderLines = null)
        {
            _TotalArea = totalArea;
            _InitSegLines = segLines;
            _Buildings = buildings;
            _BuildingSpatialIndex = new MNTSSpatialIndex(buildings);
            _Ramps = ramps;

            var bufferDistance = (VMStock.RoadWidth / 2) - OTools.SegLineEx.SegTol;
            var BuildingBounds = new MultiPolygon(Buildings.ToArray()).Buffer(bufferDistance).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            var bufferedWallLine = totalArea.Buffer(-bufferDistance).Get<Polygon>(true).OrderBy(p => p.Area).Last();//边界内缩
            //_BaseLineBoundary = bufferedWallLine.Difference(new MultiPolygon(BuildingBounds.ToArray())).
            //    Get<Polygon>(false).OrderBy(p => p.Area).Last();//内缩后的边界 - 外扩后的建筑
            //_BaseLineBoundary = baseLineBoundary;
            SeglineIndex = seglineIndex;
            var allObjs = TotalArea.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            _BoundarySpatialIndex = new MNTSSpatialIndex(allObjs);
            _BorderLines = borderLines;
        }
        public static Geometry GetBoundLanes()
        {
            var subAreas = new List<OSubArea>();
            var newSegLines = new List<SegLine>();
            newSegLines = InitSegLines.Select(segLine => segLine.CreateNew()).ToList();
            var allObjs = TotalArea.Shell.ToLineStrings().Cast<Geometry>().ToList();
            allObjs.AddRange(Buildings);
            var boundarySpatialIndex = new MNTSSpatialIndex(allObjs);
            newSegLines.UpdateSegLines(TotalArea, boundarySpatialIndex);
            var SegLineStrings = newSegLines.Select(l => l.Splitter).ToList().ToLineStrings();
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            var vaildLanes = newSegLines.Select(l => l.VaildLane).ToList().ToLineStrings();
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLanes = vaildLanes.GetCommonParts(area);
                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var walls = SegLineStrings.GetWalls(area.Shell);
                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subRamps = Ramps.Where(r => area.Contains(r.InsertPt)).ToList();
                var subBuildingBounds = BuildingBoundSPIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subArea = new OSubArea(area, subLanes, walls, subBuildings, subRamps, subBuildingBounds);
                subAreas.Add(subArea);
            }
            dynamicSubAreas = subAreas;
            var laneMLstr = new MultiLineString(vaildLanes.ToArray());
            return new MultiLineString(areas.Select(p => p.Shell).ToArray()).Buffer(0.1, MitreParam).Intersection(laneMLstr);
        }
        public static List<OSubArea> GetMovingOsubAreas()
        {
            var subAreas = new List<OSubArea>();

            var SegLineStrings = InitSegLines.Select(l => l.Splitter).ToList().ToLineStrings();
            var vaildLanes = InitSegLines.Select(l => l.VaildLane).ToList().ToLineStrings();
            var areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            //var vaildSegSpatialIndex = new MNTSSpatialIndex(SegLineStrings.Cast<Geometry>().ToList());
            //var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));

            var maxBound = MovingBounds.Select(b =>b.Buffer(VMStock.BuildingMoveDistance,MitreParam) as Polygon).ToList();
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];

                var Mbounds = new MultiPolygon( maxBound.Where(b =>b.Intersects(area)).ToArray());
                var maxExtend = Mbounds.Union(area);

                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLanes = vaildLanes.GetCommonParts(area);
                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var walls = SegLineStrings.GetWalls(area.Shell);
                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(maxExtend).Cast<Polygon>().ToList();

                var subRamps = Ramps.Where(r => area.Contains(r.InsertPt)).ToList();
                var subBuildingBounds = BuildingBoundSPIndex.SelectCrossingGeometry(maxExtend).Cast<Polygon>().ToList();
                var subArea = new OSubArea(area, subLanes, walls, subBuildings, subRamps, subBuildingBounds);
                subAreas.Add(subArea);
            }
            return subAreas;
        }

        public static List<OSubArea> GetOSubAreas(Genome genome)
        {
            var subAreas = new List<OSubArea>();
            var newWallLine = ProcessToWallLine(genome);
            if (newWallLine != null) 
                genome.Area = newWallLine.Area * 0.001 * 0.001;
            else if(genome!=null) genome.Area = TotalArea.Area * 0.001 * 0.001;
            var newSegs = ProcessToSegLines(genome, newWallLine);
            var SegLineStrings = newSegs.Select(l =>l.Splitter).ToList().ToLineStrings();
            var vaildLanes = newSegs.Select(l => l.VaildLane).ToList().ToLineStrings();
            List<Polygon> areas;
            if(newWallLine != null) areas = newWallLine.Shell.GetPolygons(SegLineStrings);//区域分割
            else areas = TotalArea.Shell.GetPolygons(SegLineStrings);//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            //var vaildSegSpatialIndex = new MNTSSpatialIndex(SegLineStrings.Cast<Geometry>().ToList());
            //var segLineSpIndex = new MNTSSpatialIndex(SegLineStrings.Where(lstr => lstr != null));
            // 创建子区域列表
            for (int i = 0; i < areas.Count; i++)
            {
                var area = areas[i];
                if (area.Area < 0.5 * VMStock.RoadWidth * VMStock.RoadWidth) continue;
                var subLanes = vaildLanes.GetCommonParts(area);
                //var subSegLineStrings = segLineSpIndex.SelectCrossingGeometry(area).Cast<LineString>();
                var walls = SegLineStrings.GetWalls(area.Shell);
                var subBuildings = BuildingSpatialIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                
                var subRamps = Ramps.Where(r => area.Contains(r.InsertPt)).ToList();
                var subBuildingBounds = BuildingBoundSPIndex.SelectCrossingGeometry(area).Cast<Polygon>().ToList();
                var subArea = new OSubArea(area, subLanes, walls, subBuildings,subRamps, subBuildingBounds);
                subAreas.Add(subArea);
            }
            return subAreas;
        }
        //输出的分区线数量一致，需要求最大全连接组
        public static List<SegLine> ProcessToSegLines(Genome genome,Polygon newWallLine = null)
        {
            var newSegLines = new List<SegLine>();
            if (genome == null)
            {
                newSegLines = InitSegLines.Select(segLine => segLine.CreateNew()).ToList();
            }
            else
            {
                for (int i = 0; i < InitSegLines.Count; i++)
                {
                    newSegLines.Add(InitSegLines[i].GetMovedLine(genome.OGenes[0][i]));
                }
            }
            if(newWallLine == null) newSegLines.UpdateSegLines(SeglineIndex, TotalArea, BoundarySpatialIndex);//注意边界可变的情况存在bug
            else
            {
                var allObjs = newWallLine.Shell.ToLineStrings().Cast<Geometry>().ToList();
                allObjs.AddRange(Buildings);
                var boundarySpatialIndex = new MNTSSpatialIndex(allObjs);
                newSegLines.UpdateSegLines(SeglineIndex, newWallLine, boundarySpatialIndex);
            }
            //newSegLines = newSegLines.Where(l => l.VaildLane != null).ToList();
            //获取最大全连接组,存在其他组标记 + 报错
            var groups = newSegLines.GroupSegLines().OrderBy(g => g.Count).ToList();
            for (int i = 0; i < groups.Count - 1; i++)
            {
                newSegLines.Slice(groups[i]).ForEach(l => {l.VaildLane = null; });
            }
            groups = newSegLines.GroupSegLines(2).OrderBy(g => g.Count).ToList();
            for (int i = 0; i < groups.Count - 1; i++)
            {
                newSegLines.Slice(groups[i]).ForEach(l => { l.Splitter = null; });
            }

            //newSegLines = newSegLines.Slice(groups.Last());
            return newSegLines;
        }

        public static Polygon ProcessToWallLine(Genome genome)
        {
            if(genome == null) return null;
            if (BorderLines != null && genome.OGenes.ContainsKey(1))
            {
                var newBorderLines = new List<LineSegment>();
                for (int i = 0; i < BorderLines.Count; i++)
                {
                    var moveDist = genome.OGenes[1][i].dDNAs.First().Value;
                    newBorderLines.Add(BorderLines[i].Translate(BorderLines[i].NormalVector().Multiply(moveDist)));
                }
                var areas = newBorderLines.GetPolygons().OrderBy(p => p.Area);
                if (areas.Count() == 0) return null;
                var AllArea = areas.Last().Union(new MultiPolygon(BuildingBounds.ToArray())).Get<Polygon>(true);
                genome.Area = AllArea.Sum(p => p.Area);
                var area = AllArea.OrderBy(p=>p.Area).Last();
                genome.ExtraArea = genome.Area - area.Area;
                return area;
            }
            if(Center!= null && genome.OGenes.ContainsKey(2))
            {
                var coordinates = new List<Coordinate>();
                var Ogenes = genome.OGenes[2];
                var Count = MaxMoveDistances.Count();
                var angleStepSize = AngleUtility.PiTimes2 / Count;
                for (int i = 0; i < MaxMoveDistances.Count(); i++)
                {
                    var DistProp = Ogenes[i].dDNAs[0].Value;
                    var AngleProp = Ogenes[i].dDNAs[1].Value;
                    var StartAngle = angleStepSize * i;
                    var EndAngle = angleStepSize * (i + 1);
                    var angle = AngleProp * StartAngle + (1 - AngleProp) * EndAngle;
                    var Distance = MaxMoveDistances[i]*DistProp;
                    var Vector = new Vector2D(Math.Cos(angle), Math.Sin(angle));
                    var coordinate = Vector.Multiply(Distance).Translate(Center);
                    coordinates.Add(coordinate);
                }
                coordinates.Add(coordinates.First());
                var centerPt = Center.ToPoint();
                var AllArea = new Polygon(new LinearRing(coordinates.ToArray())).Intersection(TotalArea).
                    Union(new MultiPolygon(BuildingBounds.ToArray())).Get<Polygon>(true);
                genome.Area = AllArea.Sum(p => p.Area);
                var newTotalArea = AllArea.Where(p =>p.Contains(centerPt)).OrderBy(p=>p.Area).Last();
                genome.ExtraArea = genome.Area - newTotalArea.Area;
                return newTotalArea;
            }
            return null;
        }

        public static void UpdateBuildings(List<Vector2D> moveVectors)
        {
            if (moveVectors.Count != MovingBounds.Count) throw new ArgumentException("Vectors have Different Length with bounds");

            var Transformation = new AffineTransformation();
            for(int i = 0; i < MovingBounds.Count; i++)
            {
                var vector = moveVectors[i];
                var bound = MovingBounds[i];
                Transformation.SetToTranslation(vector.X, vector.Y);
                for (int j = 0; j < _Buildings.Count; j++)
                {
                    var building = _Buildings[j];
                    if (bound.Intersects(building))
                    {
                        _Buildings[j] = Transformation.Transform(building) as Polygon;
                    }
                }
                for (int k = 0; k < _Ramps.Count; k++)
                {
                    var ramp = _Ramps[k];
                    if (bound.Intersects(ramp.Area)) _Ramps[k] = ramp.Transform(vector);
                    
                }
                // 筛选可动bounding
                for(int m = 0;m < _BuildingBounds.Count; m++)
                {
                    var BB = _BuildingBounds[m];
                    if (bound.Intersects(BB)) _BuildingBounds[m] =Transformation.Transform( BB) as Polygon;
                    
                }
            }
            _BuildingSpatialIndex = new MNTSSpatialIndex(_Buildings);
            _BuildingBoundSPIndex = new MNTSSpatialIndex(_BuildingBounds);
        }
    }
}
