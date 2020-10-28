using System;
using Autodesk.AutoCAD.Geometry;
using System.Text.RegularExpressions;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Service
{
    public class ThStructureBeamUtils
    {
        public static Scale2d Size(string str)
        {
            var match = BeamAnnotaionMatch(str);
            if (!match.Success)
            {
                return new Scale2d();
            }
            return new Scale2d(
                Convert.ToDouble(match.Groups[1].Value),
                Convert.ToDouble(match.Groups[2].Value));
        }

        private static Match BeamAnnotaionMatch(string str)
        {
            var match = Regex.Match(str, @"^(\s*\d*[.]?\d*\s*)[xX](\s*\d*[.]?\d*)");
            return match;
        }

        public static bool IsBeamAnnotaion(string str)
        {
            var match = BeamAnnotaionMatch(str);
            return match.Success;
        }


        public static Point3d Coordinate(string text)
        {
            string[] patterns = text.Split(',');
            return new Point3d(Convert.ToDouble(patterns[0]), Convert.ToDouble(patterns[1]), 0);
        }
        public static bool IsLooseCollinear(ThIfcLineBeam firstBeam,ThIfcLineBeam secondBeam)
        {
            return ThMEPNTSExtension.IsLooseCollinear(
                firstBeam.StartPoint, firstBeam.EndPoint,
                secondBeam.StartPoint, secondBeam.EndPoint);
        }
        /// <summary>
        /// 判断直梁的端口与弧梁端口是否共线
        /// </summary>
        /// <param name="firstBeam"></param>
        /// <param name="portPt"></param>
        /// <param name="secondBeam"></param>
        /// <returns></returns>
        public static bool IsLooseCollinear(ThIfcLineBeam lineBeam, Point3d portPt, ThIfcArcBeam arcBeam)
        {
            if(portPt.DistanceTo(arcBeam.StartPoint)< portPt.DistanceTo(arcBeam.EndPoint))
            {
                return ThMEPNTSExtension.IsLooseCollinear(lineBeam.StartPoint, lineBeam.EndPoint,
                arcBeam.StartPoint, arcBeam.StartPoint + arcBeam.StartTangent.MultiplyBy(100.0));
            }
            else
            {
                return ThMEPNTSExtension.IsLooseCollinear(lineBeam.StartPoint, lineBeam.EndPoint,
                arcBeam.EndPoint, arcBeam.EndPoint + arcBeam.EndTangent.MultiplyBy(100.0));
            }    
        }
    }
}