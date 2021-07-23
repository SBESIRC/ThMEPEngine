using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDADBlkOutput
    {
        public Point3d position { get; private set; }

        public string name { get; set; }

        public Vector3d dir { get; set; }

        public Dictionary<string, string> visibility { get; set; }

        public ThDrainageSDADBlkOutput(Point3d pt)
        {
            position = pt;
            visibility = new Dictionary<string, string>();
        }

        public void TransformBy(Matrix3d matrix)
        {
            position = position.TransformBy(matrix);
            dir = dir.TransformBy(matrix);
        }
    }
}
