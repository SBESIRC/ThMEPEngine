using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Model
{
    public class ThVertex : IEquatable<ThVertex>
    {
        public Point3d Position { get; set; }
        public ThVertex(Point3d position)
        {
            Position = position;
        }

        public bool Equals(ThVertex other)
        {
            return Position.IsEqualTo(other.Position, new Tolerance(1.0, 1.0));
        }
    }
}
