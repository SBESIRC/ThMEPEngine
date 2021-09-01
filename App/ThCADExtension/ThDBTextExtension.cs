using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThDBTextExtension
    {
        // https://forums.autodesk.com/t5/net/c-can-i-get-the-exact-extents-dbtext-that-has-oblique-and/td-p/6640817
        public static Polyline OBBFrame(this DBText dBText)
        {
            using (var text = dBText.Clone() as DBText)
            {
                //
                // compute the transformation matrix of the "UCS Object" of the text
                var plane = new Plane(Point3d.Origin, text.Normal);
                var xform = Matrix3d.Rotation(text.Rotation, text.Normal, text.Position) *
                    Matrix3d.Displacement(text.Position.GetAsVector()) *
                    Matrix3d.PlaneToWorld(plane);

                // inverse transformation of the text (back to origin in the WCS plane)
                text.TransformBy(xform.Inverse());

                // compute the frame vertices coordinates
                var extents = text.GeometricExtents;
                double offsetDist = text.Height * 0.2,
                    minX = extents.MinPoint.X - offsetDist,
                    minY = extents.MinPoint.Y - offsetDist,
                    maxX = extents.MaxPoint.X + offsetDist,
                    maxY = extents.MaxPoint.Y + offsetDist;

                // draw the frame in WCS, then transform it in the text "UCS Object"
                var vertices = new Point2d[]
                {
                    new Point2d(minX, minY),
                    new Point2d(maxX, minY),
                    new Point2d(maxX, maxY),
                    new Point2d(minX, maxY)
                };
                var frame = new Polyline()
                {
                    Closed = true,
                };
                frame.CreatePolyline(vertices);
                frame.TransformBy(xform);
                return frame;
            }
        }
    }
}