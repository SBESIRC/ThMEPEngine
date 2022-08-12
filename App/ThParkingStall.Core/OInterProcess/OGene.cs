using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OInterProcess
{
    //基因组，包含不同类型的OGene,OGene顺序很重要，不可打乱
    [Serializable]
    public class Genome
    {
        public Dictionary<int, List<OGene>> OGenes = new Dictionary<int, List<OGene>>();//key代表基因的type
        public int ParkingStallCount = -1;
        public double Area;
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

    }
    #region DNA
    [Serializable]
    public class DDNA//double DNA
    {
        public int Type { get; set; }//DNA类型，为分割线法向基因（0），或是边界DNA（1），或后续添加
        public double Value;//绝对基因则value代表绝对值。相对基因，若为正则相对于最小值增加，若为负则相对最大值减少
        public bool IsRelative;//是否为相对值
        //public bool IsLowerBound;//是否为最小值,只有当dna是相对dna，且value为0时生效。false则为上边界
        public DDNA(int type,double value)
        {
            Type = type;
            Value = value;
            //IsLowerBound = isLowerBound;
            switch (Type)
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
            return new DDNA(Type,Value);
        }
        //public DDNA GetDDNA(double new_value)
        //{
        //    return new DDNA(Type, new_value);
        //}
        //public double GetAbsoluteValue()
        //{
        //    if (IsRelative) return Value;
        //    else throw new ArgumentException("Only Absolute DNA can get absolute value without lower upper Bound ");
        //}
        //public double GetAbsoluteValue(double lb,double ub)
        //{
        //    if (IsRelative) return Value;
        //    else
        //    {
        //        if(Value < 0) return lb;    
        //        var absoluteVal = lb + Value;
        //        if (absoluteVal < ub) return absoluteVal;
        //        else return ub;
        //    }
        //}
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
