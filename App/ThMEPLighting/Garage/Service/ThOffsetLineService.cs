using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThOffsetLineService
    {
        public Line First { get; set; }
        public Line Second { get; set; }
        private Line Center { get; set; }
        private double OffsetDistance { get; set; }
        private ThOffsetLineService(Line center,double offsetDistance)
        {
            Center = center;
            OffsetDistance = offsetDistance;            
        }
        public static ThOffsetLineService Offset(
            Line center,double offsetDistance)
        {
            var instance = new ThOffsetLineService(center, offsetDistance);
            instance.Offset();
            return instance;
        }
        private void Offset()
        {
            var vec = Center.StartPoint.GetVectorTo(Center.EndPoint)
                   .GetPerpendicularVector().GetNormal();
            var upSp = Center.StartPoint + vec.MultiplyBy(OffsetDistance);
            var upEp = Center.EndPoint + vec.MultiplyBy(OffsetDistance);

            var downSp = Center.StartPoint - vec.MultiplyBy(OffsetDistance);
            var downEp = Center.EndPoint - vec.MultiplyBy(OffsetDistance);

            First = new Line(upSp, upEp);
            Second = new Line(downSp, downEp);
        }
    }
}
