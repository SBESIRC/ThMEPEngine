using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;

namespace ThParkingStall.Core.IO
{
    public static class ReadWriteEx
    {
        public static void WriteToStream(this LinearRing linearRing, BinaryWriter writer)
        {
            var coordinates = linearRing.Coordinates;
            writer.Write(coordinates.Count());
            foreach (var coordinate in coordinates)
            {
                writer.Write(coordinate.X);
                writer.Write(coordinate.Y);
            }
        }
        public static LinearRing ReadLinearRing(BinaryReader reader)
        {
            var CoorCnt = reader.ReadInt32();
            var coordinates = new Coordinate[CoorCnt];
            for (int i = 0; i < CoorCnt; i++)
            {
                {
                    var x = reader.ReadDouble();
                    var y = reader.ReadDouble();
                    coordinates[i] = new Coordinate(x, y);
                }
            }
            return new LinearRing(coordinates);
        }
        //public static void WriteToStream(this Dictionary<LinearRing, int> CachedCnts, BinaryWriter writer)
        //{
        //    var dicCnt = CachedCnts.Count;
        //    writer.Write(dicCnt);
        //    foreach (var kv in CachedCnts)
        //    {
        //        kv.Key.WriteToStream(writer);
        //        writer.Write(kv.Value);
        //    }
        //}
        //public static Dictionary<LinearRing, int> ReadCached(BinaryReader reader)
        //{
        //    var cached = new Dictionary<LinearRing, int>();
        //    var keyCnt = reader.ReadInt32();
        //    for (int i = 0; i < keyCnt; ++i)
        //    {
        //        var key = ReadLinearRing(reader);
        //        var value = reader.ReadInt32();
        //        cached.Add(key, value);
        //    }
        //    return cached;
        //}

        public static void WriteToStream(this Dictionary<SubAreaKey, int> CachedCnts, BinaryWriter writer)
        {
            var dicCnt = CachedCnts.Count;
            writer.Write(dicCnt);
            foreach (var kv in CachedCnts)
            {
                kv.Key.WriteToStream(writer);
                writer.Write(kv.Value);
            }
        }
        public static Dictionary<SubAreaKey, int> ReadCached(BinaryReader reader)
        {
            var cached = new Dictionary<SubAreaKey, int>();
            var keyCnt = reader.ReadInt32();
            for (int i = 0; i < keyCnt; ++i)
            {
                var key = SubAreaKey.ReadFromStream(reader);
                var value = reader.ReadInt32();
                cached.Add(key, value);
            }
            return cached;
        }
        public static void WriteToStream(this List<Chromosome> chromosomes, BinaryWriter writer)
        {
            var Cnts = chromosomes.Count;
            writer.Write(Cnts);
            chromosomes.ForEach(chromosome => chromosome.WriteToStream(writer));
        }
        public static List<Chromosome> ReadChromosomes(BinaryReader reader)
        {
            List<Chromosome> chromosomes = new List<Chromosome>();
            var Cnts = reader.ReadInt32();
            for (int i = 0; i < Cnts; ++i)
            {
                chromosomes.Add(Chromosome.ReadFromStream(reader));
            }
            return chromosomes;
        }

        public static void WriteToStream(this List<double> doubles, BinaryWriter writer)
        {
            writer.Write(doubles.Count);
            doubles.ForEach(i => writer.Write(i));
        }
        public static List<double> ReadDoubles(BinaryReader reader)
        {
            var Cnt = reader.ReadInt32();
            var Ints = new List<double>();
            for (int i = 0; i < Cnt; i++)
            {
                Ints.Add(reader.ReadDouble());

            }
            return Ints;
        }
        public static void WriteToStream(this List<int> intgers, BinaryWriter writer)
        {
            writer.Write(intgers.Count);
            intgers.ForEach(i => writer.Write(i));
        }
        public static List<int> ReadInts(BinaryReader reader)
        {
            var Cnt = reader.ReadInt32();
            var Ints = new List<int>();
            for (int i = 0; i < Cnt; i++)
            {
                Ints.Add(reader.ReadInt32());

            }
            return Ints;
        }

        public static void WriteToStream(this List<Int16> intgers, BinaryWriter writer)
        {
            writer.Write(intgers.Count);
            intgers.ForEach(i => writer.Write(i));
        }
        public static List<Int16> ReadInt16s(BinaryReader reader)
        {
            var Cnt = reader.ReadInt32();
            var Ints = new List<Int16>();
            for (int i = 0; i < Cnt; i++)
            {
                Ints.Add(reader.ReadInt16());

            }
            return Ints;
        }

    }
}
