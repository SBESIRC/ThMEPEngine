using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertParameter
    {
        public Scale3d Scale { get; set; }

        public ConvertMode Mode { get; set; }

        public List<ThBConvertRule> Rules { get; set; }
    }
}
