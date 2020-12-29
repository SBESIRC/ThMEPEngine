using ThCADExtension;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;

namespace ThMEPHVAC.IO
{
    public class DuctSizeParameter
    {
        public double DuctWidth { get; set; }
        public double DuctHeight { get; set; }
        public double SectionArea { get; set; }
        public double AspectRatio { get; set; }
    }

    public class ThDuctParameterJsonReader : ThDuctJsonReader
    {
        public List<DuctSizeParameter> Parameters { get; set; }

        public ThDuctParameterJsonReader()
        {
            var ductParameterString = ReadWord(ThCADCommon.DuctSizeParametersPath());
            Parameters = FuncJson.Deserialize<List<DuctSizeParameter>>(ductParameterString);
        }
    }
}
