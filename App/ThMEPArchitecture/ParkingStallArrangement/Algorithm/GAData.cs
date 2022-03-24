using System;
using AcHelper;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using System.Diagnostics;
using ThCADCore.NTS;
using ThMEPArchitecture.PartitionLayout;
using Dreambuild.AutoCAD;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using ThMEPArchitecture.ViewModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class Gene : IEquatable<Gene>
    {
        public double Value { get; set; }//线的值
        public bool VerticalDirection { get; set; }//点的方向，true是y方向
        public double MinValue { get; set; }//线的最小值
        public double MaxValue { get; set; }//线的最大值
        public double StartValue { get; set; }//线的起始点另一维
        public double EndValue { get; set; }//线的终止点另一维
        public Gene(double value, bool direction, double minValue, double maxValue, double startValue, double endValue)
        {
            Value = value;
            VerticalDirection = direction;
            MinValue = minValue;//绝对的最小值
            MaxValue = maxValue;//绝对的最大值
            StartValue = startValue;
            EndValue = endValue;
        }
        public Gene((double , bool , double , double , double , double ) data)
        {
            Value = data.Item1;
            VerticalDirection = data.Item2;
            MinValue = data.Item3;//绝对的最小值
            MaxValue = data.Item4;//绝对的最大值
            StartValue = data.Item5;
            EndValue = data.Item6;
        }
        public Gene Clone()
        {
            var gene = new Gene(Value, VerticalDirection, MinValue, MaxValue, StartValue, EndValue);
            return gene;
        }
        public Gene()
        {
            Value = 0;
            VerticalDirection = false;
            MinValue = 0;
            MaxValue = 0;
            StartValue = 0;
            EndValue = 0;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ VerticalDirection.GetHashCode();
        }
        public bool Equals(Gene other)
        {
            return Value.Equals(other.Value) && VerticalDirection.Equals(other.VerticalDirection);
        }

        public Line ToLine()
        {
            Point3d spt, ept;
            if (VerticalDirection)
            {
                spt = new Point3d(Value, StartValue, 0);
                ept = new Point3d(Value, EndValue, 0);
            }
            else
            {
                spt = new Point3d(StartValue, Value, 0);
                ept = new Point3d(EndValue, Value, 0);
            }
            return new Line(spt, ept);
        }
    }

    public class Chromosome
    {
        //Group of genes
        public List<Gene> Genome = new List<Gene>();

        public Serilog.Core.Logger Logger = null;

        public int ParkingStallCount { get; set; }

        static private Dictionary<PartitionBoundary, int> _cachedPartitionCnt = new Dictionary<PartitionBoundary, int>();

        static public Dictionary<PartitionBoundary, int> CachedPartitionCnt
        {
            get { return _cachedPartitionCnt; }
            set { _cachedPartitionCnt = value; }
        }
        public Chromosome Clone()
        {
            var clone = new Chromosome();
            clone.Logger = Logger;
            clone.Genome = new List<Gene>();
            clone.ParkingStallCount = ParkingStallCount;

            foreach (var gene in Genome)
            {
                clone.Genome.Add(gene.Clone());
            }
            return clone;
        }
        public int GenomeCount()
        {
            return Genome.Count;
        }

        public int GetMaximumNumber_(LayoutParameter layoutPara, GaParameter gaPara)
        {
            var rst2 = layoutPara.Set(Genome);
            if (!rst2) return 0;

            Random rand = new Random();
            int rst = rand.Next(200);
            ParkingStallCount = rst;
            return rst;
        }

        //Fitness method
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara, ParkingStallArrangementViewModel parameterViewModel)
        {
            //储存
            GAData.Set(this);
            var rst = layoutPara.Set(Genome);
            if (!rst) return 0;

            if (!IsValidatedSolutions(layoutPara)) return -1;
            int result = GetParkingNums(layoutPara, parameterViewModel);
            //Thread.Sleep(3);
            //int result = General.Utils.RandInt(1000) + 20;
            ParkingStallCount = result;
            //System.Diagnostics.Debug.WriteLine(Count);

            return result;
        }

        public static bool IsValidatedSolutions(LayoutParameter layoutPara)
        {
            var lanes = new List<Line>();
            var boundary = layoutPara.OuterBoundary;
            for (int k = 0; k < layoutPara.AreaNumber.Count; k++)
            {
                layoutPara.SubAreaId2SegsDic.TryGetValue(k, out List<Line> iniLanes);
                lanes.AddRange(iniLanes);
            }
            var tmplanes = new List<Line>();
            //与边界邻近的无效车道线剔除
            for (int i = 0; i < lanes.Count; i++)
            {
                var buffer = lanes[i].Buffer(2750 - 1);
                var splits = GeoUtilities.SplitCurve(boundary, buffer);
                if (splits.Count() == 1) continue;
                splits = splits.Where(e => buffer.Contains(e.GetPointAtParam(e.EndParam / 2))).Where(e => e.GetLength() > 1).ToArray();
                if (splits.Count() == 0) continue;
                var split = splits.First();
                var ps = lanes[i].GetClosestPointTo(split.StartPoint, false);
                var pe = lanes[i].GetClosestPointTo(split.EndPoint, false);
                var splitline = new Line(ps, pe);
                var splitedlines = GeoUtilities.SplitLine(lanes[i], new List<Point3d>() { ps, pe });
                splitedlines = splitedlines.Where(e => e.GetCenter().DistanceTo(splitline.GetClosestPointTo(e.GetCenter(), false)) > 1).ToList();
                lanes.RemoveAt(i);
                tmplanes.AddRange(splitedlines);
                i--;
            }
            lanes.AddRange(tmplanes);
            GeoUtilities.RemoveDuplicatedLines(lanes);
            //连接碎车道线
            int count = 0;
            while (true)
            {
                count++;
                if (count > 10) break;
                if (lanes.Count < 2) break;
                for (int i = 0; i < lanes.Count - 1; i++)
                {
                    var joined = false;
                    for (int j = i + 1; j < lanes.Count; j++)
                    {
                        if (GeoUtilities.IsParallelLine(lanes[i], lanes[j]) && (lanes[i].StartPoint.DistanceTo(lanes[j].StartPoint) == 0
                            || lanes[i].StartPoint.DistanceTo(lanes[j].EndPoint) == 0
                            || lanes[i].EndPoint.DistanceTo(lanes[j].StartPoint) == 0
                            || lanes[i].EndPoint.DistanceTo(lanes[j].EndPoint) == 0))
                        {
                            var pl = GeoUtilities.JoinCurves(new List<Polyline>(), new List<Line>() { lanes[i], lanes[j] }).Cast<Polyline>().First();
                            var line = new Line(pl.StartPoint, pl.EndPoint);
                            if (Math.Abs(line.Length - lanes[i].Length - lanes[j].Length) < 1)
                            {
                                lanes.RemoveAt(j);
                                lanes.RemoveAt(i);
                                lanes.Add(line);
                                joined = true;
                                break;
                            }
                        }
                    }
                    if (joined) break;
                }
            }
            //判断是否有孤立的车道线
            if (lanes.Count == 1) return true;
            for (int i = 0; i < lanes.Count; i++)
            {
                bool connected = false;
                for (int j = 0; j < lanes.Count; j++)
                {
                    if (i != j)
                    {
                        if (GeoUtilities.IsConnectedLines(lanes[i], lanes[j]) || lanes[i].Intersect(lanes[j], Intersect.OnBothOperands).Count > 0)
                        {
                            connected = true;
                            break;
                        }
                    }
                }
                if (!connected) return false;
            }
            return true;
        }

        public bool IsVaild(LayoutParameter layoutPara, ParkingStallArrangementViewModel ParameterViewModel)
        {
            return layoutPara.IsVaildGenome(Genome, ParameterViewModel);
        }
        public void Clear()
        {
            Genome.Clear();
        }

        public int GetMaximumNumberFast(LayoutParameter layoutPara, GaParameter gaPara)
        {
            var rst = layoutPara.Set(Genome);
            if (!rst) return 0;
            int result = GetParkingNumsFast(layoutPara);
            ParkingStallCount = result;
            return result;
        }

        private int GetParkingNumsFast(LayoutParameter layoutPara)
        {
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                int index = layoutPara.AreaNumber[j];
                layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
                layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
                layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> obstaclesList);
                layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> buildingBoxes);
                layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> walls);
                layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);
                var obstacles = new List<Polyline>();
                obstaclesList.ForEach(e => obstacles.AddRange(e));
                var bound = GeoUtilities.JoinCurves(walls, inilanes)[0];
                PartitionFast partition = new PartitionFast(walls, inilanes, obstacles, bound, buildingBoxes);
                try
                {
                    count += partition.CalCarSpotsFastly();
                }
                catch (Exception ex)
                {
                    Active.Editor.WriteMessage(ex.Message);
                }
            }
            return count;
        }


        private int GetParkingNums(LayoutParameter layoutPara, ParkingStallArrangementViewModel ParameterViewModel)
        {
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                var partitionpro = new ParkingPartitionPro();
                try
                {
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    if (!partitionpro.Validate()) continue;
                    var partitionBoundary = new PartitionBoundary(partitionpro.Boundary.Vertices());
                    if (CachedPartitionCnt.ContainsKey(partitionBoundary))
                    {
                        count += CachedPartitionCnt[partitionBoundary];
                    }
                    else
                    {
                        var subCnt = partitionpro.CalNumOfParkingSpaces();
                        CachedPartitionCnt.Add(partitionBoundary, subCnt);
                        System.Diagnostics.Debug.WriteLine($"Sub area count: {CachedPartitionCnt.Count}");
                        count += subCnt;
                    }
                }
                catch (Exception ex)
                {
                    Active.Editor.WriteMessage(ex.Message);
                }
            }
            return count;
        }

        public void AddChromos(Gene c)
        {
            Genome.Add(c);
        }
    }
    public static class GAData
    {
        private static Chromosome _Chromosome;
        public static void Set(Chromosome chromosome)
        {
            _Chromosome = chromosome;
        }
        private static List<(double,bool,double, double, double, double)> GetChromosomeData()
        {
            var res = new List<(double, bool, double, double, double, double)>();
            _Chromosome.Genome.ForEach(gene => res.Add((gene.Value, gene.VerticalDirection, gene.MinValue, gene.MinValue, gene.StartValue, gene.EndValue)));
            return res;
        }
        public static Chromosome LoadChromosome(string fileName = "GAData.dat")
        {
            var path = Path.Combine(System.IO.Path.GetTempPath(), fileName);
            var list = Load<(double, bool, double, double, double, double)>(path);
            var chromosome = new Chromosome();
            foreach(var geneData in list)
            {
                var gene = new Gene(geneData);
                chromosome.Genome.Add(gene);
            }
            return chromosome;
        }
        public static void Save(string fileName = "GAData.dat")
        {
            var path = Path.Combine(System.IO.Path.GetTempPath(), fileName);
            //Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            //string drawingName = Path.GetFileName(doc.Name);
            var list = GetChromosomeData();
            // Gain code access to the file that we are going
            // to write to
            try
            {
                // Create a FileStream that will write data to file.
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, list);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static List<T> Load<T>(string path)
        {
            var list = new List<T>();
            // Check if we had previously Save information of our friends
            // previously
            if (File.Exists(path))
            {
                try
                {
                    // Create a FileStream will gain read access to the
                    // data file.
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        var formatter = new BinaryFormatter();
                        list = (List<T>)
                            formatter.Deserialize(stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            return list;
        }
    }
}
