using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.IO.ReadWriteEx;
namespace ThParkingStall.Core.OInterProcess
{
    #region Gene
    //基因组集合,主进程传到子进程的数据结构
    [Serializable]
    public class GenomeColection
    {
        public List<Genome> Genomes  = new List<Genome>();
        //cache
        public Dictionary<OSubAreaKey, LayoutResult> NewCachedPartitionCnt = new Dictionary<OSubAreaKey, LayoutResult>();//新出现的子区域
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(Genomes.Count);
            Genomes.ForEach(g =>g.WriteToStream(writer));
            NewCachedPartitionCnt.WriteToStream(writer);
        }
        public static GenomeColection ReadFromStream(BinaryReader reader)
        {
            var genomeCnt = reader.ReadInt32();
            var result = new GenomeColection();
            for(int i = 0; i < genomeCnt; i++)
            {
                result.Genomes.Add(Genome.ReadFromStream(reader));
            }
            result.NewCachedPartitionCnt = ReadOCached(reader);
            return result;
        }
    }

    //基因组，包含不同类型的OGene,OGene顺序很重要，不可打乱
    [Serializable]
    public class Genome
    {
        public Dictionary<int, List<OGene>> OGenes = new Dictionary<int, List<OGene>>();//key代表基因的type

        //以下三个参数随基因变化而变，不参与克隆与数据传输
        public int ParkingStallCount = -1;
        public double Area = OInterParameter.TotalArea.Area * 0.001 * 0.001;
        public double score;
        public void Add(OGene oGene)
        {
            var type = oGene.GeneType;
            if (OGenes.ContainsKey(type))
            {
                OGenes[type].Add(oGene);
            }
            else
            {
                OGenes.Add(type, new List<OGene> { oGene });
            }
        }
        public Genome Clone()
        {
            var clone = new Genome();
            foreach(var k in OGenes.Keys)
            {
                foreach(var ogene in OGenes[k]) clone.Add(ogene.Clone());
            }
            return clone;
        }
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(OGenes.Count);
            foreach(var kv in OGenes)
            {
                writer.Write(kv.Key);
                writer.Write(kv.Value.Count);
                kv.Value.ForEach(gene =>gene.WriteToStream(writer));
            }
        }
        public static Genome ReadFromStream(BinaryReader reader)
        {
            var dicCnt = reader.ReadInt32();
            var genome = new Genome();
            for (int i = 0; i < dicCnt; i++)
            {
                var key = reader.ReadInt32();
                var geneCnt = reader.ReadInt32();
                var geneLis = new List<OGene>();
                for(int j = 0; j < geneCnt; j++) geneLis.Add(OGene.ReadFromStream(reader));
                genome.OGenes.Add(key, geneLis);
            }
            return genome;
        }
    }
    [Serializable]
    public class OGene
    {
        public int GeneType { get; set; }//基因类型，为分割线基因（0），或是边界基因（1），可扩展
        public List<DDNA> dDNAs { get; set; }
        //private bool IsComplete = false;//dna是否完整
        public OGene(int type)//未设置DNA的基因
        {
            GeneType = type;
            dDNAs = new List<DDNA>();
            //IsComplete = false;
        }
        public OGene(int geneType,double value)//仅包含一个基因的组
        {
            GeneType = geneType;
            DDNA dDNA;
            switch (GeneType)
            {
                case 0:
                    dDNA = new DDNA(0,value);
                    break;
                case 1:
                    dDNA = new DDNA(1,value);
                    break;
                default:
                    throw new NotImplementedException("Do not have this type now");
            }
            dDNAs = new List<DDNA> { dDNA };
            //IsComplete = true;
        }
        public OGene Clone()
        {
            var clone = new OGene(GeneType);
            clone.dDNAs = dDNAs.Select(d =>d.Clone()).ToList();
            return clone;
        }
        //public OGene(int type, List<double> values)//一根线包含多个基因
        //{
        //    Type = type;
        //    switch (Type)
        //    {
        //        case 0:
        //            dDNAs = values.Select(v => new DDNA(0,v)).ToList();
        //            break;
        //        default:
        //            throw new NotImplementedException("Do not have this type now");
        //    }
        //    IsComplete = true;
        //}
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(GeneType);
            writer.Write(dDNAs.Count);
            dDNAs.ForEach(dDNA =>dDNA.WriteToStream(writer));
        }
        public static OGene ReadFromStream(BinaryReader reader)
        {
            var geneType = reader.ReadInt32();
            var Cnts = reader.ReadInt32();
            var result = new OGene(geneType);
            for(int i = 0; i < Cnts; i++) result.dDNAs.Add(DDNA.ReadFromStream(reader));
            return result;
        }
    }
    #endregion
    #region DNA
    [Serializable]
    public class DDNA//double DNA
    {
        public int DNA_Type { get; set; }//DNA类型，为分割线法向基因（0），或是边界DNA（1），或后续添加
        public double Value;//绝对基因则value代表绝对值。相对基因，若为正则相对于最小值增加，若为负则相对最大值减少
        public bool IsRelative;//是否为相对值
        //public bool IsLowerBound;//是否为最小值,只有当dna是相对dna，且value为0时生效。false则为上边界
        public DDNA(int type,double value)
        {
            DNA_Type = type;
            Value = value;
            //IsLowerBound = isLowerBound;
            switch (DNA_Type)
            {
                case 0:
                    IsRelative = true;
                    break;
                case 1:
                    IsRelative = true;
                    break;
            }
        }
        public DDNA Clone()
        {
            return new DDNA(DNA_Type,Value);
        }
        public void WriteToStream(BinaryWriter writer)
        {
            writer.Write(DNA_Type);
            writer.Write(Value);
        }
        public static DDNA ReadFromStream(BinaryReader reader)
        {
            return new DDNA(reader.ReadInt32(), reader.ReadDouble());
        }
    }
    [Serializable]
    public class IDNA//Int DNA
    {
        public int Type { get; set; }//DNA类型
        public int Value;
        public bool IsAbsolute;//是否为绝对值

    }
    [Serializable]
    public class BDNA//bool DNA
    {
        public int Type { get; set; }//DNA类型
        public bool Value;
        public bool IsAbsolute;//是否为绝对值

    }
    #endregion


}
