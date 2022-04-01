using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Test
{
    internal class DisplayTool
    {
        public static string GetPlineValues(Polyline pline)
        {
            var values = new List<double>();
            var pts = pline.GetPoints();
            foreach (var pt in pts)
            {
                values.Add(pt.X);
                values.Add(pt.Y);
            }
            string rst = "";
            for (int i = 0; i < values.Count; i++)
            {
                rst += Convert.ToString(values[i]);
                if (i < values.Count - 1)
                {
                    rst += ',';
                }
            }
            return rst;
        }

        public static string GetPlineValues(List<Point3dEx> pts)
        {
            var values = new List<double>();
            foreach (var ptex in pts)
            {
                var pt = ptex._pt;
                values.Add(pt.X);
                values.Add(pt.Y);
            }
            string rst = "";
            for (int i = 0; i < values.Count; i++)
            {
                rst += Convert.ToString(values[i]);
                if (i < values.Count - 1)
                {
                    rst += ',';
                }
            }
            return rst;
        }
    }
}
