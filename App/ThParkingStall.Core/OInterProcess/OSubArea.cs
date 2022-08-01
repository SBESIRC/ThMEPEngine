using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.ObliqueMPartitionLayout;

namespace ThParkingStall.Core.OInterProcess
{
    public class OSubArea
    {
        public Polygon OutBound { get { return OInterParameter.TotalArea; } }//原始边界
        public readonly Polygon Area;//该区域的面域
        public readonly List<LineString> Walls;
        public readonly List<LineSegment> VaildLanes;//该区域全部车道线(目前是分区线）
        //public readonly List<LineString> SegLines;//该区域全部分区线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        public readonly List<ORamp> Ramps;//该区域全部的坡道
        //public readonly List<Polygon> BoundingBoxes;//该区域所有建筑物的bounding box
        public int Count = -3;//车位总数
        public ObliqueMPartition obliqueMPartition;
        public OSubArea(Polygon area, List<LineSegment> vaildLanes, List<LineString> walls,List<Polygon> buildings,List<ORamp> ramps = null)
        {
            Area = area;
            VaildLanes = vaildLanes;
            Walls = walls;
            Buildings = buildings;
            Ramps = ramps;
        }

        public void UpdateParkingCnts()
        {
            //暂未包含cache
            try
                {
                obliqueMPartition = new ObliqueMPartition(Walls, VaildLanes, Buildings, Area);
                obliqueMPartition.OutputLanes = new List<LineSegment>();
                obliqueMPartition.OutBoundary = Area;
                obliqueMPartition.BuildingBoxes = new List<Polygon>();
                obliqueMPartition.ObstaclesSpatialIndex = new MNTSSpatialIndex(Buildings);
#if DEBUG
                    var s = MDebugTools.AnalysisPolygon(obliqueMPartition.Boundary);
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    FileStream fs = new FileStream(dir + "\\bound.txt", FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(s);
                    sw.Close();
                    fs.Close();
#endif
                obliqueMPartition.Process(true);
                //MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro.ConvertToMParkingPartitionPro());
                //mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
                Count = obliqueMPartition.Cars.Count;
            }
                catch (Exception ex)
                {
                    MCompute.Logger?.Information(ex.Message);
                    MCompute.Logger?.Information("----------------------------------");
                    MCompute.Logger?.Information(ex.StackTrace);
                    MCompute.Logger?.Information("##################################");
                    //MPGAData.Save();
                }
        }
    }
}
