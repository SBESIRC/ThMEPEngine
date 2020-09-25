using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Geometry
{
    public static class MinRectangle
    {
        /// <summary>
        /// Calculates the minimum bounding box.
        /// </summary>
        /// <param name="points">Bounding Box.</param>
        public static Polyline Calculate(Polyline polyline)
        {
            List<Point2d> points = new List<Point2d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint2dAt(i));
            }

            Rectangle2d minBox = null;
            var minAngle = 0d;

            //foreach edge of the convex hull
            for (var i = 0; i < points.Count; i++)
            {
                var nextIndex = i + 1;

                var current = points[i];
                var next = points[nextIndex % points.Count];

                var segment = new Line(new Point3d(current.X, current.Y, 0), new Point3d(next.X, next.Y, 0));

                //min / max points
                var top = double.MinValue;
                var bottom = double.MaxValue;
                var left = double.MaxValue;
                var right = double.MinValue;

                //get angle of segment to x axis
                var angle = AngleToXAxis(segment);

                //rotate every point and get min and max values for each direction
                foreach (var p in points)
                {
                    var rotatedPoint = RotateToXAxis(p, angle);

                    top = Math.Max(top, rotatedPoint.Y);
                    bottom = Math.Min(bottom, rotatedPoint.Y);

                    left = Math.Min(left, rotatedPoint.X);
                    right = Math.Max(right, rotatedPoint.X);
                }

                //create axis aligned bounding box
                var box = new Rectangle2d(new Point2d(left, bottom), new Point2d(right, top));

                if (minBox == null || minBox.Area() > box.Area())
                {
                    minBox = box;
                    minAngle = angle;
                }
            }

            //rotate axis algined box back
            List<Point2d> boxPoints = minBox.Points.Select(p => RotateToXAxis(p, -minAngle)).ToList();
            Polyline minimalBoundingBox = new Polyline(boxPoints.Count);
            Point2d thisP = boxPoints.First();
            int index = 0;
            minimalBoundingBox.AddVertexAt(index, thisP, 0, 0, 0);
            boxPoints.Remove(thisP);
            while (boxPoints.Count > 0)
            {
                thisP = boxPoints.OrderBy(x => x.GetDistanceTo(thisP)).First();
                index++;
                minimalBoundingBox.AddVertexAt(index, thisP, 0, 0, 0);
                boxPoints.Remove(thisP);
            }
            minimalBoundingBox.Closed = true;

            return minimalBoundingBox;
        }

        /// <summary>
        /// Calculates the angle to the X axis.
        /// </summary>
        /// <returns>The angle to the X axis.</returns>
        /// <param name="s">The segment to get the angle from.</param>
        static double AngleToXAxis(Line s)
        {
            return -Math.Atan(s.Delta.Y / s.Delta.X);
        }

        /// <summary>
        /// Rotates vector by an angle to the x-Axis
        /// </summary>
        /// <returns>Rotated vector.</returns>
        /// <param name="v">Vector to rotate.</param>
        /// <param name="angle">Angle to trun by.</param>
        static Point2d RotateToXAxis(Point2d v, double angle)
        {
            var newX = v.X * Math.Cos(angle) - v.Y * Math.Sin(angle);
            var newY = v.X * Math.Sin(angle) + v.Y * Math.Cos(angle);

            return new Point2d(newX, newY);
        }
    }

    public class Rectangle2d
    {
        public Point2d Location { get; set; }

        public Vector2d Size { get; set; }

        public Rectangle2d()
        {
        }

        public Rectangle2d(Point2d a, Point2d c) : this()
        {
            Location = a;
            Size = c - a;
        }

        public double Area()
        {
            return Size.X * Size.Y;
        }

        public Point2d[] Points
        {
            get
            {
                return new[] {
                    new Point2d (Location.X, Location.Y),
                    new Point2d (Location.X + Size.X, Location.Y),
                    new Point2d (Location.X + Size.X, Location.Y + Size.Y),
                    new Point2d (Location.X, Location.Y + Size.Y)
                };
            }
        }
    }
}
