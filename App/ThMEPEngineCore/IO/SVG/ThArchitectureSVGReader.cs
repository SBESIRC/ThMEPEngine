using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Svg;
using ThMEPEngineCore.Model;
using ThCADExtension;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThArchitectureSVGReader 
    {
        public ThSvgParseInfo ParseInfo { get; private set; }
        public ThArchitectureSVGReader()
        {
            ParseInfo= new ThSvgParseInfo();
        }
        public void ReadFromContent(string content)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content);
            var doc = SvgDocument.Open(xmlDoc);
            Parse(doc);
        }

        public void ReadFromFile(string filePath)
        {
            var doc = SvgDocument.Open(filePath);
            Parse(doc);
        }

        private void Parse(SvgDocument doc)
        {
            var results = new List<ThGeometry>();
            var floorInfos = new List<ThFloorInfo>();
            var componentInfos = new List<ThComponentInfo>();
            var docProperties = new Dictionary<string, string>();           
            foreach (var attribute in doc.CustomAttributes)
            {
                docProperties.Add(attribute.Key, attribute.Value);
            }
            foreach (var children in doc.Children)
            {
                if (children is SvgUnknownElement svgUnknownElement)
                {
                    // 解析楼层信息
                    var floorInfo = svgUnknownElement.CustomAttributes.ParseFloorInfo();
                    if (floorInfo != null)
                    {
                        floorInfos.Add(floorInfo);
                    }
                    else
                    {
                        // 解析构件信息
                        var componentInfo = svgUnknownElement.CustomAttributes.ParseComponentInfo();
                        if (componentInfo != null)
                        {
                            componentInfos.Add(componentInfo);
                        }
                    }
                    continue;
                }
                var properties = new Dictionary<string, object>();
                if (children.Stroke != null)
                {
                    //var strokecolor = (children.Stroke as SvgColourServer).Colour; // 轮廓颜色
                    //var acadColor = Autodesk.AutoCAD.Colors.Color.FromRgb(strokecolor.R, strokecolor.G, strokecolor.B);
                    //properties.Add(ThSvgPropertyNameManager.OutlineColorIndexPropertyName, acadColor.ColorIndex);
                }
                if (children.Fill != null)
                {
                    //var fillColor = (children.Fill as SvgColourServer).Colour;
                    //var acadFillColor = Autodesk.AutoCAD.Colors.Color.FromRgb(fillColor.R, fillColor.G, fillColor.B);
                    //properties.Add(ThSvgPropertyNameManager.FillColorIndexPropertyName, acadFillColor.ColorIndex);
                    properties.Add(ThSvgPropertyNameManager.FillColorPropertyName, children.Fill.ToString());
                }
                if (children.StrokeDashArray != null && children.StrokeDashArray.Count > 0)
                {
                    properties.Add(ThSvgPropertyNameManager.LineTypePropertyName, "Hidden");
                }
                else
                {
                    properties.Add(ThSvgPropertyNameManager.LineTypePropertyName, "CONTINUOUS");
                }
                if (children.CustomAttributes != null)
                {
                    foreach (var attribute in children.CustomAttributes)
                    {
                        if (attribute.Key.GetType() == typeof(string))
                        {
                            properties.Add(attribute.Key, attribute.Value);
                        }
                        else
                        {
                            properties.Add(attribute.Key.ToString(), attribute.Value);
                        }
                    }
                }
                if (children is SvgPath svgPath)
                {
                    var curves = svgPath.ParseSvgPath();
                    var material = properties.ContainsKey("material") ? properties["material"].ToString() : "";
                    var lineType = properties.ContainsKey("line-type") ? properties["line-type"].ToString() : "";
                    if (lineType == "duanmian" && IsMaterial(material))
                    {
                        if (curves.Count>1)
                        {
                            var shell = curves[0];
                            curves.RemoveAt(0);
                            var mPolygon = ThMPolygonTool.CreateMPolygon(shell, curves);
                            results.Add(mPolygon.CreateThGeometry(properties));
                        }
                        else
                        {
                            curves.ForEach(e => results.Add(e.CreateThGeometry(properties)));
                        }
                    }
                    else
                    {                        
                        curves.ForEach(c => results.Add(c.CreateThGeometry(properties)));
                    }
                }
                else if (children is SvgRectangle svgRect)
                {
                    var poly = svgRect.CreateRectangle();
                    results.Add(poly.CreateThGeometry(properties));
                }
                else if (children is SvgLine svgLine)
                {
                    var line = svgLine.CreateLine();
                    results.Add(line.CreateThGeometry(properties));
                }
                else if (children is SvgCircle svgCircle)
                {
                    var circle = svgCircle.CreateCircle();
                    results.Add(circle.CreateThGeometry(properties));
                }
                else if (children is SvgEllipse sgvEllipse)
                {
                    var ellipse = sgvEllipse.CreateEllipse();
                    results.Add(ellipse.CreateThGeometry(properties));
                }
                else if (children is SvgPolygon svgPolygon)
                {
                    var polygon = svgPolygon.CreatePolygon();
                    if (polygon.Length > 0.0)
                    {
                        results.Add(polygon.CreateThGeometry(properties));
                    }
                }
                else if (children is SvgPolyline svgPolyline)
                {
                    var poly = svgPolyline.CreatePolyline();
                    if (poly.Length > 0.0)
                    {
                        results.Add(poly.CreateThGeometry(properties));
                    }
                }
                else if (children is SvgText svgText)
                {
                    // 创建文字
                    if (!properties.ContainsKey("type"))
                    {
                        properties.Add("type", "IfcAnnotation");
                    }
                    var dbText = svgText.CreateDBText();
                    results.Add(dbText.CreateThGeometry(properties));
                }                
                else
                {
                    throw new NotSupportedException();
                }
            }
            // 收集结果
            ParseInfo.Geos = results;
            ParseInfo.FloorInfos = floorInfos.OrderBy(o => o.BottomElevation).ToList();
            ParseInfo.DocProperties = docProperties;
            ParseInfo.ComponentInfos = componentInfos;
        }

        private bool IsMaterial(string material)
        {
            return ThTextureMaterialManager.AllMaterials.Contains(material);
        }
    }
}
