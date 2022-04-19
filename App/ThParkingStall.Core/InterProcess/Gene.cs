﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using NetTopologySuite.Geometries;
using System.Globalization;
using ThParkingStall.Core.Tools;
namespace ThParkingStall.Core.InterProcess
{
    [Serializable]
    public class Gene : IEquatable<Gene>
    {
        public double Value { get; set; }//线的值
        public bool Vertical { get; set; }//点的方向，true是y方向
        public double MinValue { get; set; }//线的最小值
        public double MaxValue { get; set; }//线的最大值
        public double StartValue { get; set; }//线的起始点另一维
        public double EndValue { get; set; }//线的终止点另一维
        public Gene(double value, bool direction, double minValue, double maxValue, double startValue, double endValue)
        {
            Value = value;
            Vertical = direction;
            MinValue = minValue;//相对的最小值
            MaxValue = maxValue;//相对的最大值
            StartValue = startValue;
            EndValue = endValue;
        }
        public Gene(double value, bool direction, double startValue, double endValue)
        {
            Value = value;
            Vertical = direction;
            StartValue = startValue;
            EndValue = endValue;
        }
        public Gene(LineSegment lineSegment)
        {
            Vertical = lineSegment.IsVertical();
            if (Vertical)
            {
                Value = lineSegment.P0.X;
                StartValue = Math.Min( lineSegment.P0.Y, lineSegment.P1.Y);
                EndValue = Math.Max(lineSegment.P0.Y, lineSegment.P1.Y);
            }
            else
            {
                Value = lineSegment.P0.Y;
                StartValue = Math.Min(lineSegment.P0.X, lineSegment.P1.X);
                EndValue = Math.Max(lineSegment.P0.X, lineSegment.P1.X);
            }
        }
        public Gene(LineSegment lineSegment,double value)
        {
            Vertical = lineSegment.IsVertical();
            if (Vertical)
            {
                Value = value;
                StartValue = Math.Min(lineSegment.P0.Y, lineSegment.P1.Y);
                EndValue = Math.Max(lineSegment.P0.Y, lineSegment.P1.Y);
            }
            else
            {
                Value = value;
                StartValue = Math.Min(lineSegment.P0.X, lineSegment.P1.X);
                EndValue = Math.Max(lineSegment.P0.X, lineSegment.P1.X);
            }
        }
        public Gene Clone()
        {
            var gene = new Gene(Value, Vertical, MinValue, MaxValue, StartValue, EndValue);
            return gene;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Vertical.GetHashCode();
        }
        public bool Equals(Gene other)
        {
            return Value.Equals(other.Value) && Vertical.Equals(other.Vertical)
                && StartValue.Equals(other.StartValue)&& EndValue.Equals(other.EndValue);
        }

        public LineSegment ToLineSegment()
        {
            Coordinate spt, ept;
            if (Vertical)
            {
                spt = new Coordinate(Value, StartValue);
                ept = new Coordinate(Value, EndValue);
            }
            else
            {
                spt = new Coordinate(StartValue, Value);
                ept = new Coordinate(EndValue, Value);
            }
            return new LineSegment(spt, ept);
        }
        public override string ToString()
        {
            return string.Format(NumberFormatInfo.InvariantInfo, "({0:R},{1:R},{2:R},{3:R},{4:R},{5:R})", 
                Value, Vertical, MinValue, MaxValue, StartValue, EndValue);
        }
    }
    [Serializable]
    public class Chromosome
    {
        //Group of genes
        public List<Gene> Genome = new List<Gene>();

        public int ParkingStallCount { get; set; }
        public Chromosome Clone()
        {
            var clone = new Chromosome();
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

        //Fitness method

        public void Clear()
        {
            Genome.Clear();
        }

        public void Append(Gene c)
        {
            Genome.Add(c);
        }
        public override string ToString()
        {
            string str = "";
            foreach(var gene in Genome)
            {
                str += gene.ToString();
                str += ";";
            }
            return str;
        }
    }
    [Serializable]
    public class ChromosomeCollection
    {
        public List<Chromosome> Chromosomes = new List<Chromosome>();//染色体列表

