using System;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcBeamStandardCase : ThIfcBeam
    {
        //
        public override Polyline Extend(double length, double width)
        {
            throw new NotImplementedException();
        }
    }
}
