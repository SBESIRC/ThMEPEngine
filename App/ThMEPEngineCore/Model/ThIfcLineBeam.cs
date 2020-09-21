using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Model
{
    public class ThIfcLineBeam :ThIfcBeam
    {
        public Vector3d Direction 
        { 
            get
            {
                return this.StartPoint.GetVectorTo(this.EndPoint);
            }
        }
        public double Length
        {
            get
            {
                return StartPoint.DistanceTo(EndPoint);
            }
        }
        public override Polyline Extend(double lengthIncrement,double widthIncrement)
        {
            Vector3d perpendDir = Direction.GetPerpendicularVector();
            Polyline outline = this.Outline as Polyline;
            double actualwidth = outline.GetPoint3dAt(0).DistanceTo(outline.GetPoint3dAt(1));
            Point3d newSp = this.StartPoint - Direction.GetNormal().MultiplyBy(lengthIncrement);
            Point3d newEp = this.EndPoint + Direction.GetNormal().MultiplyBy(lengthIncrement);
            Point3d pt1 = newSp - perpendDir.GetNormal().MultiplyBy(actualwidth / 2.0+ widthIncrement);
            Point3d pt2 = newSp + perpendDir.GetNormal().MultiplyBy(actualwidth / 2.0+ widthIncrement);
            Point3d pt3 = pt2 + Direction.GetNormal().MultiplyBy(newSp.DistanceTo(newEp));
            Point3d pt4 = pt1 + Direction.GetNormal().MultiplyBy(newSp.DistanceTo(newEp));
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }
    }
}
