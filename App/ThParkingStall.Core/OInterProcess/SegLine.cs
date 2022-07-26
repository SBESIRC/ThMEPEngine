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
        private double _MinVal;
        public double MinVal//迭代最小值
        {
            get { if (IsFixed) return 0.0;else return _MinVal; }
        }
        private double _MaxVal;
        public double MaxVal//迭代最大值
        {
            get { if (IsFixed) return 0.0; else return _MaxVal; }
        }

        public bool IsInitLine;//是否是初始分割线

        public SegLine(LineSegment inputLine, bool isFixed = false, int roadWidth = -1,double minVal = 0,double maxVal = 0)
        {
            Splitter = inputLine.Positivize();
            IsFixed = isFixed;
            RoadWidth = roadWidth;
            _MinVal = minVal;
            _MaxVal = maxVal;
            IsInitLine = true;
        }
        public SegLine Clone()
        {
            var clone = new SegLine(Splitter,IsFixed,RoadWidth, _MinVal, _MaxVal);
            clone.VaildLane = VaildLane;
            clone.IsInitLine = false;
            return clone;
        }
        //输入基因值0~1，输出移动后的分割线（基于最大最小值）
        public SegLine GetMovedLine(double fraction)
        {
            if (fraction < 0.0 || fraction > 1.0) throw new ArgumentOutOfRangeException("Fraction must between 0 and 1!");
            if (!IsInitLine) throw new ArgumentException("Only InitLine Can Create new SegLine!");
            var distance = MinVal* fraction + MaxVal* (1-fraction);
            var clone = Clone();
            clone.Splitter = Splitter.Translate(Splitter.NormalVector().Multiply(distance));
            return clone;
        }
    }
}
