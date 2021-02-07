using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThLineExtension
    {
        public static Vector3d LineDirection(this Line line)
        {
            return line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }

        // https://adndevblog.typepad.com/autocad/2012/07/using-curveextend.html
        public static Line ExtendLine(this Line line, double distance)
        {
            var direction = line.LineDirection();
            return new Line(line.StartPoint - direction * distance, line.EndPoint + direction * distance);
        }

        // https://www.keanw.com/2010/07/shortening-a-set-of-autocad-lines-using-net.html
        public static Line ShortenLine(this Line line, double distance)
        {
            double ep = line.EndParam;
            double sp = line.StartParam;
            double delta = (ep - sp) * (distance / line.Length);
            DoubleCollection dc = new DoubleCollection()
            {
                sp + delta,
                ep - delta,
            };
            DBObjectCollection objs = line.GetSplitCurves(dc);
            return objs[1] as Line;
        }
    }
}
