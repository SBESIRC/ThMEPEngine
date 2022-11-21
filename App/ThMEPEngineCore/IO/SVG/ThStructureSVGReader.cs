using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Svg;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThStructureSVGReader
    {
        public ThSvgParseInfo ParseInfo { get; private set; }
        public ThStructureSVGReader()
        {
            ParseInfo = new ThSvgParseInfo();
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
            var docProperties = new Dictionary<string, string>();           
            foreach (var attribute in doc.CustomAttributes)
            {
                docProperties.Add(attribute.Key, attribute.Value);
            }
            foreach (var children in doc.Children)
            {
                try
                {
                    if (children is SvgUnknownElement svgUnknownElement)
                    {
                        var originOffset = svgUnknownElement.CustomAttributes.ParseOrginOffset();
                        if(!string.IsNullOrEmpty(originOffset))
                        {
                            docProperties.Add(ThSvgPropertyNameManager.OriginOffsetPropertyName, originOffset);
                            continue;
                        }
                        var floorInfo = svgUnknownElement.CustomAttributes.ParseFloorInfo();
                        if (floorInfo != null)
                        {
                            floorInfos.Add(floorInfo);
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
                        properties.Add(ThSvgPropertyNameManager.LineTypePropertyName, "DASH");
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
                    if (!properties.ContainsKey("type"))
                    {
                        continue;
                    }
                    if (children is SvgPath svgPath)
                    {
                        var curves = svgPath.ParseSvgPath();
                        var cleanCurves = Clean(curves.ToCollection());
                        if(cleanCurves.Count==0)
                        {
                            continue;
                        }
                        if (properties["type"].ToString() == "IfcSlab")
                        {
                            if (cleanCurves.Count > 1)
                            {
                                var polygons = BuildArea(cleanCurves);
                                polygons.OfType<Entity>().ForEach(e => results.Add(CreateThGeometry(e, properties)));
                            }
                            else
                            {
                                cleanCurves.OfType<Curve>().ForEach(c => results.Add(CreateThGeometry(c, properties)));
                            }
                        }
                        else
                        {
                            cleanCurves.OfType<Curve>().ForEach(c => results.Add(CreateThGeometry(c, properties)));
                        }
                    }
                    else if (children is SvgRectangle svgRect)
                    {
                        var poly = svgRect.CreateRectangle();
                        results.Add(CreateThGeometry(poly, properties));
                    }
                    else if (children is SvgLine svgLine)
                    {
                        var line = svgLine.CreateLine();
                        results.Add(CreateThGeometry(line, properties));
                    }
                    else if (children is SvgCircle svgCircle)
                    {
                        var circle = svgCircle.CreateCircle();
                        results.Add(CreateThGeometry(circle, properties));
                    }
                    else if (children is SvgEllipse sgvEllipse)
                    {
                        var ellipse = sgvEllipse.CreateEllipse();
                        results.Add(CreateThGeometry(ellipse, properties));
                    }
                    else if (children is SvgPolygon svgPolygon)
                    {
                        var polygon = svgPolygon.CreatePolygon();
                        if (polygon.Length > 0.0)
                        {
                            results.Add(CreateThGeometry(polygon, properties));
                        }
                    }
                    else if (children is SvgPolyline svgPolyline)
                    {
                        var poly = svgPolyline.CreatePolyline();
                        if (poly.Length > 0.0)
                        {
                            results.Add(CreateThGeometry(poly, properties));
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
                        results.Add(CreateThGeometry(dbText, properties));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                catch
                {
                }
            }
            // 收集结果
            ParseInfo.Geos = results;
            ParseInfo.FloorInfos = floorInfos.OrderBy(o=>o.BottomElevation).ToList();
            ParseInfo.DocProperties = docProperties;
        }
        
        private DBObjectCollection BuildArea(DBObjectCollection objs)
        {
            var results = objs.BuildArea();
            results = results.FilterSmallArea(1.0);
            return results;
        }
        private DBObjectCollection Clean(DBObjectCollection polygons)
        {
            if(polygons.Count==0)
            {
                return new DBObjectCollection();
            }
            else
            {
                var results = polygons.FilterSmallArea(1.0);
                var simplifier = new ThPolygonalElementSimplifier();
                results = simplifier.Normalize(results);
                results = results.FilterSmallArea(1.0);
                results = simplifier.MakeValid(results);
                results = results.FilterSmallArea(1.0);
                results = simplifier.Simplify(results);
                results = results.FilterSmallArea(1.0);
                return results;
            }            
        }
        //private Matrix3d GetTransformMatrix(SvgTransformCollection svgTransforms)
        //{
        //    if(svgTransforms==null)
        //    {
        //        return Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin);
        //    }
        //    else
        //    {
        //        var matrix2d = Multiply(GetMatrixs(svgTransforms));
        //        var matrix3d = ToMatrix3d(matrix2d);
        //        var rotateMt = Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin);
        //        var mt = rotateMt.PreMultiplyBy(matrix3d);
        //        return mt;
        //    }            
        //}

        private ThGeometry CreateThGeometry(Entity boundary, Dictionary<string, object> properties)
        {
            return ThGeometry.Create(boundary, properties);
        }
    }
}
