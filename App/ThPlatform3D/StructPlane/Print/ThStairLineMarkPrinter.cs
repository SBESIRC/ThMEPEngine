using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThPlatform3D.Common;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.StructPlane.Service;
using Linq2Acad;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThStairLineMarkPrinter
    {
        // 文字基点距离线的垂直距离
        private const double TextPosDistanceToLine = 85.0;

        public static ObjectIdCollection Print(AcadDatabase acadDb, Line line , PrintConfig lineConfig, AnnotationPrintConfig textConfig)
        {
            // 打印楼梯对角线
            var results = new ObjectIdCollection();
            results.Add(line.Print(acadDb, lineConfig));

            // 原点创建文字
            var mark = CreateText("楼梯详图");
            var textH = mark.GeometricExtents.MaxPoint.Y - mark.GeometricExtents.MinPoint.Y;

            // 旋转文字
            var textRotation = ThAdjustDbTextRotationService.GetRotation(line.StartPoint.GetVectorTo(line.EndPoint));
            var mt1 = Matrix3d.Rotation(textRotation, Vector3d.ZAxis, Point3d.Origin);
            mark.TransformBy(mt1);

            // 移动文字
            var oldCenter = mark.GetCenterPoint();
            var midPt = line.StartPoint.GetMidPt(line.EndPoint);
            var perpendVec = line.StartPoint.GetVectorTo(line.EndPoint).GetPerpendicularVector().GetNormal();
            var newCenter = midPt;
            if (perpendVec.DotProduct(Vector3d.YAxis) > 0.0)
            {
                newCenter = midPt + perpendVec.MultiplyBy(textH / 2.0 + TextPosDistanceToLine);
            }
            else
            {
                newCenter = midPt - perpendVec.MultiplyBy(textH / 2.0 + TextPosDistanceToLine);
            }
            var mt2 = Matrix3d.Displacement(oldCenter.GetVectorTo(newCenter));
            mark.TransformBy(mt2);

            // 打印
            results.Add(mark.Print(acadDb, textConfig));
            acadDb.Element<DBText>(mark.ObjectId, true);
            return results;
        }

        private static DBText CreateText(string content,double height =5.0)
        {
            // 原点创建文字
            var dbText = new DBText();
            dbText.TextString = content;
            dbText.Height = height;
            dbText.WidthFactor = 1.0;
            dbText.Position = Point3d.Origin;
            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            dbText.VerticalMode = TextVerticalMode.TextBottom;
            return dbText;
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
