using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcBeamStandardCase : ThIfcBeam
    {
        public override Curve Centerline()
        {
            throw new NotImplementedException();
        }

        //
        public override Polyline Extend(double length, double width)
        {
            throw new NotImplementedException();
        }

        public override Polyline ExtendBoth(double startExtendLength, double endExtendLength)
        {
            throw new NotImplementedException();
        }
    }
}
