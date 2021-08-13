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

        public static Line ExtendLine(this Line line, double distance)
        {
            var direction = line.LineDirection();
            return new Line(line.StartPoint - direction * distance, line.EndPoint + direction * distance);
        }
        public static Polyline Tesslate(this Line line,double length)
        {
            var pts = new Point3dCollection();
            var point = line.StartPoint;
            var dir = line.LineDirection();
            pts.Add(point);
            while(true)
            {
                point += dir.MultiplyBy(length);
                if(line.StartPoint.DistanceTo(point)>= line.Length)
                {
                    pts.Add(line.EndPoint);
                    break;
                }
                else
                {
                    pts.Add(point);
                }
            }
            var poly = new Polyline();
            for(int i =0;i<pts.Count;i++)
            {
                poly.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0.0, 0.0, 0.0);
            }
            return poly;
        }
    }
}
