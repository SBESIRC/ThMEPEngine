namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSPoint3d
    {
        public ThPDSPoint3d()
        {
            this.X = 0;
            this.Y = 0;
        }

        public ThPDSPoint3d(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public double X;
        public double Y;
    }
}
