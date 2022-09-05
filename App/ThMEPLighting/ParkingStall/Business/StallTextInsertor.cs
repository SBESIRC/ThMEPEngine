using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Common;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.ParkingStall.Business
{
    class StallTextInsertor
    {
        private List<LightPlaceInfo> m_LightPlaceInfos;
        private ThMEPOriginTransformer m_OriginTransformer;
        private Parkingillumination m_Parkingillumination;
        public StallTextInsertor(List<LightPlaceInfo> lightPlaceInfos, Parkingillumination illumination, ThMEPOriginTransformer originTransformer)
        {
            m_LightPlaceInfos = lightPlaceInfos;
            m_OriginTransformer = originTransformer;
            m_Parkingillumination = illumination;
        }

        /// <summary>
        /// 插入块
        /// </summary>
        /// <param name="insertPts"></param>
        /// <param name="sensorType"></param>
        public static void MakeTextInsert(List<LightPlaceInfo> lightPlaceInfos, Parkingillumination illumination, ThMEPOriginTransformer originTransformer = null)
        {
            var blockInsertor = new StallTextInsertor(lightPlaceInfos, illumination, originTransformer);

            blockInsertor.Do();
        }

        public void Do()
        {
            double scaleNum = ThParkingStallService.Instance.BlockScale;
            List<DBText> showText = new List<DBText>();
            List<Polyline> showPLines = new List<Polyline>();
            double height = 3.5 * scaleNum;
            foreach (var item in m_LightPlaceInfos) 
            {
                var area = item.BigGroupInfo.BigGroupPoly.Area;
                area = area / (1000.0 * 1000.0);
                var lightCount = item.InsertLightPosisions.Count();
                var lpd = GetLPDValue(lightCount, area);
                var realEav = GetRealIllumination(lightCount, area);
                string showMsg = string.Format("平均照度={0} lx，LPD={1} W/平米", realEav.ToString("N1"), lpd.ToString("N1"));

                var position = item.Position;
                var allCurves = new DBObjectCollection();
                item.BigGroupInfo.BigGroupPoly.Explode(allCurves);
                var allLines = allCurves.OfType<Line>().ToList().OrderByDescending(c=>c.GetLength()).ToList();
                //这里是矩形，计算两个长边看用那个边计算文字合适
                //文字尽量取【-90°,90°】
                var firstLine = allLines[0];
                var secondLine = allLines[1];
                var firstPrj = position.PointToLine(firstLine);
                var secondPrj = position.PointToLine(secondLine);
                var firstDir = (position - firstPrj).GetNormal();
                var secondDir = (position - secondPrj).GetNormal();
                var longLine = item.BigGroupInfo.BigGroupLongLine;
                var firstDirDot = firstDir.DotProduct(Vector3d.YAxis);
                var secondDirDot = secondDir.DotProduct(Vector3d.YAxis);
                if (firstDirDot > 0)
                {
                    longLine = firstLine;
                }
                else
                {
                    longLine = secondLine;
                }
                var prjPoint = ThPointVectorUtil.PointToLine(position, longLine);
                var innerDir = (position - prjPoint).GetNormal();
                var longDir = longLine.LineDirection();
                if (longDir.CrossProduct(innerDir).Z < 0)
                {
                    longLine = new Line(longLine.EndPoint, longLine.EndPoint);
                    longDir = longDir.Negate();
                }
                position = longLine.StartPoint;
                var angle = Vector3d.XAxis.GetAngleTo(longDir, Vector3d.ZAxis);
                var addPLine = item.BigGroupInfo.BigGroupPoly.Clone() as Polyline;
                if (null != m_OriginTransformer)
                {
                    position = m_OriginTransformer.Reset(position);
                    m_OriginTransformer.Reset(addPLine);
                }

                showPLines.Add(addPLine);
                //计算位置 文字在左下角
                DBText infotext = new DBText()
                {
                    TextString = showMsg,
                    Height = height,
                    WidthFactor = 0.7,
                    HorizontalMode = TextHorizontalMode.TextLeft,
                    Oblique = 0,
                    Position = position,
                    Rotation = angle,
                };
                showText.Add(infotext);
            }

            //载入文字样式
            string layerName = ParkingStallCommon.PARK_LIGHT_RESULT_LAYER;
            string textStyleName = ParkingStallCommon.PARK_LIGHT_RESULT_TEXTSTYPENAME;
            AddLayerLoadStyle(layerName, textStyleName);

            using (var db = AcadDatabase.Active())
            {
                var addTextIds = new List<ObjectId>();
                foreach (var item in showText) 
                {
                    item.Layer = layerName;
                    item.ColorIndex = 7;
                    var id = db.ModelSpace.Add(item);
                    if (null == id || id == ObjectId.Null)
                        continue;
                    addTextIds.Add(id);
                }
                var styleId = DbHelper.GetTextStyleId(textStyleName);
                if (null != styleId && styleId != ObjectId.Null)
                    showText.ForEach(c => c.TextStyleId = styleId);
                foreach (var item in showPLines) 
                {
                    item.Layer = layerName;
                    db.ModelSpace.Add(item);
                }
            }
        }
        private double GetLPDValue(int lightCount,double area) 
        {
            // LPD计算公式：LPD = N * P / A   N——灯具数，正整数； P——灯具功率； A——工作面面积，m²；
            var temp = lightCount * m_Parkingillumination.LightRatedPower / area;
            return temp;
        }
        private double GetRealIllumination(int lightCount, double area) 
        {
            //var eav = N*lm*U*K/A
            var temp = lightCount * m_Parkingillumination.LightRatedIllumination*m_Parkingillumination.UtilizationCoefficient* m_Parkingillumination.MaintenanceFactor / area;
            return temp;
        }
        private void AddLayerLoadStyle(string layerName,string textStyleName)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (AcadDatabase modelDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
                {
                    acadDatabase.TextStyles.Import(modelDb.TextStyles.ElementOrDefault(textStyleName), false);
                }
                LayerTableRecord layerRecord = null;
                foreach (var layer in acadDatabase.Layers)
                {
                    if (layer.Name.Equals(layerName))
                    {
                        layerRecord = acadDatabase.Layers.Element(layerName);
                        break;
                    }
                }
                ObjectId lineTypeId = ObjectId.Null;
                foreach (var item in acadDatabase.Linetypes)
                {
                    if (item.Name == ParkingStallCommon.PARK_LIGHT_RESULT_LAYERLINETYPENAME)
                    {
                        lineTypeId = item.ObjectId;
                        break;
                    }
                }
                if (layerRecord == null)
                {
                    layerRecord = acadDatabase.Layers.Create(layerName);
                    layerRecord.Color = Color.FromColorIndex(ColorMethod.ByLayer, 1);
                    if (lineTypeId != ObjectId.Null)
                        layerRecord.LinetypeObjectId = lineTypeId;
                    layerRecord.IsPlottable = false;
                    layerRecord.Transparency = new Transparency((byte)255);
                    //layerRecord.PlotStyleName = "Color_9";
                    layerRecord.ViewportVisibilityDefault = false;
                    layerRecord.Description = "车位照度检查";
                }
                DbHelper.EnsureLayerOn(layerName);
            }
        }
    }
}
