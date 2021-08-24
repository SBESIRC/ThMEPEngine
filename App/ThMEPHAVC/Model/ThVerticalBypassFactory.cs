using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;

namespace ThMEPHVAC.Model
{
    public class ThVerticalBypassFactory
    {
        public static DBObjectCollection Create_vt_elbow_geo(double height, double width)
        {
            double hw = width / 2;
            double hh = height / 2;

            var dL = new Point3d(-hw, -hh, 0);
            var uL = new Point3d(-hw, hh, 0);
            var dR = new Point3d(hw, -hh, 0);
            var uR = new Point3d(hw, hh, 0);
            var points = new Point3dCollection() { uR, dL, dR, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            var closeL1 = new Line(uL, dL);
            var closeL2 = new Line(uR, dR);
            var line_set = new DBObjectCollection();
            frame.Explode(line_set);
            line_set.Add(closeL1);
            line_set.Add(closeL2);
            return line_set;
        }
        public static DBObjectCollection Create_vt_elbow_flg(double height, double width)
        {
            var hw = width / 2;
            var hh = height / 2;
            var ext = 45;

            var dL = new Point3d(-hw - ext, -hh - ext, 0);
            var uL = new Point3d(-hw - ext, hh + ext, 0);
            var dR = new Point3d(hw + ext, -hh - ext, 0);
            var uR = new Point3d(hw + ext, hh + ext, 0);

            var points = new Point3dCollection() { uR, dR, dL, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            var intverL = new Line(uL + new Vector3d(-ext, 0, 0),
                                   dL + new Vector3d(-ext, 0, 0));
            var line_set = new DBObjectCollection();
            frame.Explode(line_set);
            line_set.Add(intverL);
            return line_set;
        }
        public static DBObjectCollection Create_vt_elbow_center(Matrix3d dis_mat)
        {
            var p = Point3d.Origin.TransformBy(dis_mat);
            return new DBObjectCollection() { new DBPoint(p) };
        }
    }
}
