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
            }
        }

        /// <summary>
        /// 组显示数据
        /// </summary>
        /// <param name="inputProfileDatas"></param>
        public static void DrawGroup(List<PlaceInputProfileData> inputProfileDatas)
        {
            foreach (var singleProfileData in inputProfileDatas)
            {
                DrawSingleInputProfileData(singleProfileData);
            }
        }

        /// <summary>
        /// 创建组
        /// </summary>
        /// <param name="inputProfileData"></param>
        public static void DrawSingleInputProfileData(PlaceInputProfileData inputProfileData)
        {
            var mainBeam = inputProfileData.MainBeamOuterProfile;
            var secondBeams = inputProfileData.SecondBeamProfiles;

            var mainIds = DrawProfile(new List<Curve>() { mainBeam }, "mainBeam", Color.FromRgb(255, 0, 0));
            var secondBeamIds = DrawProfile(secondBeams.Polylines2Curves(), "secondBeams", Color.FromRgb(0, 255, 0));

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
