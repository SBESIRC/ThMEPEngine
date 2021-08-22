using System;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;


namespace ThMEPEngineCore.Extension
{
    public static class ThIfcBeamExtension
    {
        public static void ExtendBoth(this ThIfcBeam beam, double startExtendLength, double endExtendLength)
        {
            if (beam is ThIfcLineBeam lineBeam)
            {
                var outliner = new ThLineBeamOutliner(lineBeam);
                outliner.ExtendBoth(startExtendLength, endExtendLength);
                lineBeam.StartPoint = outliner.StartPoint;
                lineBeam.EndPoint = outliner.EndPoint;
                lineBeam.Outline = outliner.Outline;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 梁截面高度
        /// </summary>
        /// <param name="beam"></param>
        /// <returns></returns>
        public static double SectionHeight(this ThIfcBeam beam)
        {
            return beam.Height;
        }

        /// <summary>
        /// 梁底标高
        /// </summary>
        /// <param name="beam"></param>
        /// <returns></returns>
        public static double BottomElevation(this ThIfcBeam beam)
        {
            if (beam.DistanceToFloor < 0)
            {
                return Math.Abs(beam.DistanceToFloor);
            }
            else
            {
                return 0;
            }
        }
    }
}
