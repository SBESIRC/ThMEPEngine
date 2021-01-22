using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPLighting.EmgLight.Assistant
{
    public class DrawUtils
    {
        private static List<ObjectId> DrawProfile(List<Entity> curves, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                CreateLayer(LayerName);
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

        private static List<ObjectId> DrawProfile(Entity curve, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var objectIds = new List<ObjectId>();
            if (curve == null)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {

                CreateLayer(LayerName);

                var clone = curve.Clone() as Entity;
                clone.Layer = LayerName;
                clone.Color = color;
                clone.LineWeight = lineWeight;
                objectIds.Add(db.ModelSpace.Add(clone));
            }
            return objectIds;
        }

        private static List<ObjectId> DrawProfile(Point3d pt, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var objectIds = new List<ObjectId>();
            if (pt == null)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {

                CreateLayer(LayerName);

                var clone = new Circle(pt, new Vector3d(0, 0, 1), 200);
                clone.Layer = LayerName;
                clone.Color = color;
                clone.LineWeight = lineWeight;
                objectIds.Add(db.ModelSpace.Add(clone));
            }
            return objectIds;
        }

        private static List<ObjectId> DrawProfile(Point3d pt, string s, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var objectIds = new List<ObjectId>();
            if (pt == null)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {

                CreateLayer(LayerName);

                DBText text = new DBText();
                text.Position = pt;
                text.TextString = s;
                text.Rotation = 0;
                text.Height = 1000;
                text.Color = color;
                text.LineWeight = lineWeight;
                text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLEP5");

                text.Layer = LayerName;
                objectIds.Add(db.ModelSpace.Add(text));
            }
            return objectIds;
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        private static ObjectId CreateLayer(string aimLayer)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    //layerRecord.Color = color;
                    //layerRecord.LineWeight = lineWeight;
                    layerRecord.IsPlottable = false;
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

        private static List<ObjectId> DrawProfileDebug(List<Entity> curves, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, color, lineWeight);
        }

        private static List<ObjectId> DrawProfileDebug(Entity curves, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, color, lineWeight);
        }

        private static List<ObjectId> DrawProfileDebug(Point3d pt, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(pt, LayerName, color, lineWeight);
        }

        private static List<ObjectId> DrawProfileDebug(Point3d pt, string s, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(pt, s, LayerName, color, lineWeight);
        }

        public static void ShowGeometry(List<Line> geom, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var curves = new List<Entity>();
            geom.ForEach(e => curves.Add(e));
            DrawUtils.DrawProfileDebug(curves, LayerName,color,lineWeight );
        }
        public static void ShowGeometry(List<Polyline> geom, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var curves = new List<Entity>();
            geom.ForEach(e => curves.Add(e));
            DrawUtils.DrawProfileDebug(curves, LayerName, color, lineWeight);
        }
        public static void ShowGeometry(Polyline geom, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            var curves = new List<Entity>();
            curves.Add(geom);
            DrawUtils.DrawProfileDebug(curves, LayerName, color, lineWeight);
        }
        public static void ShowGeometry(Point3d pt, string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            DrawUtils.DrawProfileDebug(pt, LayerName, color, lineWeight);
        }
        public static void ShowGeometry(Point3d pt, string s,  string LayerName, Color color = null, LineWeight lineWeight = LineWeight.LineWeight025)
        {
            DrawUtils.DrawProfileDebug(pt,s, LayerName, color, lineWeight);
        }

    }
}
