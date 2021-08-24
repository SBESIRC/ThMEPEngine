using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPLighting.Garage.Service
{
    public class ThDistributeLightService
    {
        public List<Point3d> SplitPoints { get; private set; }
        private ThLineSplitParameter SplitParameter { get; set; }

        private ThDistributeLightService(ThLineSplitParameter splitParameter)
        {
            SplitParameter = splitParameter;
            SplitPoints = new List<Point3d>();
            if(SplitParameter.Margin < 0)
            {
                SplitParameter.Margin = 0;
            }
        }
        public static List<Point3d> Distribute(ThLineSplitParameter splitParameter)
        {
            var instance = new ThDistributeLightService(splitParameter);
            instance.Distribute();
            return instance.SplitPoints;
        }
        private void Distribute()
        {
            if(!IsValid())
            {
                return;
            }
            double totalTength = SplitParameter.Length; //总长
            double restLength = totalTength % SplitParameter.Interval; //剩余
            if (restLength / 2.0 < SplitParameter.Margin)
            {
                //修正
                restLength += SplitParameter.Interval;
            }
            var forwardLength = restLength / 2.0;
            while (forwardLength < totalTength)
            {
                SplitPoints.Add(GetDistributePoint(forwardLength).Value);
                forwardLength += SplitParameter.Interval;
            }
        }
        private Point3d? GetDistributePoint(double length)
        {
            Point3d? result = null;
            if (length<0)
            {
                return result;
            }
            if(length<1e-6)
            {
                return SplitParameter.Segment[0];
            }
            var sum = 0.0;
            for(int i=0;i< SplitParameter.Segment.Count-1;i++)
            {
                var vec = SplitParameter.Segment[i].GetVectorTo(SplitParameter.Segment[i + 1]);
                if (length<= (sum+ vec.Length))
                {
                    return SplitParameter.Segment[i] + vec.GetNormal().MultiplyBy(length - sum);
                }
                else
                {
                    sum += vec.Length;
                }
            }
            return result;
        }

        private bool IsValid()
        {
            if(this.SplitParameter==null)
            {
                return false;
            }            
            if (SplitParameter.Interval <= 0.0)
            {
                return false;
            }
            if (SplitParameter.Segment.Count<2)
            {
                //表示出入起终点是重复点
                return false;
            }
            if (SplitParameter.Segment[0].DistanceTo(SplitParameter.Segment[SplitParameter.Segment.Count-1]) < SplitParameter.Margin*2)
            {
                return false;
            }
            return true;
        }
    }
    public class ThLineSplitParameter
    {
        public List<Point3d> Segment { get; set; }
        public double Margin { get; set; }
        public double Interval { get; set; }
        public ThLineSplitParameter()
        {
            Segment = new List<Point3d>();
        }
        public double Length
        {
            get
            {
                double length = 0.0;
                for(int i=0;i< Segment.Count-1;i++)
                {
                    length += Segment[i].DistanceTo(Segment[i+1]);
                }
                return length;
            }
        }
    }
}
