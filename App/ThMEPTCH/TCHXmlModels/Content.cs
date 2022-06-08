using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlModels
{
    public class Content : TCHXmlBaseModel
    {
        [XmlElement("TCH_WALL")]
        public TCH_WALL[] TCH_WALLs { get; set; }
        [XmlElement("TCH_OPENING")]
        public TCH_OPENING[] TCH_OPENINGs { get; set; }
        [XmlElement("TCH_SLAB")]
        public TCH_SLab[] TCH_SLabs { get; set; }
    }
}
