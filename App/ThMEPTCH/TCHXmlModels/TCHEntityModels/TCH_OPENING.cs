using System.Collections.Generic;
using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;
using ThMEPTCH.TCHXmlModels.TCHOpeningAttributes;

namespace ThMEPTCH.TCHXmlModels.TCHEntityModels
{
    public class TCH_OPENING:TCHXmlEntity
    {
        public XmlDouble Height { get; set; }
        public XmlDouble Length { get; set; }
        public XmlDouble Thickness { get; set; }
        public XmlAngle Ang { get; set; }
        public XmlAngle RoataeAngle { get; set; }
        public XmlString Open_ang { get; set; }
        public XmlString Highwin_ID { get; set; }
        public XmlString Upperwin_ID { get; set; }
        public XmlPoint Center_point { get; set; }
        public XmlString Label { get; set; }
        public XmlMatrix3d Matrix3d { get; set; }
        public XmlString Property { get; set; }
        [XmlArrayItem("Link_ID")]
        public List<XmlString> Link_WALL { get; set; }
        public Lib_Block Lib_Block { get; set; }
    }
}
