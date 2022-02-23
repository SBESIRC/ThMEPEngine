using Autodesk.AutoCAD.DatabaseServices;
using System;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class SegLineEx : IEquatable<SegLineEx>
    {
        public Line Segline { get; set; }
        public double MaxVal { get; set; }
        public double MinVal { get; set; }
        public bool Direction { get; set; }

        public SegLineEx(Line segline, double maxVal, double minVal)
        {
            Segline = new Line(segline.StartPoint, segline.EndPoint);
            MaxVal = maxVal;
            MinVal = minVal;
            Direction = Segline.GetDirection() == 1;
        }

        public SegLineEx Clone()
        {
            return new SegLineEx(this.Segline, this.MaxVal, this.MinVal);
        }

        public override int GetHashCode()
        {
            return Segline.GetHashCode();
        }

        public bool Equals(SegLineEx other)
        {
            return Segline.EqualsTo(other.Segline);
        }
    }
}
