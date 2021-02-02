using System.Linq;
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
            double length = SplitParameter.LineSp.DistanceTo(SplitParameter.LineEp)
                - SplitParameter.StartConstraintLength
                - SplitParameter.EndConstraintLength;
            double restLength = length % SplitParameter.Interval;
            double splitNum = (length - restLength) / SplitParameter.Interval;
            if (restLength/2.0< SplitParameter.Margin)
            {
                restLength += SplitParameter.Interval;
                splitNum -= 1;
            }
            Vector3d normal = SplitParameter.LineSp.GetVectorTo(SplitParameter.LineEp).GetNormal();
            Point3d startPt = SplitParameter.LineSp + normal.MultiplyBy(SplitParameter.StartConstraintLength);
            Point3d baseSplitPt = startPt + normal.MultiplyBy(restLength / 2.0);
            if(restLength>0.0)
            {
                SplitPoints.Add(baseSplitPt);
            }
            for (int i=1;i< splitNum;i++)
            {
                Point3d nextSplitPt = baseSplitPt + normal.MultiplyBy(i * SplitParameter.Interval);
                SplitPoints.Add(nextSplitPt);
            }
            if(restLength > 0.0)
            {
                var lastPt = SplitParameter.LineEp - normal.MultiplyBy(restLength / 2.0);
                if (!SplitPoints.Where(o => o.DistanceTo(lastPt) <= 1.0).Any())
                {
                    SplitPoints.Add(lastPt);
                }
            }
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
            if (SplitParameter.LineSp.DistanceTo(SplitParameter.LineEp) < 1.0)
            {
                //表示出入起终点是重复点
                return false;
            }
            if (SplitParameter.LineSp.DistanceTo(SplitParameter.LineEp)< SplitParameter.Margin*2)
            {
                return false;
            }
            return true;
        }
    }
    public class ThLineSplitParameter
    {
        public Point3d LineSp { get; set; }
        public Point3d LineEp { get; set; }
        public double Margin { get; set; }
        public double Interval { get; set; }
        public double StartConstraintLength { get; set; } 
        public double EndConstraintLength { get; set; }
    }
}
