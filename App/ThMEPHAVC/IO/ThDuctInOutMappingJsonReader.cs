using ThCADExtension;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;

namespace ThMEPHVAC.IO
{
    public class InOutDuctPositionInfo
    {
        public string WorkingScenario { get; set; }
        public string InnerRoomDuctType { get; set; }
        public string OuterRoomDuctType { get; set; }
    }

    public class ThDuctInOutMappingJsonReader : ThDuctJsonReader
    {

        public List<InOutDuctPositionInfo> Mappings { get; set; }

        public ThDuctInOutMappingJsonReader()
        {
            var inOutDuctPositionInfo = ReadWord(ThCADCommon.DuctInOutMapping());
            Mappings = FuncJson.Deserialize<List<InOutDuctPositionInfo>>(inOutDuctPositionInfo);
        }
    }
}
