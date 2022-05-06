using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;

namespace ThParkingStall.Core.InterProcess
{
    public class SubArea
    {
        public Polygon OutBound { get { return InterParameter.TotalArea; } }//原始边界
        public readonly Polygon Area;//该区域的面域
        public readonly List<LineSegment> SegLines;//该区域全部分割线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        public readonly List<Ramp> Ramps;//该区域全部的坡道
        public readonly List<Polygon> BoundingBoxes;//该区域所有建筑物的bounding box
        public int Count = -3;//车位总数
        public MParkingPartitionPro mParkingPartitionPro;
        public SubArea(Polygon area, List<LineSegment> segLines,
            List<Polygon> buildings, List<Ramp> ramps, List<Polygon> boundingBoxes)
        {
            Area = area;
            SegLines = segLines;
            Buildings = buildings;
            Ramps = ramps;
            BoundingBoxes = boundingBoxes;
        }
        static object lockObj = new object();
        public void UpdateParkingCnts(bool Calculate)
        {
            if (SubAreaParkingCnt.Contains(this) && !Calculate)
            {
                Count = SubAreaParkingCnt.GetParkingNumber(this);
            }
            else
            {
                mParkingPartitionPro = this.ConvertSubAreaToMParkingPartitionPro();
                try
                {
#if DEBUG
                    var s = MDebugTools.AnalysisPolygon(mParkingPartitionPro.Boundary);
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    FileStream fs = new FileStream(dir + "\\bound.txt", FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(s);
                    sw.Close();
                    fs.Close();
#endif
                    //mParkingPartitionPro.GenerateParkingSpaces();
                    mParkingPartitionPro.Process();
                }
                catch (Exception ex)
                {
                    MCompute.Logger?.Information(ex.Message);
                    MCompute.Logger?.Information("----------------------------------");
                    MCompute.Logger?.Information(ex.StackTrace);
                    MCompute.Logger?.Information("##################################");
                    MPGAData.Save();
                }
                Count = mParkingPartitionPro.CarSpots.Count;
                lock (lockObj)
                {
                    SubAreaParkingCnt.UpdateParkingNumber(this, Count);
                }
            }
        }

    }

}
