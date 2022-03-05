using System.Collections.Generic;
using ThCADExtension;
using TianHua.Publics.BaseCode;

namespace ThMEPHVAC.IO
{
    public class PortSizeParameter
    {
        public double DuctWidth { get; set; }
        public double DuctHeight { get; set; }
        public double SectionArea { get; set; }
        public double AspectRatio { get; set; }
    }

    public class ThPortParameterJsonReader : ThDuctJsonReader
    {
        public List<PortSizeParameter> Parameters { get; set; }

        public ThPortParameterJsonReader()
        {
            var portParameterString = ReadWord(ThCADCommon.PortSizeParametersPath());
            Parameters = FuncJson.Deserialize<List<PortSizeParameter>>(portParameterString);
        }
    }
}
