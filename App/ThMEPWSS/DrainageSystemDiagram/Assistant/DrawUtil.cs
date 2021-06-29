using System;
using System.Collections.Generic;
using Linq2Acad;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class DrawUtils
    {
        private static List<ObjectId> DrawProfile(List<Entity> curves, string LayerName, short colorIndex, int lineWeightNum)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {

                Color color = Color.FromColorIndex(ColorMethod.ByColor, colorIndex);

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
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static ObjectId CreateLayer(string aimLayer, Color color, bool IsPlottable = false)
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
                //else
                //{
                //    if (!layerRecord.Color.Equals(color))
                //    {
                //        layerRecord.UpgradeOpen();
                //        layerRecord.Color = color;
                //        layerRecord.IsPlottable = false;
                //        layerRecord.DowngradeOpen();
                //    }
                //    if (!layerRecord.LineWeight.Equals(lineWeight))
                //    {
                //        layerRecord.UpgradeOpen();
                //        layerRecord.LineWeight = lineWeight;
                //        layerRecord.IsPlottable = false;
                //        layerRecord.DowngradeOpen();
                //    }
                //}
            }

            return layerRecord.ObjectId;
        }

        private static List<ObjectId> DrawProfileDebug(List<Entity> curves, string LayerName, short colorIndex, int lineWeightNum)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(Point3d pt, string LayerName, short colorIndex = 3, int lineWeightNum = 25, int r = 200, string symbol = "C")
        {
            Entity clone = null;

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
            else if (symbol == "T")
            {
            
            }
            else
            {
                clone = new Circle(pt, new Vector3d(0, 0, 1), r);
            }

            DrawUtils.ShowGeometry(clone, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(Point3d pt, string s, string LayerName, short colorIndex = 3, int lineWeightNum = 25, double hight = 1000)
        {

            DBText text = new DBText();
            text.Position = pt;
            text.TextString = s;
            text.Rotation = 0;
            text.Height = hight;
            text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLEP5");

            DrawUtils.ShowGeometry(text, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(Entity geom, string LayerName, short colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.Add(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Line> geom, string LayerName, short colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.AddRange(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Polyline> geom, string LayerName, short colorIndex = 3, int lineWeightNum = 25)
        {
            var curves = new List<Entity>();
            curves.AddRange(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, colorIndex, lineWeightNum);
        }

        public static void ShowGeometry(List<Entity> geom, string LayerName, short colorIndex = 3, int lineWeightNum = 25)
        {
            DrawUtils.DrawProfileDebug(geom, LayerName, colorIndex, lineWeightNum);
        }

    }
}
