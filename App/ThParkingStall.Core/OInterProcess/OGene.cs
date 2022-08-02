using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.OInterProcess
{
    [Serializable]
    public class OGene
    {
        public int Type { get; set; }//基因类型，为分割线基因（0），或是障碍物基因（1），可扩展
        public List<DDNA> dDNAs { get; set; }
        private bool IsComplete = false;//dna是否完整
        public OGene(int type)//未设置DNA的基因
        {
            Type = type;
            dDNAs = new List<DDNA>();
            IsComplete = false;
        }
        public OGene(int type,double value)//仅包含一个基因的组
        {
            Type = type;
            DDNA dDNA;
            switch (Type)
            {
                case 0:
                    dDNA = new DDNA(0,value);
                    break;
                default:
                    throw new NotImplementedException("Do not have this type now");
            }
            dDNAs = new List<DDNA> { dDNA };
            IsComplete = true;
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
        public int Type { get; set; }//DNA类型，为分割线法向基因（0），或是障碍物DNA（1），或后续添加
        public double Value;//绝对基因则value代表绝对值，相对基因则要用上下边界确定绝对值
        public bool IsAbsolute;//是否为绝对值
        public DDNA(int type,double value)
        {
            Type = type;
            Value = value;
            switch (Type)
            {
                case 0:
                    IsAbsolute = false;
                    break;
                case 1:
                    IsAbsolute = true;
                    break;
            }
        }

        public DDNA GetDDNA(double new_value)
        {
            return new DDNA(Type, new_value);
        }
        public double GetAbsoluteValue()
        {
            if (IsAbsolute) return Value;
            else throw new ArgumentException("Only Absolute DNA can get absolute value without lower upper Bound ");
        }
        public double GetAbsoluteValue(double lb,double ub)
        {
            if (IsAbsolute) return Value;
            else
            {
                if(Value < 0) return lb;    
                var absoluteVal = lb + Value;
                if (absoluteVal < ub) return absoluteVal;
                else return ub;
            }
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
