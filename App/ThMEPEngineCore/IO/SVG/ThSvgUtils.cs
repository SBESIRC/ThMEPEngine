using System;
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
    public static class ThSvgUtils
    {
        public static ThGeometry CreateThGeometry(this Entity boundary, Dictionary<string, object> properties)
        {
            return ThGeometry.Create(boundary, properties);
        }
        public static ThFloorInfo ParseFloorInfo(this SvgCustomAttributeCollection svgCustomAttributes)
        {
            if (svgCustomAttributes == null || svgCustomAttributes.Count == 0)
            {
                return null;
            }
            var floorInfo = new ThFloorInfo();
            bool hasFLOORNAME = false, hasFLOORNO = false, hasSTDFLRNO=false, hasBOTTOMELEVATION=false, hasELEVATION=false;
            foreach (var item in svgCustomAttributes)
            {
                switch (item.Key.ToUpper())
                {
                    case "FLOORNAME":
                        hasFLOORNAME = true;
                        floorInfo.FloorName = item.Value;
                        break;
                    case "FLOORNO":
                        hasFLOORNO = true;
                        floorInfo.FloorNo = item.Value;
                        break;
                    case "STDFLRNO":
                        hasSTDFLRNO = true;
                        floorInfo.StdFlrNo = item.Value;
                        break;
                    case "BOTTOM_ELEVATION":
                        hasBOTTOMELEVATION = true;
                        floorInfo.Bottom_elevation = item.Value;
                        break;
                    case "ELEVATION": // 存放的是楼层高度
                        hasELEVATION = true;
                        floorInfo.Height = item.Value;
                        break;
                    case "DESCRIPTION": // 存放的是楼层高度
                        floorInfo.Description = item.Value;
                        break;
                }
            }
            if(hasFLOORNAME && hasFLOORNO && hasSTDFLRNO && hasBOTTOMELEVATION && hasELEVATION)
            {
                return floorInfo;
            }
            else
            {
                return null;
            }
        }
        
        public static ThComponentInfo ParseComponentInfo(this SvgCustomAttributeCollection svgCustomAttributes)
        {
            if (svgCustomAttributes == null || svgCustomAttributes.Count == 0)
            {
                return null;
            }
            var properties = new Dictionary<string, string>();
            foreach (var item in svgCustomAttributes)
            {
                properties.Add(item.Key, item.Value);
            }
            return ParseComponentInfo(properties);
        }

        public static ThComponentInfo ParseComponentInfo(this Dictionary<string,string> properties)
        {
            var componentInfo = new ThComponentInfo();
            foreach(var item in properties)
            {
                switch (item.Key)
                {
                    case "thickness":
                        componentInfo.Thickness = item.Value;
                        break;
                    case "type":
                        componentInfo.Type = item.Value;
                        break;
                    case "start":
                        componentInfo.Start = item.Value.ToPoint3d();
                        break;
                    case "end":
                        componentInfo.End = item.Value.ToPoint3d();
                        break;
                    case "hole-width":
                        componentInfo.HoleWidth = item.Value;
                        break;
                    case "hole-height":
                        componentInfo.HoleHeight = item.Value;
                        break;
                    case "blockname":
                        componentInfo.BlockName = item.Value;
                        break;
                    case "centerangle":
                        componentInfo.CenterAngle = item.Value;
                        break;
                    case "rotateangle":
                        componentInfo.Rotation = item.Value;
                        break;
                    case "basepoint":
                        componentInfo.BasePoint = item.Value.ToPoint3d();
                        break;
                    case "opendirection":
                        componentInfo.OpenDirection = item.Value;
                        break;
                    case "matrix":
                        // 暂时不用
                        componentInfo.Matrix = item.Value;
                        break;
                    case "center-point":
                        componentInfo.CenterPoint = item.Value.ToPoint3d();
                        break;
                }
            }            
            return componentInfo;
        }
        public static DrawingType GetDrawingType(this Dictionary<string, string> properties)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.DrawingTypePropertyName))
            {
                switch (properties[ThSvgPropertyNameManager.DrawingTypePropertyName])
                {
                    case "plan":
                        return DrawingType.Plan;
                    case "elevation":
                        return DrawingType.Elevation;
                    case "section":
                        return DrawingType.Section;
                    default:
                        return DrawingType.Unknown;
                }
            }
            else
            {
                return DrawingType.Unknown;
            }
        }
        public static string GetDrawingType(this DrawingType drawingType)
        {
            switch (drawingType)
            {
                case DrawingType.Structure:
                    return "structure"; //结构图
                case DrawingType.Plan:
                    return "plane";     //平面图
                case DrawingType.Elevation:
                    return "elevation"; //立面图
                case DrawingType.Section:
                    return "section";   //剖面图
                default:
                    return "";
            }
        }
        public static Line CreateLine(this SvgLine svgLine)
        {
            return new Line(new Point3d(svgLine.StartX.Value, svgLine.StartY.Value, 0.0),
                        new Point3d(svgLine.EndX.Value, svgLine.EndY.Value, 0.0));
        }
        public static Circle CreateCircle(this SvgCircle svgCircle)
        {
            return new Circle(new Point3d(svgCircle.Center.X, svgCircle.Center.Y, 0), Vector3d.ZAxis, svgCircle.Radius.Value);
        }
        public static Polyline CreateRectangle(this SvgRectangle svgRect)
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
        public static List<Curve> ParseSvgPath(this SvgPath svgPath)
        {
            //https://www.jianshu.com/p/c819ae16d29b
            //注：大写的字母是绝对坐标，小写的字母是相对坐标
            var curves = new List<Curve>();
            if(svgPath.PathData==null)
            {
                return curves;
            }
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
        public static Polyline CreatePolyline(this SvgPolyline svgPolyline)
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

        public static Polyline CreatePolygon(this SvgPolygon svgPolygon)
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

        public static Ellipse CreateEllipse(this SvgEllipse sgvEllipse)
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
                var mt = sgvEllipse.Transforms.ToMatrix3d();
                ellipse.TransformBy(mt);
                return ellipse;
            }
        }
        public static DBText CreateDBText(this SvgText svgText)
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
                dbText.HorizontalMode = TextHorizontalMode.TextCenter;
                dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
                dbText.AlignmentPoint = dbText.Position;
                dbText.TransformBy(mt);
                return dbText;
            }
            else
            {
                var mt1 = svgText.Transforms.ToMatrix3dWithoutDisplacement();
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
        private static Point2d ToPoint2d(PointF pointF)
        {
            return new Point2d(pointF.X, pointF.Y);
        }
        private static Matrix3d ToMatrix3d(this SvgTransformCollection svgTransforms)
        {
            return ToMatrix3d(Multiply(GetMatrixs(svgTransforms)));
        }
        private static Matrix Multiply(List<Matrix> matrixs)
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
        private static List<Matrix> GetMatrixs(SvgTransformCollection transforms)
        {
            return transforms.OfType<SvgTransform>().Select(o => o.Matrix).ToList();
        }
        private static Matrix3d ToMatrix3d(this Matrix matrix)
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
        public static Matrix3d ToMatrix3dWithoutDisplacement(this SvgTransformCollection svgTransforms)
        {
            return ToMatrix3dWithoutDisplacement(Multiply(GetMatrixs(svgTransforms)));
        }
        private static Matrix3d ToMatrix3dWithoutDisplacement(this Matrix matrix)
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
        public static Point3d? ToPoint3d(this string point)
        {
            double x, y, z;
            var values = point.Split(',');
            if (point.IndexOf(",") > 0)
            {
                values = point.Split(',');
            }
            else
            {
                values = point.Split(' ');
            }
            if (values.Length == 2)
            {
                if (double.TryParse(values[0].Trim(), out x) && double.TryParse(values[1].Trim(), out y))
                {
                    return new Point3d(x, y, 0);
                }
            }
            if (values.Length == 3)
            {
                if (double.TryParse(values[0].Trim(), out x) &&
                    double.TryParse(values[1].Trim(), out y) &&
                    double.TryParse(values[2].Trim(), out z))
                {
                    return new Point3d(x, y, z);
                }
            }
            return null;
        }
        public static Vector3d? ToVector3d(this string point)
        {
            double x, y, z;
            var values = point.Split(',');
            if (values.Length == 2)
            {
                if (double.TryParse(values[0], out x) && double.TryParse(values[1], out y))
                {
                    return new Vector3d(x, y, 0);
                }
            }
            if (values.Length == 3)
            {
                if (double.TryParse(values[0], out x) &&
                    double.TryParse(values[1], out y) &&
                    double.TryParse(values[2], out z))
                {
                    return new Vector3d(x, y, z);
                }
            }
            return null;
        }
    }
    public enum DrawingType
    {
        /// <summary>
        /// 平面图
        /// </summary>
        Plan,
        /// <summary>
        /// 立面图
        /// </summary>
        Elevation,
        /// <summary>
        /// 剖面图
        /// </summary>
        Section,
        /// <summary>
        /// 结构图
        /// </summary>
        Structure,        
        /// <summary>
        /// 未知
        /// </summary>
        Unknown,
    }
}
