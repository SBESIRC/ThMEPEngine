using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcArcBeam : ThIfcBeam
    {
        public double Radius { get; set; }
        public Vector3d StartTangent { get; set; }
        public Vector3d EndTangent { get; set; }
        public override void TransformBy(Matrix3d transform)
        {
            throw new NotSupportedException();
        }
    }
}
