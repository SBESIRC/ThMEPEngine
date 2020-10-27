using System;
using Autodesk.AutoCAD.Geometry;
using System.Text.RegularExpressions;

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
    }
}