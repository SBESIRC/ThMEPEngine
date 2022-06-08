using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels.TCHWallAttributes
{
    public class Link_WinDoor : TCHXmlBaseModel
    {
        //[XmlArrayItem("Link_ID")]
        [XmlElement("Link_ID", IsNullable = false)]
        public XmlString[] Link_Ids { get; set; }
    }
}
