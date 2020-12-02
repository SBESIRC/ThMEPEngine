using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPHAVC.Duct
{
    public class ThDuctLineSimplifier
    {
        public class SimplifyParameters
        {
            public double DistGap2Merge { get; set; }

            public double DistGap2Extend { get; set; }

            public double AngleTolerance { get; set; }

            public double ArcChord { get; set; }
        }

        public static List<Line> Simplifier(DBObjectCollection curves, SimplifyParameters parameters)
        {
            return ThMEPLineExtension.LineSimplifier(curves,
                parameters.ArcChord,
                parameters.DistGap2Extend,
                parameters.DistGap2Merge,
                parameters.AngleTolerance);
        }
    }
}
