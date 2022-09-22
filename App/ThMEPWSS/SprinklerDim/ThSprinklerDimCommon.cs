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
        public static string Layer_Dim = "W-FRPT-SPRL-DIMS";
        public static string Layer_UnTagX = "AI-X方向未标注";
        public static string Layer_UnTagY = "AI-Y方向未标注";

        //public static string Style_DimTCH = "TH-STYLE3";
        public static string Style_DimCAD = "TH-DIM100-W";

    }
}
