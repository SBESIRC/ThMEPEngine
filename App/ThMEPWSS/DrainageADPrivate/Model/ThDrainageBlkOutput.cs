using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageADPrivate.Model
{
    internal class ThDrainageBlkOutput
    {
        public Point3d Position { get; private set; }

        public string Name { get; set; }

        public Vector3d Dir { get; set; }

        public Dictionary<string, string> Visibility { get; set; }

        public double Scale { get; set; } = 1;

        public string Layer { get; set; }

        public ThDrainageBlkOutput(Point3d pt)
        {
            Position = pt;
            Visibility = new Dictionary<string, string>();
        }

        public void TransformBy(Matrix3d matrix)
        {
            Position = Position.TransformBy(matrix);
            Dir = Dir.TransformBy(matrix);
        }
    }
}
