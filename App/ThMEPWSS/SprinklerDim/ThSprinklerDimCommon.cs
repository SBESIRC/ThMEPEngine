using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.SprinklerDim
{
    public class ThSprinklerDimCommon
    {
        public static Tolerance Tol_ptToLine = new Tolerance(10, 10);
        public static string Layer_Dim = "W-WSUP-DIMS";
        public static string Style_Dim = "TH-STYLE3";

    }
}
