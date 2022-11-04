using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.OInterProcess;

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
                coordinate.WriteToStream(writer);
            }
        }
        public static LinearRing ReadLinearRing(BinaryReader reader)
        {
            var CoorCnt = reader.ReadInt32();
            var coordinates = new Coordinate[CoorCnt];
            for (int i = 0; i < CoorCnt; i++)
            {
                {
                    coordinates[i] = ReadCoordinate(reader);
                }
            }
            return new LinearRing(coordinates);
        }
        public static void WriteToStream(this Coordinate coordinate, BinaryWriter writer)
        {
            writer.Write(coordinate.X);
            writer.Write(coordinate.Y);
        }

        public static Coordinate ReadCoordinate(BinaryReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            return new Coordinate(x, y);
        }
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

        public static void WriteToStream(this Dictionary<OSubAreaKey, LayoutResult> CachedCnts, BinaryWriter writer)
        {
            var dicCnt = CachedCnts.Count;
            writer.Write(dicCnt);
            foreach (var kv in CachedCnts)
            {
                kv.Key.WriteToStream(writer);
                kv.Value.WriteToStream(writer); 
            }
        }
        public static Dictionary<OSubAreaKey, LayoutResult> ReadOCached(BinaryReader reader)
        {
            var cached = new Dictionary<OSubAreaKey, LayoutResult>();
            var keyCnt = reader.ReadInt32();
            for (int i = 0; i < keyCnt; ++i)
            {
                var key = OSubAreaKey.ReadFromStream(reader);
                var value = LayoutResult.ReadFromStream(reader);
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
        public static void WriteToStream(this Dictionary<int,List<int>> dic, BinaryWriter writer)
        {
            var dicCnt = dic.Count;
            writer.Write(dicCnt);
            foreach(var kv in dic)
            {
                writer.Write(kv.Key);
                kv.Value.WriteToStream(writer);
            }
        }
        public static Dictionary<int,List<int>> ReadDicListInt(BinaryReader reader)
        {
            var result = new Dictionary<int,List<int>>();
            var dicCnt = reader.ReadInt32();
            for (int i = 0; i < dicCnt; ++i)
            {
                var key = reader.ReadInt32();
                var value = ReadInts(reader);
                result.Add(key, value);
            }
            return result;
        }
        public static void WriteToStream(this List<BuildingPosGene> genomes, BinaryWriter writer)
        {
            var Cnts = genomes.Count;
            writer.Write(Cnts);
            genomes.ForEach(g => g.WriteToStream(writer));
        }
        public static List<BuildingPosGene> ReadBPGs(BinaryReader reader)
        {
            List<BuildingPosGene> BPGs = new List<BuildingPosGene>();
            var Cnts = reader.ReadInt32();
            for (int i = 0; i < Cnts; ++i)
            {
                BPGs.Add(BuildingPosGene.ReadFromStream(reader));
            }
            return BPGs;
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
        public static void WriteToStream (this List<LayoutResult> results,BinaryWriter writer)
        {
            writer.Write(results.Count);
            results.ForEach(r => r.WriteToStream(writer));
        }
        public static List<LayoutResult> ReadLayoutResults(BinaryReader reader)
        {
            var Cnt = reader.ReadInt32();
            var results = new List<LayoutResult>();
            for(int i = 0; i < Cnt; i++)
            {
                results.Add(LayoutResult.ReadFromStream(reader));
            }
            return results;
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
