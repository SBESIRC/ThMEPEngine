using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.ObliqueMPartitionLayout;
using ThParkingStall.Core.ObliqueMPartitionLayout.ObstacleIteration;
using ThParkingStall.Core.OTools;
using static ThParkingStall.Core.IO.ReadWriteEx;
namespace ThParkingStall.Core.OInterProcess
{
    public class OSubArea
    {
        public Polygon OutBound { get { return OInterParameter.TotalArea; } }//原始边界
        public readonly Polygon Region;//该区域的面域
        public readonly List<LineString> Walls;
        public readonly List<LineSegment> VaildLanes;//该区域全部车道线(目前是分区线）
        //public readonly List<LineString> SegLines;//该区域全部分区线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        public readonly List<ORamp> Ramps;//该区域全部的坡道
        public readonly List<Polygon> BuildingBounds;//该区域所有建筑物的BuildingBounds
        public int Count = -3;//车位总数
        public double Area;
        public OSubAreaKey Key;//该子区域的key
        public ObliqueMPartition obliqueMPartition;
        static object lockObj = new object();
        public OSubArea(Polygon area, List<LineSegment> vaildLanes, List<LineString> walls,List<Polygon> buildings,List<ORamp> ramps = null,List<Polygon> buildingBounds = null)
        {
            Region = area;
            VaildLanes = vaildLanes;
            Walls = walls;
            Buildings = buildings;
            Ramps = ramps;
            Area = area.Area * 0.001 * 0.001;
            bool IncludeWall = (OInterParameter.BorderLines != null && OInterParameter.BorderLines.Count == 0 ) || OInterParameter.Center!= null;
            Key = new OSubAreaKey(this, IncludeWall);
            BuildingBounds = buildingBounds;
        }

        public void UpdateParkingCnts(bool IgnoreCache = false)
        {
            if (OCached.Contains(this) && !IgnoreCache)
            {
                lock (lockObj)
                {
                    var layoutResult = OCached.GetLayoutResult(this);
                    Count = layoutResult.ParkingCnt;
                    Area = layoutResult.Area;
                    MCompute.CatchedTimes += 1;
                }
            }
            else
            {
                try
                {
                    //obliqueMPartition = new ObliqueMPartition();
                    //obliqueMPartition.Cars = new List<InfoCar>();
                    //obliqueMPartition.Pillars = new List<Polygon>();
                    //obliqueMPartition.IniLanes = new List<Lane>();
                    //obliqueMPartition.OutputLanes = new List<LineSegment>();

                    var boundstr = MDebugTools.AnalysisPolygon(Region);
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    FileStream fs = new FileStream(dir + "\\GAMonitor.txt", FileMode.Append);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(System.DateTime.Now);
                    sw.WriteLine("区域" + boundstr);
                    sw.Close();
                    fs.Close();

                    GA gA = new GA(Walls, VaildLanes, Buildings.Select(e => e.Clone()).ToList(), BuildingBounds.Select(e => e.Clone()).ToList(), Region);
                    gA.Process();
                    var newBuildings = gA.Buildings;
                    var newbuildingBoxes = gA.BuildingBoxes;

                    obliqueMPartition = new ObliqueMPartition(Walls, VaildLanes, newBuildings, Region);
                    obliqueMPartition.OutputLanes = new List<LineSegment>();
                    obliqueMPartition.OutBoundary = Region;
                    obliqueMPartition.BuildingBoxes = newbuildingBoxes;
                    obliqueMPartition.ObstaclesSpatialIndex = new MNTSSpatialIndex(newBuildings);
                    obliqueMPartition.QuickCalculate = !IgnoreCache && VMStock.SpeedUpMode;
                    ObliqueMPartition.LoopThroughEnd = VMStock.AllowLoopThroughEnd;
#if DEBUG
                    //var s = MDebugTools.AnalysisPolygon(obliqueMPartition.Boundary);
                    //string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    //FileStream fs = new FileStream(dir + "\\bound.txt", FileMode.Create, FileAccess.Write);
                    //StreamWriter sw = new StreamWriter(fs);
                    //sw.WriteLine(s);
                    //sw.Close();
                    //fs.Close();
#endif


                    obliqueMPartition.Process(true);
                    //有bug，暂时不接
                    Area = obliqueMPartition.CaledBound.Area;
                    var lane_segs = obliqueMPartition.IniLanes.Select(e => e.Line).ToList();
                    var car_plys = obliqueMPartition.Cars.Select(e => e.Polyline).ToList();
                    var column_plys = obliqueMPartition.Pillars;
                    //MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro.ConvertToMParkingPartitionPro());
                    //mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
                    Count = obliqueMPartition.Cars.Count;

                    var layoutResult = new LayoutResult(Count, Area);
                    lock (lockObj) OCached.Update(this, layoutResult);
                }
                catch (Exception ex)
                {
                    MCompute.Logger?.Information(ex.Message);
                    MCompute.Logger?.Information("----------------------------------");
                    MCompute.Logger?.Information(ex.StackTrace);
                    MCompute.Logger?.Information("##################################");
                    MPGAData.Save();
                }
            }
        }
    }

