using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem
{
    public static class Utils
    {
        public static bool IsTextLine(this Line line)
        {
            double tor = Math.PI/4;
            var angle = Math.Abs(line.Angle);
            while (angle > Math.PI)
            {
                angle -= Math.PI;
            }
            return angle < tor || Math.Abs(angle - Math.PI) < tor;
        }
    }
}
