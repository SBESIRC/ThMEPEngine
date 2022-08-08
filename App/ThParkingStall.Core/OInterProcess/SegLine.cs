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
        //输入基因值0~1，输出移动后的分割线（基于最大最小值）
        //public SegLine GetMovedLine(double fraction)
        //{
        //    if (fraction < 0.0 || fraction > 1.0) throw new ArgumentOutOfRangeException("Fraction must between 0 and 1!");
        //    if (!IsInitLine) throw new ArgumentException("Only InitLine Can Create new SegLine!");
        //    var distance = MinVal* fraction + MaxVal* (1-fraction);
        //    var clone = Clone();
        //    clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(distance));
        //    return clone;
        //}
        //public SegLine GetMovedLine(double distance)//输入基因法向绝对数值,可为正或者负
        //{
        //    var orgVector = Splitter.OrgVector(out double orgDistance);
        //    var clone = CreateNew();
            
        //    clone.Splitter = Splitter.Translate(orgVector.Multiply(distance-orgDistance));
        //    return clone;
           
        //}

        public SegLine GetMovedLine(OGene oGene)
        {
            var clone = CreateNew();
            var dDNA = oGene.dDNAs.First();
            var relativeValue = dDNA.Value;
            double moveDistance;
            if(relativeValue == 0)//值为0，选最大或最小值
            {
                if (dDNA.IsLowerBound) moveDistance = MinValue;
                else moveDistance = MaxValue;
            }
            else
            {
                if(relativeValue > 0) moveDistance = MinValue + relativeValue;//正值，基于最小值增加
                else moveDistance = MaxValue + relativeValue;//负值，基于最大值减少
            }
            clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(moveDistance));
            return clone;
        }

        public SegLine GetMovedLine(bool ToMin = true)
        {
            var clone = CreateNew();
            double moveDistance;
            if (ToMin) moveDistance = MinValue;
            else moveDistance = MaxValue;
            clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(moveDistance));
            return clone;
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
