using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Model
{
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
                for (int i = 0; i < Segment.Count - 1; i++)
                {
                    length += Segment[i].DistanceTo(Segment[i + 1]);
                }
                return length;
            }
        }
        public bool IsValid
        {
            get
            {
                return CheckValid();
            }
        }
        private bool CheckValid()
        {
            if (this.Interval <= 0.0)
            {
                return false;
            }
            if (this.Segment.Count < 2)
            {
                //表示出入起终点是重复点
                return false;
            }
            if (Length <= Margin * 2)
            {
                return false;
            }
            return true;
        }
    }
}