        private Dictionary<LinearRing, int> _newcachedPartitionCnt = new Dictionary<LinearRing, int>();
        public Dictionary<LinearRing, int> NewCachedPartitionCnt//上一代新出现的
        {
            get { return _newcachedPartitionCnt; }
            set { _newcachedPartitionCnt = value; }
        }

        public void Append(Chromosome chromosome)
        {
            Chromosomes.Add(chromosome);
        }
    }
    [Serializable]
    public class NewCachedPartitionCnt
    {
        private Dictionary<LinearRing, int> _newcachedPartitionCnt = new Dictionary<LinearRing, int>();
        public Dictionary<LinearRing, int> Data
        {
            get { return _newcachedPartitionCnt; }
            set { _newcachedPartitionCnt = value; }
        }
    }
    public static class SubAreaParkingCnt
    {
        static private Dictionary<LinearRing, int> _cachedPartitionCnt = new Dictionary<LinearRing, int>();
        static public Dictionary<LinearRing, int> CachedPartitionCnt
        {
            get { return _cachedPartitionCnt; }
            set { _cachedPartitionCnt = value; }
        }

        static private Dictionary<LinearRing, int> _newcachedPartitionCnt = new Dictionary<LinearRing, int>();
        static public Dictionary<LinearRing, int> NewCachedPartitionCnt//新出现的子区域
        {
            get { return _newcachedPartitionCnt; }
            set { _newcachedPartitionCnt = value; }
        }
        public static bool Contains(SubArea subArea)
        {
            return CachedPartitionCnt.ContainsKey(subArea.Area.Shell);
        }
        public static int GetParkingNumber(SubArea subArea)
        {
            if (CachedPartitionCnt.ContainsKey(subArea.Area.Shell)) return CachedPartitionCnt[subArea.Area.Shell];
            else return -999999;
        }
        public static void UpdateParkingNumber(SubArea subArea,int cnt)
        {
            if (CachedPartitionCnt.ContainsKey(subArea.Area.Shell)) return;
            CachedPartitionCnt.Add(subArea.Area.Shell, cnt);
            NewCachedPartitionCnt.Add(subArea.Area.Shell, cnt);
        }

        public static void Update(Dictionary<LinearRing, int> subProcCachedPartitionCnt,bool updateNewCached = true)
        {
            foreach(var pair in subProcCachedPartitionCnt)
            {
                if(!CachedPartitionCnt.ContainsKey(pair.Key))
                {
                    CachedPartitionCnt.Add(pair.Key, pair.Value);
                    if(updateNewCached)
                        NewCachedPartitionCnt.Add(pair.Key, pair.Value);
                }
            }
        }

        public static (List<List<(double, double)>>, List<int>) GetNewUpdated()
        {
            List<int> Cnts = new List<int> ();
            List<List<(double,double)>> coordinates = new List<List<(double, double)>>();

            foreach(var pair in NewCachedPartitionCnt)
            {
                var coorvalues = new List<(double,double)>();
                foreach(var coor in pair.Key.Coordinates)
                {
                    coorvalues.Add((coor.X, coor.Y));
                }
                coordinates.Add(coorvalues);
                Cnts.Add(pair.Value);
            }
            return (coordinates, Cnts);
        }

        public static void Update(List<List<(double, double)>> coordinates, List<int> Cnts)
        {
            var dict = new Dictionary<LinearRing, int>();
            for(int i = 0; i < Cnts.Count; i++)
            {
                var key =new LinearRing( coordinates[i].Select(val => new Coordinate(val.Item1,val.Item2)).ToArray());
                dict.Add(key,Cnts[i]);
            }
            Update(dict);
        }
        public static void Update(ChromosomeCollection chromosomeCollection)
        {
            Update(chromosomeCollection.NewCachedPartitionCnt,false);
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