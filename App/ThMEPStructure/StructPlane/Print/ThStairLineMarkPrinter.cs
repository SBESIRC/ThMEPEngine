using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPStructure.Common;
using ThMEPStructure.Model.Printer;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane.Print
{
    internal class ThStairLineMarkPrinter
    {
        // 文字基点距离线的垂直距离
        private double TextPosDistanceToLine = 85.0;
        private double AngleTolerance = 1.0;
        private PrintConfig LineConfig { get; set; }
        private AnnotationPrintConfig TextConfig { get; set; }
        public ThStairLineMarkPrinter(PrintConfig lineConfig, AnnotationPrintConfig textConfig)
        {
            LineConfig = lineConfig;
            TextConfig= textConfig;
        }

        public ObjectIdCollection Print(Database db, Line line)
        {
            var results = new ObjectIdCollection();
            results.Add(line.Print(db, LineConfig));

            var mark = CreateMark(line);
            results.Add(mark.Print(db, TextConfig));
            return results;
        }

        private DBText CreateMark(Line line)
        {
            // 文字移动方向
            var lineDir = line.StartPoint.GetVectorTo(line.EndPoint);
            var textMovDir = GetTextMoveDirection(lineDir);

            // 文字摆放角度
            var textRotation = GetTextRotation(line.Angle);

            // 文字中心
            var midPt = line.StartPoint.GetMidPt(line.EndPoint);
            var offsetDis = TextPosDistanceToLine + TextConfig.Height / 2.0;
            var textCenter = midPt + textMovDir.GetNormal().MultiplyBy(offsetDis);

            var text = CreateText("楼梯详图");
            text.Rotation = textRotation;
            text.Position = textCenter;
            return text;
        }

        private DBText CreateText(string content,double height =5.0)
        {
            // 原点创建文字
            var dbText = new DBText();
            dbText.TextString = content;
            dbText.Height = 5;
            dbText.WidthFactor = 1.0;
            dbText.Position = Point3d.Origin;
            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            dbText.VerticalMode = TextVerticalMode.TextVerticalMid;
            dbText.AlignmentPoint = dbText.Position;
            return dbText;
        }

        private Vector3d GetTextMoveDirection(Vector3d lineDir)
        {
            var perpendVec = lineDir.GetPerpendicularVector();
            // 文字移动方向
            var textMovDir = Vector3d.YAxis;
            if (ThGeometryTool.IsParallelToEx(perpendVec, Vector3d.XAxis))
            {
                textMovDir = Vector3d.YAxis;
            }
            else if (perpendVec.DotProduct(Vector3d.YAxis) > 0.0)
            {
                textMovDir = perpendVec;
            }
            else
            {
                textMovDir = perpendVec.Negate();
            }
            return textMovDir;
        }

        /// <summary>
        /// 获取文字的角度
        /// </summary>
        /// <param name="lineRad">弧度</param>
        /// <returns>弧度</returns>
        private double GetTextRotation(double lineRad)
        {
            // 文字角度
            double textAng = 0.0;
            double lineAng = lineRad.RadToAng() % 180.0;
            if (lineAng <= AngleTolerance || (180.0 - lineAng) <= AngleTolerance)
            {
                // 平行于X轴
                textAng = 0.0;
            }
            if (Math.Abs(90.0 - lineAng) <= AngleTolerance)
            {
                // 平行于Y轴
                textAng =90.0;
            }
            else if ((lineAng > 0 && lineAng < 90.0) || (lineAng > 180 && lineAng < 270.0))
            {
                //一、三象限
                textAng = lineAng; // 锐角
            }
            else
            {
                //二、四象限
                textAng = lineAng + Math.PI; // 钝角
            }
            return textAng.AngToRad();
        }

        public static PrintConfig GetLineConfig()
        {
            return new PrintConfig
            {
                LayerName = ThPrintLayerManager.StairSlabCornerLineLayerName,
            };
        }
        public static AnnotationPrintConfig GetTextConfig(string drawingScale)
        {
            var config = GetTextConfig();
            config.ScaleHeight(drawingScale);
            return config;
        }
        private static AnnotationPrintConfig GetTextConfig()
        {
            return new AnnotationPrintConfig
            {
                LayerName = ThPrintLayerManager.StairSlabCornerTextLayerName,
                TextStyleName = ThPrintStyleManager.THSTYLE3,
                Height =2.5,
                WidthFactor=0.7,
            };
        }
    }
}
