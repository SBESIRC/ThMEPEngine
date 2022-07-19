using System;
using Linq2Acad;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Diagnostics
{
    /// <summary>
    /// debug模式（图纸参数userr2=1）绘图
    /// </summary>
    public class DrawUtils
    {
        private static List<ObjectId> DrawProfile(List<Entity> curves, string LayerName, int colorIndex, int lineWeightNum)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {

                Color color = Color.FromColorIndex(ColorMethod.ByColor, (short)colorIndex);

                CreateLayer(LayerName, color);

                var lineWeight = (LineWeight)lineWeightNum;

                foreach (var curve in curves)
                {
                    if (curve != null)
                    {
                        var clone = curve.Clone() as Entity;
                        clone.Layer = LayerName;
                        clone.Color = color;
                        clone.LineWeight = lineWeight;
                        objectIds.Add(db.ModelSpace.Add(clone));
                    }
                }
            }

            return objectIds;
        }

        /// <summary>
        /// 创建新的图层
        /// 不覆盖图层默认颜色，保证一个图层上可以画多个颜色
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static ObjectId CreateLayer(string aimLayer, Color color, bool IsPlottable = false, bool ReplaceLayerSetting = false)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.ToUpper().Equals(aimLayer.ToUpper()))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    if (color == null)
                    {
                        color = Color.FromRgb(255, 0, 0);
                    }
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = IsPlottable;
                }
                else
                {
                    if (!layerRecord.Color.Equals(color) && ReplaceLayerSetting == true)
                    {
                        layerRecord.UpgradeOpen();
                        layerRecord.Color = color;
                        layerRecord.IsPlottable = IsPlottable;
                        layerRecord.DowngradeOpen();
                    }
                }
            }

            return layerRecord.ObjectId;
        }

        /// <summary>
        /// 确认系统变量userr2处于debug mode
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="LayerName"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineWeightNum"></param>
        /// <returns></returns>
        private static List<ObjectId> DrawProfileDebug(List<Entity> curves, string LayerName, int colorIndex, int lineWeightNum)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, colorIndex, lineWeightNum);
        }

        /// <summary>
        /// 画一个点。C:画圆，S:方块，X:叉
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="LayerName"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineWeightNum"></param>
        /// <param name="r"></param>
        /// <param name="symbol"></param>
        public static void ShowGeometry(Point3d pt, string LayerName, int colorIndex = 3, int lineWeightNum = 25, int r = 200, string symbol = "C")
        {
            if (pt == null || pt == Point3d.Origin)
            {
                return;
            }

            Entity clone = null;
            Entity clone2 = null;

            if (symbol == "C")
            {
                clone = new Circle(pt, new Vector3d(0, 0, 1), r);
            }
            else if (symbol == "S")
            {
                var sq = new Polyline();
                sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y + r), 0, 0, 0);
                sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y + r), 0, 0, 0);
                sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y - r), 0, 0, 0);
                sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y - r), 0, 0, 0);
                sq.Closed = true;
                clone = sq;
            }
            else if (symbol == "X")
            {
                var x1 = new Line(new Point3d(pt.X - r, pt.Y + r, 0), new Point3d(pt.X + r, pt.Y - r, 0));
                var x2 = new Line(new Point3d(pt.X + r, pt.Y + r, 0), new Point3d(pt.X - r, pt.Y - r, 0));

                clone = x1;
                clone2 = x2;
            }
            else
            {
                clone = new Circle(pt, new Vector3d(0, 0, 1), r);
            }

            DrawUtils.ShowGeometry(clone, LayerName, colorIndex, lineWeightNum);
            if (clone2 != null)
            {
                DrawUtils.ShowGeometry(clone2, LayerName, colorIndex, lineWeightNum);
            }
        }

        /// <summary>
        /// 打印文字。hight：文字大小
        /// 支持多行文字。字符串中用\n换行
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="s"></param>
        /// <param name="LayerName"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineWeightNum"></param>
        /// <param name="hight"></param>
        public static void ShowGeometry(Point3d pt, string s, string LayerName, int colorIndex = 3, int lineWeightNum = 25, double hight = 1000)
        {
            if (pt == null || pt == Point3d.Origin)
            {
                return;
            }

            var text = new MText();
            text.Location = pt;
            text.Contents = s;
            text.Rotation = 0;
            text.Height = hight;
            text.TextHeight = hight;
            //text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");

            DrawUtils.ShowGeometry(text, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(Entity geom, string LayerName, int colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.Add(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Line> geom, string LayerName, int colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.AddRange(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Polyline> geom, string LayerName, int colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.AddRange(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Entity> geom, string LayerName, int colorIndex = 3, int lineWeightNum = 25)
        {
            DrawUtils.DrawProfileDebug(geom, LayerName, colorIndex, lineWeightNum);
        }

        /// <summary>
        /// 画一个带方向的点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <param name="LayerName"></param>
        /// <param name="colorIndex"></param>
        /// <param name="lineWeightNum"></param>
        /// <param name="l"></param>
        public static void ShowGeometry(Point3d pt, Vector3d dir, string LayerName, int colorIndex = 3, int lineWeightNum = 25, int l = 200)
        {
            if (pt == null || pt == Point3d.Origin)
            {
                return;
            }
            dir = dir.GetNormal();
            var ptE = pt + dir * l;

            var line = new Line(pt, ptE);

            var ptA1 = ptE + dir.RotateBy(150 * Math.PI / 180, -Vector3d.ZAxis) * l / 5;
            var Arrow1 = new Line(ptE, ptA1);

            var ptA2 = ptE + dir.RotateBy(150 * Math.PI / 180, Vector3d.ZAxis) * l / 5;
            var Arrow2 = new Line(ptE, ptA2);

            DrawUtils.ShowGeometry(line, LayerName, colorIndex, lineWeightNum);
            DrawUtils.ShowGeometry(Arrow1, LayerName, colorIndex, lineWeightNum);
            DrawUtils.ShowGeometry(Arrow2, LayerName, colorIndex, lineWeightNum);
        }
    }
}
