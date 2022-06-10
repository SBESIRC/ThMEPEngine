using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHEntityModels
{
    public class TCHFloor : TCHXmlBaseModel
    {
        public XmlInt Entities_Index { get; set; }
        public XmlString Floor_Num { get; set; }
        public XmlDouble Floor_Elevation { get; set; }
        public XmlDouble Floor_Height { get; set; }
    }
}
