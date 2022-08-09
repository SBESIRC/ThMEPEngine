using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.OTools;

namespace ThParkingStall.Core.OInterProcess
{
    [Serializable]
    public class SegLine
    {
        public LineSegment Splitter=null;// 参与分割的线
        public LineSegment VaildLane=null;//有效车道，具有车道性质的部分
        public int RoadWidth;//车道宽,-1代表取默认值
        public bool IsFixed =false;//是否固定
        public bool IsInitLine;//是否是初始分割线，仅初始线可计算最大最小值，仅初始线可平移
        private double _MinValue;
        public double MinValue//初始线沿法向可以向下移动的范围
        {
            get { if (IsFixed) return 0.0;else return _MinValue; }
        }
        private double _MaxValue;
        public double MaxValue//初始线沿法向可以向上移动的范围
        {
            get { if (IsFixed) return 0.0; else return _MaxValue; }
        }

        public SegLine(LineSegment inputLine, bool isFixed = false, int roadWidth = -1)
        {
            Splitter = inputLine.Positivize();
            IsFixed = isFixed;
            RoadWidth = roadWidth;
            IsInitLine = true;
        }
        public SegLine CreateNew()//非完全克隆，最大最小值未克隆
        {
            var clone = new SegLine(Splitter.Clone(),IsFixed,RoadWidth);
            clone.VaildLane = VaildLane?.Clone();
            clone.IsInitLine = false;//非初始线，只有new的是初始线
            return clone;
        }

        public void SetMinMaxValue(double minValue,double maxValue)
        {
            if (!IsInitLine) throw new NotSupportedException("Only Support InitLine!");
            _MinValue = minValue;
            _MaxValue = maxValue;
        }
        //基因转换到线
        //public SegLine _GetMovedLine(OGene oGene)
        //{
        //    var clone = CreateNew();
        //    var dDNA = oGene.dDNAs.First();
        //    var relativeValue = dDNA.Value;
        //    double moveDistance;
        //    if(relativeValue == 0)//值为0，选最大或最小值
        //    {
        //        if (dDNA.IsLowerBound) moveDistance = MinValue;
        //        else moveDistance = MaxValue;
        //    }
        //    else
        //    {
        //        if(relativeValue > 0) moveDistance = MinValue + relativeValue;//正值，基于最小值增加
        //        else moveDistance = MaxValue + relativeValue;//负值，基于最大值减少
        //    }
        //    clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(moveDistance));
        //    return clone;
        //}
        public SegLine GetMovedLine(OGene oGene = null)
        {
            var clone = CreateNew();
            double moveDistance;
            if (IsFixed || oGene == null)
            {
                moveDistance = 0;
            }
            else
            {
                var dDNA = oGene.dDNAs.First();
                var relativeValue = dDNA.Value;
                if (relativeValue > 0)//相对值为正
                {
                    moveDistance = Math.Min( MinValue + relativeValue,MaxValue);//正值，基于最小值增加
                }
                else//非正
                {
                    moveDistance =Math.Max( MaxValue + relativeValue,MinValue);//负值，基于最大值减少
                }
            }
            clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(moveDistance));
            return clone;
        }

        //线移动到最小或最大值
        public SegLine GetMovedLine(bool ToMin = true)
        {
            var clone = CreateNew();
            double moveDistance;
            if (ToMin) moveDistance = MinValue;
            else moveDistance = MaxValue;
            clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(moveDistance));
            return clone;
        }
        public OGene GetOGene(double relativeValue)
        {
            return new OGene(0, relativeValue);
        }


        //分割线转为基因，adjust--是否自动调整
        public OGene ToGene(bool Adjust = true)
        {
            double relativeValue;
            if(MinValue <= 0 && MaxValue >= 0)//原始分割线在范围内
            {
                if(Math.Abs(MaxValue) > Math.Abs(MinValue))//离下边界近
                {
                    relativeValue = Math.Abs(MinValue);//相对下边，取正
                }
                else
                {
                    relativeValue = -Math.Abs(MaxValue);//相对上边，取负
                }
            }
            else 
            {
                if (MinValue > 0)//小于下边界
                {
                    relativeValue = 0.1;//取最小值 + 0.1
                }
                else
                {
                    relativeValue = -0.1;//最大值 - 0.1
                }

            }
            return new OGene(0, relativeValue); 
        }
    }

    public static class Extension
    {
        public static LineSegment Clone(this LineSegment line)
        {
            return new LineSegment(line.P0.Copy(), line.P1.Copy());
        }
    }
}
