using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetARX;
using ThMEPElectrical.Model;
using Autodesk.AutoCAD.ApplicationServices;
using ThCADExtension;

namespace ThMEPElectrical.Assistant
{
    public static class DrawUtils
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

        public static List<ObjectId> DrawProfile(List<Entity> entities, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (entities == null || entities.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var entity in entities)
                {
                    var clone = entity.Clone() as Entity;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }

        public static List<ObjectId> DrawProfileDebug(List<Curve> curves, string LayerName, Color color = null)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch && !ThMEPCommon.PROTECTAREA_LAYER_NAME.Equals(LayerName))
                return new List<ObjectId>();

            return DrawProfile(curves, LayerName, color);
        }

        public static List<ObjectId> DrawEntitiesDebug(List<Entity> entities, string LayerName, Color color = null)
        {
            // 调试按钮关闭且图层不是保护半径有效图层
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch && !ThMEPCommon.PROTECTAREA_LAYER_NAME.Equals(LayerName))
                return new List<ObjectId>();

            return DrawProfile(entities, LayerName, color);
        }

        /// <summary>
        /// 创建新的图层
        /// </summary>
        /// <param name="allLayers"></param>
        /// <param name="aimLayer"></param>
        public static void CreateLayer(string aimLayer, Color color)
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
        }

        /// <summary>
        /// 组显示数据
        /// </summary>
        /// <param name="inputProfileDatas"></param>
        public static void DrawGroup(List<PlaceInputProfileData> inputProfileDatas)
        {
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return;

            foreach (var singleProfileData in inputProfileDatas)
            {
                DrawSingleInputProfileData(singleProfileData);
            }
        }

        public static void DrawGroupPath(List<SplitBeamPath> paths)
        {
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return;

            foreach (var path in paths)
            {
                DrawSinglePath(path);
            }
        }

        public static void DrawSinglePath(SplitBeamPath splitBeamPath)
        {
            var profilePaths = new List<Curve>();
            splitBeamPath.pathNodes.ForEach(beam => profilePaths.Add(beam.Profile));

            var pathIds = DrawProfileDebug(profilePaths, "path", Color.FromRgb(255, 0, 0));
            var totalIds = new ObjectIdList();
            totalIds.AddRange(pathIds);
            var groupName = totalIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }

        public static void DrawDetectionRegion(List<DetectionRegion> detectionRegions)
        {
            foreach (var detectionRegion in detectionRegions)
            {
                var curves = new List<Curve>();
                curves.Add(detectionRegion.DetectionProfile);
                curves.AddRange(detectionRegion.DetectionInnerProfiles);
                detectionRegion.secondBeams.ForEach(e => curves.Add(e.Profile));

                DrawUtils.DrawProfileDebug(curves, "detectionRegions");
            }
        }

        public static void DrawDetectionPolygon(List<DetectionPolygon> detectionPolygons)
        {
            foreach (var detectionPolygon in detectionPolygons)
            {
                var entities = new List<Entity>();
                if (detectionPolygon.Holes.Count > 0)
                {
                    entities.Add(ThMPolygonTool.CreateMPolygon(detectionPolygon.Shell, detectionPolygon.Holes.Polylines2Curves()));
                }
                else
                {
                    entities.Add(detectionPolygon.Shell);
                }

                DrawUtils.DrawEntitiesDebug(entities, "detectionPolygons");
            }
        }

        public static void DrawSecondBeam2Curves(List<SecondBeamProfileInfo> secondBeamProfileInfos, string secondName)
        {
            var curves = new List<Curve>();
            foreach (var secondBeam in secondBeamProfileInfos)
            {
                curves.Add(secondBeam.Profile);
            }

            DrawUtils.DrawProfile(curves, secondName);
        }

        /// <summary>
        /// 创建组
        /// </summary>
        /// <param name="inputProfileData"></param>
        public static void DrawSingleInputProfileData(PlaceInputProfileData inputProfileData)
        {
            var mainBeam = inputProfileData.MainBeamOuterProfile;
            var secondBeams = inputProfileData.SecondBeamProfiles;

            var mainIds = DrawProfileDebug(new List<Curve>() { mainBeam }, "mainBeam", Color.FromRgb(255, 0, 0));
            var secondBeamIds = DrawProfileDebug(secondBeams.Polylines2Curves(), "secondBeams", Color.FromRgb(0, 255, 0));

            var totalIds = new ObjectIdList();
            totalIds.AddRange(mainIds);
            totalIds.AddRange(secondBeamIds);

            var groupName = mainIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }
    }
}
