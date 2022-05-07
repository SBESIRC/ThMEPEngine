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
        public readonly List<LineSegment> SegLines;//该区域全部分割线
        public readonly List<Polygon> Buildings; //该区域全部建筑物,包含坡道
        public readonly List<Ramp> Ramps;//该区域全部的坡道
        public readonly List<Polygon> BoundingBoxes;//该区域所有建筑物的bounding box
        public readonly SubAreaKey Key;
        public int Count = -3;//车位总数
        public MParkingPartitionPro mParkingPartitionPro;

        public SubArea(Polygon area, List<LineSegment> segLines,
            List<Polygon> buildings, List<Ramp> ramps, List<Polygon> boundingBoxes, SubAreaKey key)
        {
            Area = area;
            SegLines = segLines;
            Buildings = buildings;
            Ramps = ramps;
            BoundingBoxes = boundingBoxes;
            Key = key;
        }
        static object lockObj = new object(); 
        public void UpdateParkingCnts(bool Calculate)
        {
            lock (lockObj)
            {
                if (SubAreaParkingCnt.Contains(this) && !Calculate)
                {
                    Count = SubAreaParkingCnt.GetParkingNumber(this);
                    MCompute.CatchedTimes += 1;
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
                    SubAreaParkingCnt.UpdateParkingNumber(this, Count);
                }
        }
    }
    }

    public class SubAreaKey : IEquatable<SubAreaKey>
    {
        public List<Int16> GeneIdxs;
        public List<double> GeneVals;
        public bool ValIncreaseDir;
        private static readonly double Tol = 1e-10;
        public SubAreaKey(List<int> geneIdxs, List<double> geneVals,bool valIncreaseDir)
        {
            if (geneIdxs.Count != geneVals.Count) throw new ArgumentException("Index and Value Counts are different!");
            GeneIdxs = geneIdxs.Select(i => Convert.ToInt16(i)).ToList();
            GeneVals = geneVals;
            ValIncreaseDir = valIncreaseDir;
        }
        public SubAreaKey(List<Int16> geneIdxs, List<double> geneVals, bool valIncreaseDir)
        {
            if (geneIdxs.Count != geneVals.Count) throw new ArgumentException("Index and Value Counts are different!");
            GeneIdxs = geneIdxs;
            GeneVals = geneVals;
            ValIncreaseDir = valIncreaseDir;
        }
        public override int GetHashCode()
        {
            int res = 0x2D2816FE;
            res = res * 31 + ValIncreaseDir.GetHashCode();
            foreach (var item in GeneIdxs)
            {
                res = res * 31 +  item.GetHashCode();
            }
            //foreach (var item in GeneVals)
            //{
            //    res = res * 31 + item.GetHashCode();
            //}
            return res;
        }
        public bool Equals(SubAreaKey other)
        {
            
            //return this.PlanKey.SetEquals(other.PlanKey);
            if(ValIncreaseDir != other.ValIncreaseDir) return false;
            if(GeneIdxs.Count != other.GeneIdxs.Count) return false;
            for(int i = 0; i < GeneIdxs.Count; i++)
            {
                if(GeneIdxs[i] != other.GeneIdxs[i]) return false;
                //if(GeneVals[i] != other.GeneVals[i]) return false;
                if (Math.Abs(GeneVals[i]- other.GeneVals[i]) >= Tol) return false;
            }
            return true;
        }
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(ValIncreaseDir);
            GeneIdxs.WriteToStream(writer);
            GeneVals.WriteToStream(writer);
        }

        public static SubAreaKey ReadFromStream(BinaryReader reader)
        {
            var valIncreaseDir = reader.ReadBoolean();
            var geneIdxs = ReadWriteEx.ReadInt16s(reader);
            var geneVals = ReadWriteEx.ReadDoubles(reader);
            return new SubAreaKey (  geneIdxs,  geneVals ,  valIncreaseDir );
        }
    }
}
