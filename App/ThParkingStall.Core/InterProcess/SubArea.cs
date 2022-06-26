using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.IO;
namespace ThParkingStall.Core.InterProcess
{
    public class SubArea
    {
        public Polygon OutBound { get { return InterParameter.TotalArea; } }//原始边界
        public readonly Polygon Area;//该区域的面域
        public readonly List<LineString> Walls;
        public readonly List<LineSegment> VaildLanes;//该区域全部车道线
        public readonly List<LineString> SegLines;//该区域全部分区线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        public readonly List<Ramp> Ramps;//该区域全部的坡道
        public readonly List<Polygon> BoundingBoxes;//该区域所有建筑物的bounding box
        public readonly SubAreaKey Key;
        public int Count = -3;//车位总数
        public MParkingPartitionPro mParkingPartitionPro;

        public SubArea(Polygon area, List<LineSegment> vaildLanes, List<LineString> walls,
            List<Polygon> buildings, List<Ramp> ramps, List<Polygon> boundingBoxes, SubAreaKey key)
        {
            Area = area;
            VaildLanes = vaildLanes;
            Walls = walls;
            Buildings = buildings;
            Ramps = ramps;
            BoundingBoxes = boundingBoxes;
            Key = key;
        }
        public SubArea(Polygon area, List<LineString> segLines, List<LineString> walls,
                        List<Polygon> buildings, List<Ramp> ramps, List<Polygon> boundingBoxes)
        {
            Area = area;
            SegLines = segLines;
            Walls = walls;
            Buildings = buildings;
            Ramps = ramps;
            BoundingBoxes = boundingBoxes;
        }
        static object lockObj = new object();
        public void UpdateParkingCnts(bool IgnoreCache)
        {
            if (SubAreaParkingCnt.Contains(this) && !IgnoreCache)
            {
                Count = SubAreaParkingCnt.GetParkingNumber(this);
                if (MCompute.LogInfo)
                {
                    lock (lockObj)
                    {
                        MCompute.CatchedTimes += 1;
                    }
                }
            }
            else
            {
                //mParkingPartitionPro = this.ConvertSubAreaToMParkingPartitionPro();
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

    public class SubAreaKey : IEquatable<SubAreaKey>
    {
        public List<double> X_Vals;
        public List<double> Y_Vals;
        //public bool ValIncreaseDir;
        public (double, double) Center;
        //private static readonly double Tol = 1e-10;
        public SubAreaKey(List<Point> Centers, Coordinate center)
        {
            var ordered = Centers.OrderBy(c=>c.X).ThenBy(c =>c.Y);
            X_Vals=ordered.Select(c => c.X).ToList();
            Y_Vals=ordered.Select(c => c.Y).ToList();
            //ValIncreaseDir = valIncreaseDir;
            Center = (center.X, center.Y);
        }
        public SubAreaKey(List<double> x_Vals, List<double> y_Vals, (double, double) center)
        {
            X_Vals = x_Vals;
            Y_Vals = y_Vals;
            //ValIncreaseDir = valIncreaseDir;
            Center = center;
        }
        public override int GetHashCode()
        {
            int res = 0x2D2816FE;
            //res = res * 31 + ValIncreaseDir.GetHashCode();
            res = res * 31 + Center.Item1.GetHashCode();
            res = res * 31 + Center.Item2.GetHashCode();
            foreach (var item in X_Vals)
            {
                res = res * 31 + item.GetHashCode();
            }
            foreach (var item in Y_Vals)
            {
                res = res * 31 + item.GetHashCode();
            }
            return res;
        }
        public bool Equals(SubAreaKey other)
        {

            //return this.PlanKey.SetEquals(other.PlanKey);
            //if(ValIncreaseDir != other.ValIncreaseDir) return false;
            //if(Math.Abs(Center.Item1 - other.Center.Item1) >= Tol || 
            //    Math.Abs(Center.Item2 - other.Center.Item2) >= Tol) return false;
            if (Center.Item1 != other.Center.Item1 || Center.Item2 != other.Center.Item2) return false;
            if (X_Vals.Count != other.X_Vals.Count) return false;
            for (int i = 0; i < X_Vals.Count; i++)
            {
                if (X_Vals[i] != other.X_Vals[i]) return false;
                if (Y_Vals[i] != other.Y_Vals[i]) return false;
                //if (Math.Abs(GeneVals[i] - other.GeneVals[i]) >= Tol) return false;
            }
            return true;
        }
        public void WriteToStream(BinaryWriter writer)
        {
            //writer.Write(ValIncreaseDir);
            writer.Write(Center.Item1);
            writer.Write(Center.Item2);
            X_Vals.WriteToStream(writer);
            Y_Vals.WriteToStream(writer);
        }

        public static SubAreaKey ReadFromStream(BinaryReader reader)
        {
            //var valIncreaseDir = reader.ReadBoolean();
            var center = (reader.ReadDouble(), reader.ReadDouble());
            var x_Vals = ReadWriteEx.ReadDoubles(reader);
            var y_Vals = ReadWriteEx.ReadDoubles(reader);
            return new SubAreaKey(x_Vals, y_Vals, center);
        }
    }
}
