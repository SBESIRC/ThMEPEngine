using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcArcBeam : ThIfcBeam, ICloneable
    {
        public object Clone()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 起始端
        /// </summary>
        public Vector3d StartTangent { get; set; }
        public Vector3d EndTangent { get; set; }
        public double Radius { get; set; }
        public ThIfcArcBeam()
        {
        }
        public override Polyline Extend(double length, double width)
        {
            //TODO
            return this.Outline.Clone() as Polyline;
        }

        public override Polyline ExtendBoth(double startExtendLength, double endExtendLength)
        {
            throw new System.NotImplementedException();
        }
    }
}
