using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem
{
    public static class Utils
    {
        /// <summary>
        /// 判断一条直线是否为水平线
        /// </summary>
        public static bool IsHorizontalLine(this Line line)
        {
            double tor = 0.02;
            var angle = Math.Abs(line.Angle);
            while(angle > Math.PI)
            {
                angle -= Math.PI;
            }
            return angle < tor || Math.Abs(angle - Math.PI)<tor;
        }
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
