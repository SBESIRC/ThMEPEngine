using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPTCH.Model;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.TCHXmlDataConvert.TCHEntityToThIfc
{
    [TCHConvertAttribute("天正门窗转中间数据")]
    class TCHXmlOpeningToThIfcDoorWindow : TCHConvertBase
    {
        public TCHXmlOpeningToThIfcDoorWindow()
        {
            AcceptTCHEntityTypes.Add(typeof(TCH_OPENING));
        }
        public override List<object> ConvertToBuidingElement()
        {
            var thIfcObjs = new List<object>();
            if (null == TCHXmlEntities || TCHXmlEntities.Count < 1)
                return thIfcObjs;
            foreach (var item in TCHXmlEntities)
            {
                if (null == item)
                    continue;
                if (item is TCH_OPENING opening)
                {
                    if (XmlEntityIsDoor(opening))
                    {
                        var door = XmlDoorToTHDoor(opening);
                        if (null != door)
                            thIfcObjs.Add(door);
                    }
                    else if (XmlEntityIsWindow(opening))
                    {
                        var window = XmlWindowToTHWindow(opening);
                        if (null != window)
                            thIfcObjs.Add(window);
                    }
                    else if (XmlEntityIsOpening(opening)) 
                    {
                        var open = XmlOpeningToTHOpening(opening);
                        if (null != open)
                            thIfcObjs.Add(open);
                    }
                }
            }
            return thIfcObjs;
        }
        ThTCHDoor XmlDoorToTHDoor(TCH_OPENING xmlEntity) 
        {
            var centerPt = xmlEntity.Center_point.GetCADPoint();
            var width = xmlEntity.Length.GetDoubleValue();
            var height = xmlEntity.Height.GetDoubleValue();
            var thickness = xmlEntity.Thickness.GetDoubleValue();
            var angle = xmlEntity.RoataeAngle.GetDoubleValue();
            var door = new ThTCHDoor(centerPt.Value, width, height, thickness, angle);
            SetPropertiesValue(xmlEntity, door);
            return door;
        }
        ThTCHWindow XmlWindowToTHWindow(TCH_OPENING xmlEntity) 
        {
            var centerPt = xmlEntity.Center_point.GetCADPoint();
            var width = xmlEntity.Length.GetDoubleValue();
            var height = xmlEntity.Height.GetDoubleValue();
            var thickness = xmlEntity.Thickness.GetDoubleValue();
            var angle = xmlEntity.RoataeAngle.GetDoubleValue();
            var window = new ThTCHWindow(centerPt.Value, width, height, thickness, angle);
            SetPropertiesValue(xmlEntity, window);

            return window;
        }
        ThTCHOpening XmlOpeningToTHOpening(TCH_OPENING xmlEntity) 
        {
            var centerPt = xmlEntity.Center_point.GetCADPoint();
            var width = xmlEntity.Length.GetDoubleValue();
            var height = xmlEntity.Height.GetDoubleValue();
            var thickness = xmlEntity.Thickness.GetDoubleValue();
            var angle = xmlEntity.RoataeAngle.GetDoubleValue();
            var opening = new ThTCHOpening(centerPt.Value, width, height, thickness, angle);
            SetPropertiesValue(xmlEntity,opening);
            return opening;
        }
        void SetPropertiesValue( TCH_OPENING xmlEntity, ThTCHElement ifcElement) 
        {
            ifcElement.Uuid = xmlEntity.Object_ID.value;
            ifcElement.Usage = GetLinkWallIds(xmlEntity);
            ifcElement.Properties.Add(xmlEntity.Open_ang.name, xmlEntity.Open_ang.value);
            ifcElement.Properties.Add(xmlEntity.Property.name, xmlEntity.Property.value);
            ifcElement.Properties.Add(xmlEntity.RoataeAngle.name, xmlEntity.RoataeAngle.value);
            ifcElement.Properties.Add(xmlEntity.Ang.name, xmlEntity.Ang.value);
            ifcElement.Properties.Add(xmlEntity.Highwin_ID.name, xmlEntity.Highwin_ID.value);
            ifcElement.Properties.Add(xmlEntity.Center_point.name, xmlEntity.Center_point.value);
            ifcElement.Properties.Add(xmlEntity.Upperwin_ID.name, xmlEntity.Upperwin_ID.value);
            //洞口时可能有些信息没有
            if (null != xmlEntity.Matrix3d)
                ifcElement.Properties.Add(xmlEntity.Matrix3d.name, xmlEntity.Matrix3d.value);
            if (null != xmlEntity.Lib_Block)
            {
                if (null != xmlEntity.Lib_Block.Lib_2D)
                    ifcElement.Properties.Add(xmlEntity.Lib_Block.Lib_2D.name, xmlEntity.Lib_Block.Lib_2D.value);
                if (null != xmlEntity.Lib_Block.Lib_3D)
                    ifcElement.Properties.Add(xmlEntity.Lib_Block.Lib_3D.name, xmlEntity.Lib_Block.Lib_3D.value);
            }
        }
        bool XmlEntityIsDoor(TCH_OPENING xmlEntity) 
        {
            if (null == xmlEntity.Property)
                return false;
            if (string.IsNullOrEmpty(xmlEntity.Property.value))
                return false;
            return xmlEntity.Property.value.Contains("门");
        }
        bool XmlEntityIsWindow(TCH_OPENING xmlEntity)
        {
            if (null == xmlEntity.Property)
                return false;
            if (string.IsNullOrEmpty(xmlEntity.Property.value))
                return false;
            return xmlEntity.Property.value.Contains("窗");
        }
        bool XmlEntityIsOpening(TCH_OPENING xmlEntity) 
        {
            if (null == xmlEntity.Property)
                return false;
            if (string.IsNullOrEmpty(xmlEntity.Property.value))
                return false;
            return xmlEntity.Property.value.Contains("洞");
        }

        string GetLinkWallIds(TCH_OPENING xmlEntity) 
        {
            string ids = "";
            if (null == xmlEntity || xmlEntity.Link_WALL == null || xmlEntity.Link_WALL.Count < 1)
                return ids;
            ids = string.Format(",{0},", string.Join(",", xmlEntity.Link_WALL.Select(c => c.value).ToArray()));
            return ids;
        }
    }
}
