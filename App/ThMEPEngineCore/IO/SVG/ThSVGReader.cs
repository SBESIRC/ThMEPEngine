using System;
using System.Xml;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using Svg;
using Svg.Transforms;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThSVGReader
    {
        public List<ThGeometry> Geos { get; set; }
        public List<ThFloorInfo> FloorInfos { get; set; }
        public Dictionary<string, string> DocProperties { get; set; }
        public ThSVGReader()
        {
            Geos = new List<ThGeometry>();
            FloorInfos = new List<ThFloorInfo>();
            DocProperties = new Dictionary<string, string>();
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
                if (children is SvgUnknownElement svgUnknownElement)
                {
                    var floorInfo = ParseFloorInfo(svgUnknownElement.CustomAttributes);
                    if(floorInfo!=null)
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

                if (children is SvgPath svgPath)
                {
                    var curves = ParseSvgPath(svgPath);
                    curves.ForEach(c => results.Add(CreateThGeometry(c, properties)));
                }
                else if (children is SvgRectangle svgRect)
                {
                    var poly = CreateRectangle(svgRect);
                    results.Add(CreateThGeometry(poly, properties));
                }
                else if (children is SvgLine svgLine)
                {
                    var line = CreateLine(svgLine);
                    results.Add(CreateThGeometry(line, properties));
                }
                else if (children is SvgCircle svgCircle)
                {
                    var circle = CreateCircle(svgCircle);
                    results.Add(CreateThGeometry(circle, properties));
                }
                else if (children is SvgEllipse sgvEllipse)
                {
                    var ellipse = CreateEllipse(sgvEllipse);
                    results.Add(CreateThGeometry(ellipse, properties));
                }
                else if (children is SvgPolygon svgPolygon)
                {
                    var polygon = CreatePolygon(svgPolygon);
                    if (polygon.Length > 0.0)
                    {
                        results.Add(CreateThGeometry(polygon, properties));
                    }
                }
                else if (children is SvgPolyline svgPolyline)
                {
                    var poly = CreatePolyline(svgPolyline);
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
                    var dbText = CreateDBText(svgText);
                    results.Add(CreateThGeometry(dbText, properties));
                }                
                else
                {
                    throw new NotSupportedException();
                }
            }
            // 收集结果
            Geos = results;
            FloorInfos = floorInfos;
            DocProperties = docProperties;
        }

        private Line CreateLine(SvgLine svgLine)
        {
            return new Line(new Point3d(svgLine.StartX.Value, svgLine.StartY.Value, 0.0),
                        new Point3d(svgLine.EndX.Value, svgLine.EndY.Value, 0.0));
        }

        private Circle CreateCircle(SvgCircle svgCircle)
        {
            return new Circle(new Point3d(svgCircle.Center.X, svgCircle.Center.Y, 0), Vector3d.ZAxis, svgCircle.Radius.Value);
        }

        private Polyline CreatePolyline(SvgPolyline svgPolyline)
        {
            // 暂时未考虑矩阵转换
            var points = new Point2dCollection();
            for (int i = 0; i < svgPolyline.Points.Count; i += 2)
            {
                var pt = new Point2d(svgPolyline.Points[i].Value, svgPolyline.Points[i + 1].Value);
                points.Add(pt);
            }
            if (points.Count > 1)
            {
                return ThDrawTool.CreatePolyline(points, false);
            }
            else
            {
                return new Polyline();
            }
        }

        private Polyline CreatePolygon(SvgPolygon svgPolygon)
        {
            // 暂时未考虑矩阵转换
            var points = new Point2dCollection();
            for (int i = 0; i < svgPolygon.Points.Count; i += 2)
            {
                var pt = new Point2d(svgPolygon.Points[i].Value, svgPolygon.Points[i + 1].Value);
                points.Add(pt);
            }
            if (points.Count > 1)
            {
                return ThDrawTool.CreatePolyline(points);
            }
            else
            {
                return new Polyline() { Closed = true };
            }
        }

        private Ellipse CreateEllipse(SvgEllipse sgvEllipse)
        {
            if (sgvEllipse.Transforms == null || sgvEllipse.Transforms.Count == 0)
            {
                var center = new Point3d(sgvEllipse.CenterX.Value, sgvEllipse.CenterY.Value, 0.0);
                var xRadius = sgvEllipse.RadiusX.Value;
                var yRadius = sgvEllipse.RadiusY.Value;
                var radiusRatio = yRadius / xRadius;
                return new Ellipse(center, Vector3d.ZAxis, new Vector3d(xRadius, 0, 0), radiusRatio, 0.0, 2 * Math.PI);
            }
            else
            {
                var xRadius = sgvEllipse.RadiusX.Value;
                var yRadius = sgvEllipse.RadiusY.Value;
                var radiusRatio = yRadius / xRadius;
                var ellipse = new Ellipse(Point3d.Origin, Vector3d.ZAxis,
                    new Vector3d(xRadius, 0, 0), radiusRatio, 0.0, 2 * Math.PI);
                var mt = ToMatrix3d(sgvEllipse.Transforms);
                ellipse.TransformBy(mt);
                return ellipse;
            }
        }

        private Polyline CreateRectangle(SvgRectangle svgRect)
        {
            if (svgRect.Transforms == null || svgRect.Transforms.Count == 0)
            {
                var pt1 = new Point2d(svgRect.Bounds.Left, svgRect.Bounds.Top);
                var pt2 = new Point2d(svgRect.Bounds.Right, svgRect.Bounds.Top);
                var pt3 = new Point2d(svgRect.Bounds.Right, svgRect.Bounds.Bottom);
                var pt4 = new Point2d(svgRect.Bounds.Left, svgRect.Bounds.Bottom);
                var pts = new Point2dCollection() { pt1, pt2, pt3, pt4 };
                return ThDrawTool.CreatePolyline(pts);
            }
            else
            {
                var pt1 = new Point2d(0, 0);
                var pt2 = new Point2d(svgRect.Width, 0);
                var pt3 = new Point2d(svgRect.Width, svgRect.Height);
                var pt4 = new Point2d(0, svgRect.Height);
                var pts = new Point2dCollection() { pt1, pt2, pt3, pt4 };
                var poly = ThDrawTool.CreatePolyline(pts);
                var mt = ToMatrix3d(svgRect.Transforms);
                poly.TransformBy(mt);
                return poly;
            }
        }

        private DBText CreateDBText(SvgText svgText)
        {
            if (svgText.Transforms == null || svgText.Transforms.Count == 0)
            {
                // 文字变换矩阵                
                var position = new Point3d(svgText.X[0].Value, svgText.Y[0].Value, 0);
                var mt = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(position));
                // 创建文字
                var dbText = new DBText();
                dbText.TextString = svgText.Text;
                dbText.Height = 5;
                dbText.WidthFactor = 1.0;
                dbText.Position = Point3d.Origin;
                dbText.HorizontalMode = TextHorizontalMode.TextMid;
                dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                dbText.AlignmentPoint = dbText.Position;
                dbText.TransformBy(mt);
                return dbText;
            }
            else
            {
                var mt1 = ToMatrix3dWithoutDisplacement(svgText.Transforms);
                var position = new Point3d(svgText.X[0].Value, svgText.Y[0].Value, 0);
                var mt2 = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(position));
                // 先在原点创建文字
                var dbText = new DBText();
                dbText.TextString = svgText.Text;
                dbText.Height = 5;
                dbText.WidthFactor = 1.0;
                dbText.Position = Point3d.Origin;
                dbText.HorizontalMode = TextHorizontalMode.TextMid;
                dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                dbText.AlignmentPoint = dbText.Position;
                dbText.TransformBy(mt1);
                dbText.TransformBy(mt2);
                return dbText;
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

        private List<Matrix> GetMatrixs(SvgTransformCollection transforms)
        {
            return transforms.OfType<SvgTransform>().Select(o => o.Matrix).ToList();
        }

        private Matrix3d ToMatrix3d(SvgTransformCollection svgTransforms)
        {
            return ToMatrix3d(Multiply(GetMatrixs(svgTransforms)));
        }
        private Matrix3d ToMatrix3d(Matrix matrix)
        {
            var xVec = new Vector3d(matrix.Elements[0], matrix.Elements[1], 0);
            var rotatAng = xVec.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis.Negate());
            var datas = new double[]
            {
                Math.Cos(rotatAng), Math.Sin(rotatAng), 0, 0,
                -Math.Sin(rotatAng), Math.Cos(rotatAng), 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
            };
            var mt1 = new Matrix3d(datas);
            //var mt2 = Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin);
            var position = new Point3d(matrix.Elements[4], matrix.Elements[5], 0);
            var mt2 = Matrix3d.Displacement(position - Point3d.Origin);
            return mt1.PreMultiplyBy(mt2);
        }
        private Matrix3d ToMatrix3dWithoutDisplacement(SvgTransformCollection svgTransforms)
        {
            return ToMatrix3dWithoutDisplacement(Multiply(GetMatrixs(svgTransforms)));
        }
        private Matrix3d ToMatrix3dWithoutDisplacement(Matrix matrix)
        {
            var xVec = new Vector3d(matrix.Elements[0], matrix.Elements[1], 0);
            var rotatAng = xVec.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis.Negate());
            var datas = new double[]
            {
                Math.Cos(rotatAng), Math.Sin(rotatAng), 0, 0,
                -Math.Sin(rotatAng), Math.Cos(rotatAng), 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
            };
            return new Matrix3d(datas);
        }

        private Matrix Multiply(List<Matrix> matrixs)
        {
            if (matrixs.Count > 0)
            {
                var first = matrixs[0];
                for (int i = 1; i < matrixs.Count; i++)
                {
                    first.Multiply(matrixs[i]);
                }
                return first;
            }
            else
            {
                return new Matrix(1, 0, 0, 1, 0, 0);
            }
        }

        private ThGeometry CreateThGeometry(Entity boundary, Dictionary<string, object> properties)
        {
            return new ThGeometry()
            {
                Boundary = boundary,
                Properties = properties,
            };
        }

        private List<Curve> ParseSvgPath(SvgPath svgPath)
        {
            //https://www.jianshu.com/p/c819ae16d29b
            //注：大写的字母是绝对坐标，小写的字母是相对坐标
            var curves = new List<Curve>();
            for (int i = 0; i < svgPath.PathData.Count; i++)
            {
                var currentSegment = svgPath.PathData[i];
                var currentString = currentSegment.ToString();
                if (currentString[0] == 'M' || currentString[0] == 'm') // 一段曲线的起点
                {
                    Polyline polyline = new Polyline() { Closed = false };
                    int index = 0;
                    if (currentString[0] == 'M')
                    {
                        polyline.AddVertexAt(index++, ToPoint2d(currentSegment.End), 0, 0, 0);
                    }
                    else
                    {
                        if (curves.Count > 0)
                        {
                            var preStartPt = curves[curves.Count - 1].StartPoint;
                            var currentStart = new Point2d(preStartPt.X + currentSegment.End.X, preStartPt.Y + currentSegment.End.Y);
                            polyline.AddVertexAt(index++, currentStart, 0, 0, 0);
                        }
                        else
                        {
                            polyline.AddVertexAt(index++, ToPoint2d(currentSegment.End), 0, 0, 0);
                        }
                    }
                    var j = i + 1;
                    for (; j < svgPath.PathData.Count; j++)
                    {
                        var nextSegment = svgPath.PathData[j];
                        var nextString = nextSegment.ToString();
                        if (nextString[0] == 'M' || nextString[0] == 'm')
                        {
                            break;
                        }
                        else if (nextString[0] == 'Z')
                        {
                            //首尾要闭合(closepath)
                            polyline.Closed = true;
                            break;
                        }
                        else if (nextString[0] == 'z')
                        {
                            //首尾要闭合(closepath)
                            polyline.Closed = true;
                            break;
                        }
                        else if (nextString[0] == 'L')
                        {
                            //直线
                            polyline.AddVertexAt(index++, ToPoint2d(nextSegment.End), 0, 0, 0);
                        }
                        else if (nextString[0] == 'l')
                        {
                            var lastPt = polyline.EndPoint.ToPoint2D();
                            var newPt = new Point2d(lastPt.X + nextSegment.End.X, lastPt.Y + nextSegment.End.Y);
                            polyline.AddVertexAt(index++, newPt, 0, 0, 0);
                        }
                        else if (nextString[0] == 'H')
                        {
                            //水平相加
                            var lastPt = polyline.EndPoint.ToPoint2D();
                            var newPt = new Point2d(nextSegment.End.X, lastPt.Y);
                            polyline.AddVertexAt(index++, newPt, 0, 0, 0);
                        }
                        else if (nextString[0] == 'h')
                        {
                            //水平相加       
                            var lastPt = polyline.EndPoint.ToPoint2D();
                            var newPt = new Point2d(lastPt.X + nextSegment.End.X, lastPt.Y);
                            polyline.AddVertexAt(index++, newPt, 0, 0, 0);
                        }
                        else if (nextString[0] == 'V')
                        {
                            //竖相加
                            var lastPt = polyline.EndPoint.ToPoint2D();
                            var newPt = new Point2d(lastPt.X, nextSegment.End.Y);
                            polyline.AddVertexAt(index++, newPt, 0, 0, 0);
                        }
                        else if (nextString[0] == 'v')
                        {
                            //竖相加
                            var lastPt = polyline.EndPoint.ToPoint2D();
                            var newPt = new Point2d(lastPt.X, lastPt.Y + nextSegment.End.Y);
                            polyline.AddVertexAt(index++, newPt, 0, 0, 0);
                        }
                        else if (nextString[0] == 'A' || nextString[0] == 'a')
                        {
                            //椭圆弧
                            throw new NotImplementedException();
                        }
                        else if (nextString[0] == 'C' || nextString[0] == 'S' ||
                            nextString[0] == 'Q' || nextString[0] == 'T')
                        {
                            // C->三次贝塞尔曲线,S->简写的贝塞尔曲线命令,Q->二次贝塞尔曲线
                            // T->smooth quadratic Belzier
                            throw new NotSupportedException();
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    i = j - 1;
                    if (polyline.NumberOfVertices > 1)
                    {
                        curves.Add(polyline);
                    }
                }
            }
            return curves;
        }
        private Point2d ToPoint2d(PointF pointF)
        {
            return new Point2d(pointF.X, pointF.Y);
        }
        private ThFloorInfo ParseFloorInfo(SvgCustomAttributeCollection svgCustomAttributes)
        {
            if(svgCustomAttributes == null || svgCustomAttributes.Count==0)
            {
                return null;
            }
            var floorInfo = new ThFloorInfo();
            bool isFloorName=false, isFloorNo = false, isStdFlrNo = false,
                isBottomElevation = false, isElevation = false;
            foreach (var item in svgCustomAttributes)
            {
                switch(item.Key.ToUpper())
                {
                    case "FLOORNAME":
                        isFloorName = true;
                        floorInfo.FloorName = item.Value;
                        break;
                    case "FLOORNO":
                        isFloorNo = true;
                        floorInfo.FloorNo = item.Value;
                        break;
                    case "STDFLRNO":
                        isStdFlrNo = true;
                        floorInfo.StdFlrNo = item.Value;
                        break;
                    case "BOTTOM_ELEVATION":
                        isBottomElevation = true;
                        floorInfo.Bottom_elevation = item.Value;
                        break;
                    case "ELEVATION":
                        isElevation = true;
                        floorInfo.Elevation = item.Value;
                        break;
                }
            }
            if(isFloorName && isFloorNo && isStdFlrNo && isBottomElevation && isElevation)
            {
                return floorInfo;
            }
            else
            {
                return null;
            }
        }
    }
}
