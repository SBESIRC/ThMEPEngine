using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;

namespace ThMEPTCH.TCHXmlModels
{
    public class Entity_Collection : TCHXmlBaseModel
    {
        public XmlInt Collection_Index { get; set; }
        [XmlElement("Content",IsNullable =false)]
        public Content[] contents { get; set; }
    }
}
