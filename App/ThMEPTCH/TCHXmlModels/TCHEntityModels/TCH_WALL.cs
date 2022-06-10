using System.Collections.Generic;
using System.Xml.Serialization;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;
using ThMEPTCH.TCHXmlModels.TCHWallAttributes;

namespace ThMEPTCH.TCHXmlModels.TCHEntityModels
{
    public class TCH_WALL : TCHXmlEntity
    {
        public Link_WinDoor Link_WinDoor { get; set; }
        [XmlArrayItem("Link_ID")]
        public List<XmlString> Link_Column { get; set; }
        public Height Height { get; set; }
        public Width Width { get; set; }
        public Baseline Baseline { get; set; }
        public DrawPara DrawPara { get; set; }
        public XmlInt ShapeWall { get; set; }
    }
}