    public class OSubAreaKey : IEquatable<OSubAreaKey>
    {
        private HashSet<Coordinate> WallCoors;//边界顶点
        private HashSet<Coordinate> LaneCoors;//车道顶点
        public OSubAreaKey(OSubArea oSubArea,bool IncludeWall = true)
        {
            var wallCoors = new List<Coordinate>();
            if(IncludeWall) oSubArea.Walls.ForEach(w => wallCoors.AddRange(w.Coordinates));
            var laneCoors = new List<Coordinate>();
            oSubArea.VaildLanes.ForEach(l => { laneCoors.Add(l.P0);laneCoors.Add(l.P1);});
            WallCoors = wallCoors.GroupAndFilter();
            LaneCoors = laneCoors.GroupAndFilter();
        }
        public OSubAreaKey(HashSet<Coordinate> wallCoors, HashSet<Coordinate> laneCoors)
        {
            this.WallCoors = wallCoors;
            this.LaneCoors = laneCoors;
        }
        public override int GetHashCode()
        {
            int res1 = 0x56E5EEC8;
            foreach(var coor in WallCoors) res1 ^= coor.GetHashCode();
            int res2 = 0x72522171;
            foreach (var coor in LaneCoors) res2 ^= coor.GetHashCode();
            return res1 + res2;
        }
        public bool Equals(OSubAreaKey other)
        {
            if (this.WallCoors.Count != other.WallCoors.Count) return false;
            if (this.LaneCoors.Count != other.LaneCoors.Count) return false;
            if (this.WallCoors.Except(other.WallCoors).Count() != 0) return false;
            if (this.LaneCoors.Except(other.LaneCoors).Count() != 0) return false;
            return true;

        }
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(WallCoors.Count);
            foreach(var coor in WallCoors) coor.WriteToStream(writer);
            writer.Write(LaneCoors.Count);
            foreach (var coor in LaneCoors) coor.WriteToStream(writer);
        }
        public static OSubAreaKey ReadFromStream(BinaryReader reader)
        {
            var wallCoorCnt = reader.ReadInt32();
            var wallCoors = new HashSet<Coordinate>();
            for (int i = 0; i < wallCoorCnt; i++)
            {
                wallCoors.Add(ReadCoordinate(reader));
            }
            var laneCoorsCnt = reader.ReadInt32();
            var laneCoors = new HashSet<Coordinate>();
            for (int i = 0; i < laneCoorsCnt; i++)
            {
                laneCoors.Add(ReadCoordinate(reader));
            }
            return new OSubAreaKey(wallCoors, laneCoors);
        }    
    }

    //排布结果，包含车位数，面积等参数
    public class LayoutResult
    {
        public int ParkingCnt;
        public double Area;
        public LayoutResult(int parkingCnt,double area)
        {
            ParkingCnt = parkingCnt;
            Area = area;
        }
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(ParkingCnt);
            writer.Write(Area);
        }
        public static LayoutResult ReadFromStream(BinaryReader reader)
        {
            return new LayoutResult(reader.ReadInt32(), reader.ReadDouble());
        }
    }


    public static class OCached
    {
        static private Dictionary<OSubAreaKey, LayoutResult> _cachedPartitionCnt = new Dictionary<OSubAreaKey, LayoutResult>();
        static public Dictionary<OSubAreaKey, LayoutResult> CachedPartitionCnt
        {
            get { return _cachedPartitionCnt; }
            set { _cachedPartitionCnt = value; }
        }

        static private Dictionary<OSubAreaKey, LayoutResult> _newcachedPartitionCnt = new Dictionary<OSubAreaKey, LayoutResult>();
        static public Dictionary<OSubAreaKey, LayoutResult> NewCachedPartitionCnt//新出现的子区域
        {
            get { return _newcachedPartitionCnt; }
            set { _newcachedPartitionCnt = value; }
        }
        public static bool Contains(OSubArea subArea)
        {
            return CachedPartitionCnt.ContainsKey(subArea.Key);
        }
        public static LayoutResult GetLayoutResult(OSubArea subArea)
        {
            if (CachedPartitionCnt.ContainsKey(subArea.Key)) return CachedPartitionCnt[subArea.Key];
            else return null;
        }
        public static void Update(OSubArea subArea, LayoutResult layoutResult)
        {
            if (CachedPartitionCnt.ContainsKey(subArea.Key)) return;
            CachedPartitionCnt.Add(subArea.Key, layoutResult);
            NewCachedPartitionCnt.Add(subArea.Key, layoutResult);
        }
        public static void Update(Dictionary<OSubAreaKey, LayoutResult> subProcCachedPartitionCnt, bool updateNewCached = true)
        {
            foreach (var pair in subProcCachedPartitionCnt)
            {
                if (!CachedPartitionCnt.ContainsKey(pair.Key))
                {
                    CachedPartitionCnt.Add(pair.Key, pair.Value);
                    if (updateNewCached)//主进程更新
                        NewCachedPartitionCnt.Add(pair.Key, pair.Value);
                }
            }
        }
        public static void Update(GenomeColection genomeColection)//子进程更新
        {
            Update(genomeColection.NewCachedPartitionCnt, false);
        }
        public static void Clear()
        {
            _cachedPartitionCnt.Clear();
            _newcachedPartitionCnt.Clear();
        }
        public static void ClearNewAdded()
        {
            _newcachedPartitionCnt.Clear();
        }
    }
}
