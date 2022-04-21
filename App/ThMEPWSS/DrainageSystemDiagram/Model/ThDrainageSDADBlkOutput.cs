using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram.Model
{
    public class ThDrainageSDADBlkOutput
    {
        public Point3d Position { get; private set; }

        public string Name { get; set; }

        public Vector3d Dir { get; set; }

        public Dictionary<string, string> Visibility { get; set; }

        public double Scale { get; set; }

        public double BlkSize { get; set; }

        public ThDrainageSDADBlkOutput(Point3d pt)
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
