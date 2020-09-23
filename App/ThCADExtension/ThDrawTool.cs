using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThCADExtension
{       
    public static class ThDrawTool
    {
        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    ThLayerTool.CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    ThLayerTool.CreateLayer(LayerName, color);

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
        public static void DrawGroup(List<DrawProfileData> inputProfileDatas)
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
        public static void DrawSingleInputProfileData(DrawProfileData inputProfileData)
        {
            var mainProfiles = inputProfileData.MainProfiles;
            var auxiliaryProfiles = inputProfileData.AuxiliaryProfiles;

            var mainIds = DrawProfile(mainProfiles.Polylines2Curves(), "MainProfile", inputProfileData.MainProfileColor);
            var secondBeamIds = DrawProfile(auxiliaryProfiles.Polylines2Curves(), "AuxiliaryProfile", inputProfileData.AuxiliaryColor);

            var totalIds = new ObjectIdList();
            totalIds.AddRange(mainIds);
            totalIds.AddRange(secondBeamIds);

            var groupName = mainIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }
        public static List<Curve> Polylines2Curves(this List<Polyline> srcPolylines)
        {
            if (srcPolylines == null || srcPolylines.Count == 0)
                return null;
            var curves = new List<Curve>();

            foreach (var polyline in srcPolylines)
            {
                curves.Add(polyline);
            }

            return curves;
        }
    }
    public class DrawProfileData
    {
        public List<Polyline> MainProfiles { get; private set; }

        // 包含次梁构成的轮廓，可能没有次梁轮廓
        public List<Polyline> AuxiliaryProfiles { get; private set; }

        public Color MainProfileColor { get; set; }
        public Color AuxiliaryColor { get; set; }

        public DrawProfileData(List<Polyline> polys)
        {
            MainProfiles = polys;
            AuxiliaryProfiles = new List<Polyline>();
        }

        public DrawProfileData(List<Polyline> polys, List<Polyline> auxiliaryProfiles)
        {
            MainProfiles = polys;
            AuxiliaryProfiles = auxiliaryProfiles;
        }
    }
}
