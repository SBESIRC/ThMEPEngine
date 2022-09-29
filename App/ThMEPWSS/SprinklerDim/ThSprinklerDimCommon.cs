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
        public static string Layer_Dim = "W-FRPT-SPRL-DIMS";
        public static string Layer_UnTagX = "AI-X方向未标注";
        public static string Layer_UnTagY = "AI-Y方向未标注";
        public static string Layer_Pipe = "W-FRPT-SPRL-PIPE";

        public static string LayerFilter_W = "W-";
        public static string LayerFilter_DIMS = "-DIMS";
        public static string LayerFilter_NOTE = "-NOTE";
        public static string LayerFilter_SPRL = "SPRL";
        public static string BlkFilter_Sprinkler = "喷头";

        //public static string Style_DimTCH = "TH-STYLE3";
        public static string Style_DimCAD = "TH-DIM100-W";

    }
}
