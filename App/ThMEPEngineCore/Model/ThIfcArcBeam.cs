using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcArcBeam : ThIfcBeam, ICloneable
    {
        public Vector3d StartTangent { get; set; }
        public Vector3d EndTangent { get; set; }
        public double Radius { get; set; }
        public ThIfcArcBeam()
        {
        }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public override Polyline Extend(double length, double width)
        {
            throw new NotImplementedException();
        }

        public override Polyline ExtendBoth(double startExtendLength, double endExtendLength)
        {
            throw new NotImplementedException();
        }

        public override Curve Centerline()
        {
            throw new NotImplementedException();
        }
    }
}
