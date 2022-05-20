using System;

namespace TianHua.Electrical.PDS.Model
{
    [Serializable]
    public class ThPDSPoint3d
    {
        public ThPDSPoint3d()
        {
            this.X = 0.01;
            this.Y = 0.01;
        }

        public ThPDSPoint3d(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public double X;
        public double Y;

        public bool EqualsTo(ThPDSPoint3d other)
        {
            return this.X == other.X && this.Y == other.Y;
        }

        public bool AlmostEqualsTo(ThPDSPoint3d other)
        {
            return Math.Abs(this.X - other.X) + Math.Abs(this.Y - other.Y) < 1.0;
        }
    }
}
