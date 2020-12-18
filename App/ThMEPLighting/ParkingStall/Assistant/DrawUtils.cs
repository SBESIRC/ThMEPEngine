using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Assistant
{
    public class DrawUtils
    {
        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var curve in curves)
                {
                    var clone = curve.Clone() as Curve;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }

        /// <summary>
        /// 组显示数据
        /// </summary>
        /// <param name="inputProfileDatas"></param>
        public static void DrawGroup(List<ParkingRelatedGroup> parkingRelatedGroups)
        {
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return;

            foreach (var parkingRelatedGroup in parkingRelatedGroups)
            {
                DrawSingleNearParks(parkingRelatedGroup);
            }
        }

        /// <summary>
        /// 创建组
        /// </summary>
        /// <param name="inputProfileData"></param>
        public static void DrawSingleNearParks(ParkingRelatedGroup parkingRelatedGroup)
        {
            var nearPolylines = parkingRelatedGroup.RelatedParks.Polylines2Curves();
            var nearPolylineIds = DrawProfileDebug(nearPolylines, "nearParks", Color.FromRgb(0, 255, 0));

            var totalIds = new ObjectIdList();
            totalIds.AddRange(nearPolylineIds);

            var groupName = totalIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static ObjectId CreateLayer(string aimLayer, Color color)
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
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = false;
                }
                else
                {
                    if (!layerRecord.Color.Equals(color))
                    {
                        layerRecord.UpgradeOpen();
                        layerRecord.Color = color;
                        layerRecord.IsPlottable = false;
                        layerRecord.DowngradeOpen();
                    }
                }
            }

            return layerRecord.ObjectId;
        }

        public static List<ObjectId> DrawProfileDebug(List<Curve> curves, string LayerName, Color color = null)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, color);
        }
    }
}
