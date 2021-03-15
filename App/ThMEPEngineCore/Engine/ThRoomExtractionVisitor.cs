using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomExtractionVisitor : ThBuildingElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is Polyline polyline)
            {
                elements.AddRange(Handle(polyline, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        private List<ThRawIfcBuildingElementData> Handle(Polyline polyline, Matrix3d matrix)
        {
            var results = new List<ThRawIfcBuildingElementData>();
            if (IsBuildElement(polyline) && CheckLayerValid(polyline) && IsRoom(polyline))
            {
                var clone = polyline.WashClone();
                clone.TransformBy(matrix);
                results.Add(CreateBuildingElementData(clone, polyline.Hyperlinks[0].Description));
            }
            return results;
        }

        private ThRawIfcBuildingElementData CreateBuildingElementData(Curve curve,string description)
        {
            var propertySet = CreateWithHyperlink(description);
            var category = "";
            if(propertySet.Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY))
            {
                category = propertySet.Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY];
            }
            return new ThRawIfcBuildingElementData()
            {
                Geometry = curve,
                Data = category
            };
        }

        private bool IsRoom(Entity entity)
        {
            var thPropertySet = CreateWithHyperlink(entity.Hyperlinks[0].Description);
            if( thPropertySet.Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY) &&
                thPropertySet.Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_Boundary))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private new bool IsBuildElement(Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }

        private ThPropertySet CreateWithHyperlink(string hyperlink)
        {
            var propertySet = new ThPropertySet();
            propertySet.Section = "";
            int index = -1;
            // 按分割符“__”分割属性
            var properties = Regex.Split(hyperlink.Substring(index + 1, hyperlink.Length - index - 1), "__");
            foreach (var property in properties)
            {
                var keyValue = Regex.Split(property, "：");
                if (ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTIES.Contains(keyValue[0]))
                {
                    propertySet.Properties.Add(keyValue[0], keyValue[1]);
                }
            }
            // 返回属性集
            return propertySet;
        }
    }
}
