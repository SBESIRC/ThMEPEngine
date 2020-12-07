﻿using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWToiletPipeParameters
    {
        public List<double> Diameter { get; set; }
        public List<string> Identifier { get; set; }
        public int Number { get; set; }
        public ThWToiletPipeParameters(bool separation_water, bool caisson, int floor)
        {
            Identifier = new List<string>();
            if (!separation_water)
            {
                if (!caisson)
                {
                    Number = 2;
                    Identifier.Add(string.Format("通气TLx1"));
                    Identifier.Add(string.Format("污废PLx1"));
                }
                else
                {
                    Number = 3;
                    Identifier.Add(string.Format("沉箱DLx1"));
                    Identifier.Add(string.Format("通气TLx1"));
                    Identifier.Add(string.Format("污废PLx1"));
                }
            }
            else
            {
                if (!caisson)
                {
                    Number = 3;
                    Identifier.Add(string.Format("污水WLx1"));
                    Identifier.Add(string.Format("通气TLx1"));
                    Identifier.Add(string.Format("水FLx1"));

                }
                else
                {
                    Number = 4;
                    Identifier.Add(string.Format("沉箱DLx1"));
                    Identifier.Add(string.Format("污水WLx1"));
                    Identifier.Add(string.Format("通气TLx1"));
                    Identifier.Add(string.Format("水FLx1"));
                }
            }
            Diameter = new List<double>();
            for (int i = 0; i < Number; i++)
            {
                if (floor >= 150)
                {
                    Diameter.Add(100.00);//原要求为150
                }
                else
                {
                    Diameter.Add(100.00);
                }
            }
            Diameter[Number - 2] = 100;
        }
    }

    public class ThWToiletPipe : ThWPipe
    {
        public Point3d Center { get; set; }
    }
}
