using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;
using ThMEPEngineCore.Model.Hvac;
using ThMEPHVAC.CAD;
using AcHelper;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsDrawService
    {
        public string geoLayer;
        public string flgLayer;
        public string portLayer;
        public string airValveLayer;// (除加压送风场景)止回阀 多叶调节风阀 防火阀 消声器同图层
        public string fireValveLayer;
        public string electrycityValveLayer;
        public string startLayer;
        public string centerLayer;
        public string ductSizeLayer;
        public string dimensionLayer;
        public string portMarkLayer;
        public string holeLayer;
        public string silencerLayer;
        public string airValveName;
        public string fireValveName;
        public string electrycityValveName;
        public string holeName;
        public string portName;
        public string portMarkName;
        public string brokenLine;
        public string verticalPipe;
        public string flipDown45;
        private double textAngle;
        private string airValveVisibility;
        private string fireValveVisibility;
        public string electrycityValveVisibility;
        public ThDuctPortsDrawDim dimService;
        public ThDuctPortsDrawValve airValveService;
        public ThDuctPortsDrawValve fireValveService;
        public ThDuctPortsDrawText textService;
        public ThDuctPortsDrawPort portService;
        public ThDuctPortsDrawPortMark markService;
        public ThDuctPortsDrawEndComp endCompService;
        
        public ThDuctPortsDrawService(string scenario, string scale)
        {
            airValveName = "风阀";
            fireValveName = "防火阀";
            portMarkName = "AI-风口标注1";
            holeName = "AI-洞口";
            portName = "AI-风口";
            airValveVisibility = "多叶调节风阀";
            electrycityValveVisibility = "电动多叶调节风阀";
            flipDown45 = "AI-45度下翻";
            brokenLine = "AI-风管断线";
            verticalPipe = "AI-风管立管";
            SetLayer(scenario);
            ImportLayerBlock();
            PreProcLayer();
            dimService = new ThDuctPortsDrawDim(dimensionLayer, scale);
            airValveService = new ThDuctPortsDrawValve(airValveVisibility, airValveName, airValveLayer);
            if (scenario == "消防排烟")
            {
                fireValveVisibility = ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_280;
            }
            else if (scenario == "消防补风" || scenario == "消防加压送风")
            {
                fireValveVisibility = ThHvacCommon.BLOCK_VALVE_VISIBILITY_FIRE_MEC;
            }
            GetUcsAngle();
            fireValveService = new ThDuctPortsDrawValve(fireValveVisibility, fireValveName, fireValveLayer);
            textService = new ThDuctPortsDrawText(ductSizeLayer);
            portService = new ThDuctPortsDrawPort(portLayer, portName, textAngle);
            endCompService = new ThDuctPortsDrawEndComp(flipDown45, brokenLine, verticalPipe, geoLayer);
            markService = new ThDuctPortsDrawPortMark(textAngle, portMarkName, portMarkLayer);
        }
        private void GetUcsAngle()
        {
            var ucs = Active.Editor.CurrentUserCoordinateSystem;
            textAngle = Vector3d.XAxis.GetAngleTo(ucs.CoordinateSystem3d.Xaxis);
        }
        private void SetLayer(string scenario)
        {
            string layerFlag;
            switch (scenario)
            {
                case "消防排烟兼平时排风": case "消防补风兼平时送风":
                    layerFlag = "DUAL";
                    break;
                case "消防排烟": case "消防补风": case "消防加压送风":
                    layerFlag = "FIRE";
                    break;
                case "平时送风": case "平时排风":
                    layerFlag = "VENT";
                    break;
                case "事故排风": case "事故补风": case "平时送风兼事故补风": case "平时排风兼事故排风":
                    layerFlag = "EVENT";
                    break;
                case "厨房排油烟补风": case "厨房排油烟":
                    layerFlag = "KVENT";
                    break;
                case "空调送风": case "空调回风":
                    layerFlag = "ACON";
                    break;
                case "空调新风":
                    layerFlag = "FCON";
                    break;
                default:throw new NotImplementedException("No such scenario!");
            }
            geoLayer = "H-" + layerFlag + "-DUCT";
            centerLayer = "H-" + layerFlag + "-DUCT-MID";
            portLayer = "H-" + layerFlag + "-GRIL";
            flgLayer = "H-" + layerFlag + "-DAPP";
            airValveLayer = "H-" + layerFlag + "-DAMP";
            fireValveLayer = airValveLayer;
            ductSizeLayer = "H-" + layerFlag + "-DIMS";
            silencerLayer = (scenario == "空调新风") ? "H-ACON-DAMP" : airValveLayer;
            dimensionLayer = ductSizeLayer;
            portMarkLayer = ductSizeLayer;
            startLayer = "AI-风管起点";
            holeLayer = "H-HOLE";
            electrycityValveLayer = "H-FIRE-EDAMP";
            
        }
        private void ImportLayerBlock()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(geoLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(flgLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(portLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(airValveLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(fireValveLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(startLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(centerLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ductSizeLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(dimensionLayer));
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(portMarkLayer));
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(portMarkName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(portName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(airValveName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(fireValveName), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(flipDown45), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(brokenLine), true);
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(verticalPipe), true);
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM150"));
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM100"));
                currentDb.DimStyles.Import(blockDb.DimStyles.ElementOrDefault("TH-DIM50"));
            }
        }
        private void PreProcLayer()
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                GetCurLayer(db, "0");
                GetCurLayer(db, geoLayer);
                GetCurLayer(db, flgLayer);
                GetCurLayer(db, portLayer);
                GetCurLayer(db, airValveLayer);
                GetCurLayer(db, fireValveLayer);
                GetCurLayer(db, centerLayer);
                GetCurLayer(db, ductSizeLayer);
                GetCurLayer(db, dimensionLayer);
                GetCurLayer(db, portMarkLayer);
            }
        }
        private void GetCurLayer(AcadDatabase db, string layerName)
        {
            db.Database.UnFrozenLayer(layerName);
            db.Database.UnLockLayer(layerName);
            db.Database.UnOffLayer(layerName);
        }
        public void DrawVerticalPipe(List<SegInfo> centerLines, Matrix3d mat)
        {
            foreach (var seg in centerLines)
            {
                var duct = ThDuctPortsFactory.CreateVerticalPipe(seg, seg.ductSize);
                DrawDuct(duct, mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
                var ductParam = ThMEPHVACService.CreateDuctModifyParam(duct.centerLines, seg.ductSize, seg.elevation, seg.airVolume);
                ThDuctPortsRecoder.CreateDuctGroup(geoIds, flgIds, centerIds, ductParam);
            }
        }
        public void DrawDuct(List<SegInfo> centerLines, Matrix3d mat)
        {
            foreach (var seg in centerLines)
            {
                var l = seg.GetShrinkedLine();
                var duct = ThDuctPortsFactory.CreateDuct(l.StartPoint, l.EndPoint, ThMEPHVACService.GetWidth(seg.ductSize));
                DrawDuct(duct, mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
                var ductParam = ThMEPHVACService.CreateDuctModifyParam(duct.centerLines, seg.ductSize, seg.elevation, seg.airVolume);
                ThDuctPortsRecoder.CreateDuctGroup(geoIds, flgIds, centerIds, ductParam);
            }
        }
        public void DrawReducing(List<LineGeoInfo> reducings, Matrix3d mat)
        {
            foreach (var red in reducings)
            {
                DrawShape(red, mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
                if (geoIds.Count == 2)
                    ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "Reducing");
                else if (geoIds.Count == 4)
                    ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "AxisReducing");
                else
                    throw new NotImplementedException("[CheckError]: No such reducing!");
            }
        }
        public void DrawDuct(LineGeoInfo info, Matrix3d mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds)
        {
            DrawLines(info.geo, mat, geoLayer, out geoIds);
            DrawLines(info.flg, mat, geoLayer, out flgIds);
            DrawLines(info.centerLines, mat, centerLayer, out centerIds);
        }
        public void DrawDashDuct(LineGeoInfo info, Matrix3d mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds)
        {
            DrawDashLines(info.geo, mat, geoLayer, out geoIds);
            DrawDashLines(info.flg, mat, geoLayer, out flgIds);
            DrawDashLines(info.centerLines, mat, centerLayer, out centerIds);
        }
        public void DrawShape(LineGeoInfo info, Matrix3d mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds)
        {
            DrawLines(info.geo, mat, geoLayer, out geoIds);
            DrawLines(info.flg, mat, flgLayer, out flgIds);
            DrawLines(info.centerLines, mat, centerLayer, out centerIds);
        }
        public void DrawDashShape(LineGeoInfo info, Matrix3d mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds)
        {
            DrawDashLines(info.geo, mat, geoLayer, out geoIds);
            DrawDashLines(info.flg, mat, flgLayer, out flgIds);
            DrawDashLines(info.centerLines, mat, centerLayer, out centerIds);
        }
        public static void DrawDashLines(DBObjectCollection lines, Matrix3d transMat, string strLayer, out ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                ids = new ObjectIdList();
                if (lines != null)
                {
                    foreach (Curve obj in lines)
                    {
                        var shadow = obj.Clone() as Curve;
                        ids.Add(db.ModelSpace.Add(shadow));
                        shadow.SetDatabaseDefaults();
                        shadow.Layer = strLayer;
                        shadow.ColorIndex = (int)ColorIndex.BYLAYER;
                        shadow.Linetype = ThHvacCommon.DASH_LINETYPE;
                        shadow.TransformBy(transMat);
                    }
                }
            }
        }
        public static void DrawLines(DBObjectCollection lines, Matrix3d transMat, string strLayer, out ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                ids = new ObjectIdList();
                if(lines != null)
                {
                    foreach (Curve obj in lines)
                    {
                        var shadow = obj.Clone() as Curve;
                        ids.Add(db.ModelSpace.Add(shadow));
                        shadow.SetDatabaseDefaults();
                        shadow.Layer = strLayer;
                        shadow.ColorIndex = (int)ColorIndex.BYLAYER;
                        shadow.Linetype = "ByLayer";
                        shadow.TransformBy(transMat);
                    }
                }
            }
        }
        public static void RemoveIds(ObjectId[] objectIds)
        {
            foreach (var id in objectIds)
            {
                if (!id.IsErased)
                    id.Erase();
            }
        }
        public static void RemoveIds(DBObjectCollection dbs)
        {
            foreach (Entity e in dbs)
            {
                var id = e.Id;
                if (!id.IsErased)
                    id.Erase();
            }
        }
        public static string GetCurLayer(ObjectIdCollection coll)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return coll.Cast<ObjectId>().Select(id => acadDatabase.Element<Entity>(id)).Select(e => e.Layer).FirstOrDefault();
            }
        }
        public static void SetValveDynBlockProperity(ObjectId obj, double width, double text_height, double text_angle, string valve_visibility)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY))
                    properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, valve_visibility);
                if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA))
                    properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA, width);
                if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT))
                    properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT, text_height);
                if (properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE))
                    properity.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE, text_angle);
            }
        }
        public static void SetHoleDynBlockProperity(ObjectId obj, double width, double len)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                if (properity.Contains("长度"))
                    properity.SetValue("长度", len);
                if (properity.Contains("宽度或直径"))
                    properity.SetValue("宽度或直径", width);
            }
        }
        public static void SetMufflerDynBlockProperity(ObjectId obj, MufflerModifyParam muffler)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                if (properity.Contains("可见性"))
                    properity.SetValue("可见性", muffler.mufflerVisibility);
                if (properity.Contains("长度"))
                    properity.SetValue("长度", muffler.len);
                if (properity.Contains("宽度"))
                    properity.SetValue("宽度", muffler.width);
                if (properity.Contains("高度"))
                    properity.SetValue("高度", muffler.height);
                if (properity.Contains("字高"))
                    properity.SetValue("字高", muffler.textHeight);
            }
        }
        public static void GetHoseDynBlockProperity(ObjectId obj, out double len, out double width)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                len = properity.Contains("长度") ? (double)properity.GetValue("长度") : 0;
                width = properity.Contains("宽度或直径") ? (double)properity.GetValue("宽度或直径") : 0;
            }
        }
        public static void GetValveDynBlockProperity(ObjectId obj,
                                                     out Point3d pos,
                                                     out double width, 
                                                     out double height,
                                                     out double textAngle,
                                                     out double rotateAngle,
                                                     out string valveVisibility)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                valveVisibility = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY) ?
                                  (string)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY) : String.Empty;
                width = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA) : 0;
                height = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_HEIGHT) : 0;
                textAngle = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_TEXT_ROTATE) : 0;
                rotateAngle = data.Rotation;
                pos = data.Position;
            }
        }
        public static void GetHoleDynBlockProperity(ObjectId obj,
                                                    out Point3d pos,
                                                    out double len,
                                                    out double width,
                                                    out double rotateAngle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                len = properity.Contains("长度") ? (double)properity.GetValue("长度") : 0;
                width = properity.Contains("宽度或直径") ? (double)properity.GetValue("宽度或直径") : 0;
                rotateAngle = data.Rotation;
                pos = data.Position;
            }
        }
        public static void GetMufflerDynBlockProperity(ObjectId obj,
                                                       out Point3d pos,
                                                       out string visibility,
                                                       out double len,
                                                       out double height,
                                                       out double width,
                                                       out double textHeight,
                                                       out double rotateAngle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                visibility = properity.Contains("可见性") ? (string)properity.GetValue("可见性") : String.Empty;
                len = properity.Contains("长度") ? (double)properity.GetValue("长度") : 0;
                width = properity.Contains("宽度") ? (double)properity.GetValue("宽度") : 0;
                height = properity.Contains("高度") ? (double)properity.GetValue("高度") : 0;
                textHeight = properity.Contains("字高") ? (double)properity.GetValue("字高") : 0;
                rotateAngle = data.Rotation;
                pos = data.Position;
            }
        }
        public static void GetPortDynBlockProperity(ObjectId obj,
                                                    out Point3d pos,
                                                    out string portRange,
                                                    out double portHeight,
                                                    out double portWidth,
                                                    out double rotateAngle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var properity = data.CustomProperties;
                portWidth = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER) : 0;
                portHeight = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT) : 0;
                portRange = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE) ?
                        (string)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE) : String.Empty;
                rotateAngle = data.Rotation;
                pos = data.Position;
            }
        }
        public static void SetPortDynBlockProperity(ObjectId obj, double portWidth, double portHeight, string portRange, double textAngle, Dictionary<string, string> attr)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_WIDTH_OR_DIAMETER, portWidth);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_HEIGHT, portHeight);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PORT_RANGE, portRange);
                var block = acadDatabase.Element<BlockReference>(obj);
                foreach (ObjectId attId in block.AttributeCollection)
                {
                    // 获取块参照属性对象
                    var attRef = acadDatabase.Element<AttributeReference>(attId);
                    //判断属性名是否为指定的属性名
                    if (attr.Any(c => c.Key.Equals(attRef.Tag)))
                    {
                        attRef.Rotation = textAngle;
                        break;
                    }
                }
            }
        }
        public static void GetVerticalPipeDynBlockProperity(ObjectId obj, out double pipeWidth, out double pipeHeight)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                var size = data.Attributes[ThHvacCommon.BLOCK_DYNAMIC_VERTICAL_PIPE_CUT_SIZE];
                ThMEPHVACService.GetWidthAndHeight(size, out pipeWidth, out pipeHeight);
            }
        }
        public static void SetVerticalPipeDynBlockProperity(ObjectId obj, double pipeWidth, double pipeHeight)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_VERTICAL_PIPE_LENGTH))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_VERTICAL_PIPE_LENGTH, pipeWidth);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_VERTICAL_PIPE_WIDTH))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_VERTICAL_PIPE_WIDTH, pipeHeight);
            }
        }
        public static void SetBrokenLineDynBlockProperity(ObjectId obj, double pipeWidth)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_BROKEN_LEN))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_BROKEN_LEN, pipeWidth);
            }
        }
        public static void SetFlipDown45DynBlockProperity(ObjectId obj, double width, double height)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var data = new ThBlockReferenceData(obj);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_FLIP_DOWN_45_WIDTH))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_FLIP_DOWN_45_WIDTH, width);
                if (data.CustomProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_FLIP_DOWN_45_HEIGHT))
                    data.CustomProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_FLIP_DOWN_45_HEIGHT, height);
            }
        }

        public static void GetFanDynBlockProperity(ThBlockReferenceData fanData,
                                                   bool isAxis,
                                                   out double fanInWidth,
                                                   out double fanOutWidth,
                                                   out string installStyle)
        {
            using (var db = AcadDatabase.Active())
            {
                var properity = fanData.CustomProperties;
                if (isAxis)
                {
                    fanInWidth = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) : 0;
                    fanOutWidth = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) ?
                            (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_DIAMETER) : 0;
                }
                else
                {
                    fanInWidth = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL) ?
                        (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INLET_HORIZONTAL) : 0;
                    fanOutWidth = properity.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL) ?
                            (double)properity.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_OUTLET_HORIZONTAL) : 0;
                }
                installStyle = fanData.Attributes.Keys.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INSTALL_STYLE) ?
                        GetInstallStyle(fanData.Attributes[ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_INSTALL_STYLE]) : "吊装";
            }
        }
        private static string GetInstallStyle(string style)
        {
            if (!String.IsNullOrEmpty(style))
            {
                if (style.Contains("吊装"))
                    return "吊装";
                else if (style.Contains("落地"))
                    return "落地";
            }
            return "吊装";
        }
        public static void MoveToZero(Point3d alignP, DBObjectCollection lineSet)
        {
            var disMat = Matrix3d.Displacement(-alignP.GetAsVector());
            foreach (Curve l in lineSet)
                l.TransformBy(disMat);
        }
        public static void RemoveGroupByComp(ObjectId id)
        {
            using (var db = AcadDatabase.Active())
            {
                var ids = id.GetGroups();
                foreach (var g_id in ids)
                {
                    if (!g_id.IsErased)
                    {
                        g_id.RemoveXData(ThHvacCommon.RegAppName_Duct_Info);
                        RemoveGroup(g_id);
                    }
                }
            }  
        }
        public static void ClearGraphs(ObjectIdList ids)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var id in ids)
                {
                    if (id.Handle.Value != 0)
                    {
                        var gId = db.Database.GetObjectId(false, id.Handle, 0);
                        if (!gId.IsErased)
                            RemoveGroup(gId);
                    }
                }
            }
        }
        public static void ClearGraphs(List<Handle> handles)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var handle in handles)
                {
                    if (handle.Value != 0)
                    {
                        var g_id = db.Database.GetObjectId(false, handle, 0);
                        if (!g_id.IsErased)
                            RemoveGroup(g_id);
                    }
                }
            }
        }
        public static void ClearGraph(Handle handle)
        {
            using (var db = AcadDatabase.Active())
            {
                if (handle.Value != 0)
                {
                    var g_id = db.Database.GetObjectId(false, handle, 0);
                    if (!g_id.IsErased)
                        RemoveGroup(g_id);
                }
            }
        }
        private static void RemoveGroup(ObjectId gId)
        {
            using (var db = AcadDatabase.Active())
            {
                var component_ids = Dreambuild.AutoCAD.DbHelper.GetEntityIdsInGroup(gId);
                foreach (ObjectId i in component_ids)
                    i.Erase();
                gId.Erase();
            }
        }
        public void DrawSpecialShape(List<EntityModifyParam> specialShapesInfo, Matrix3d orgDisMat)
        {
            foreach (var info in specialShapesInfo)
            {
                switch (info.portWidths.Count)
                {
                    case 2: DrawElbow(info, orgDisMat); break;
                    case 3: DrawTee(info, orgDisMat); break;
                    case 4: DrawCross(info, orgDisMat); break;
                    default: throw new NotImplementedException("[checkerror]: No such connector!");
                }
            }
        }
        public void DrawMainDuctText(List<TextAlignLine> textAlignment, Point3d srtP, FanParam param, double mainHeight)
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            for (int i = 0; i < textAlignment.Count; ++i)
            {
                var t = textAlignment[i];
                t.l.TransformBy(mat);
            }
            textService.GetMainDuctInfo(param, textAlignment, mainHeight, out List <DBText> ductSizeInfo);
            textService.DrawDuctSizeInfo(ductSizeInfo);
        }
        public void DrawSideDuctText(ThFanAnalysis anay, Point3d srtP, FanParam fanParam)
        {
            var roomParam = new ThMEPHVACParam() { scale = fanParam.scale, elevation = Double.Parse(fanParam.roomElevation), inDuctSize = fanParam.roomDuctSize };
            DrawSideDuctText(anay.textRoomAlignment, srtP, roomParam);
            var notRoomParam = new ThMEPHVACParam() { scale = fanParam.scale, elevation = Double.Parse(fanParam.notRoomElevation), inDuctSize = fanParam.notRoomDuctSize };
            DrawSideDuctText(anay.textNotRoomAlignment, srtP, notRoomParam);
        }
        public void DrawSideDuctText(List<TextAlignLine> textAlignment, Point3d srtP, ThMEPHVACParam param)
        {
            var mat = Matrix3d.Displacement(srtP.GetAsVector());
            for (int i = 0; i < textAlignment.Count; ++i)
            {
                var t = textAlignment[i];
                t.l.TransformBy(mat);
            }
            textService.GetEndLineDuctTextInfo(param, textAlignment, out List<DBText> ductSizeInfo);
            textService.DrawDuctSizeInfo(ductSizeInfo);
        }
        protected void DrawCross(EntityModifyParam info, Matrix3d orgDisMat)
        {
            var crossInfo = GetCrossInfo(info);
            var cross = ThDuctPortsFactory.CreateCross(ThMEPHVACService.GetWidth(crossInfo.iWidth),
                                                       ThMEPHVACService.GetWidth(crossInfo.innerWidth),
                                                       ThMEPHVACService.GetWidth(crossInfo.coWidth),
                                                       ThMEPHVACService.GetWidth(crossInfo.outterWidth));
            var mat = GetTransMat(crossInfo.trans);
            DrawShape(cross, orgDisMat * mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "Cross");
        }
        private void SepCrossIdx(Vector3d inVec, 
                                 List<Point3d> points, 
                                 Point3d centerP,
                                 out int collinearIdx,
                                 out int other1Idx,
                                 out int other2Idx)
        {
            collinearIdx = 1;
            other1Idx = 2;
            other2Idx = 3;
            for (int i = 1; i < 4; ++i)
            {
                var dirVec = (points[i] - centerP).GetNormal();
                if (ThMEPHVACService.IsCollinear(inVec, dirVec))
                    collinearIdx = i;
            }
            if (collinearIdx == 2)
            {
                other1Idx = 1; other2Idx = 3;
            }
            if (collinearIdx == 3)
            {
                other1Idx = 1; other2Idx = 2;
            }
        }
        private CrossInfo GetCrossInfo(EntityModifyParam info)
        {
            var points = info.portWidths.Keys.ToList();
            var inVec = (points[0] - info.centerP).GetNormal();
            SepCrossIdx(inVec, points, info.centerP, out int collinearIdx, out int other1Idx, out int other2Idx);
            var branchVec = (points[other1Idx] - info.centerP).GetNormal();
            var flag = inVec.CrossProduct(branchVec).Z > 0;
            var innerIdx = flag ? other1Idx : other2Idx;
            var outterIdx = flag ? other2Idx : other1Idx;
            double innerWidth = ThMEPHVACService.GetWidth(info.portWidths[points[innerIdx]]);
            double outterWidth = ThMEPHVACService.GetWidth(info.portWidths[points[outterIdx]]);
            double rotateAngle = ThDuctPortsShapeService.GetCrossRotateAngle(inVec);
            var innerVec = (points[innerIdx] - info.centerP).GetNormal();
            var judgeVec = (Vector3d.XAxis).RotateBy(rotateAngle, -Vector3d.ZAxis);
            var tor = new Tolerance(1e-3, 1e-3);
            var flip = false;
            if (!judgeVec.IsEqualTo(innerVec, tor))
                flip = true;
            if (outterWidth < innerWidth)
                flip = true;
            var trans = new TransInfo() { rotateAngle = rotateAngle, centerPoint = info.centerP, flip = flip};
            return new CrossInfo() { iWidth = info.portWidths[points[0]], 
                                     innerWidth = info.portWidths[points[innerIdx]], 
                                     coWidth = info.portWidths[points[collinearIdx]], 
                                     outterWidth = info.portWidths[points[outterIdx]], trans = trans };
        }
        protected void DrawTee(EntityModifyParam info, Matrix3d orgDisMat)
        {
            var points = info.portWidths.Keys.ToList();
            var type = ThDuctPortsShapeService.GetTeeType(info.centerP, points[1], points[2]);
            var teeInfo = GetTeeInfo(info, type);
            var tee = ThDuctPortsFactory.CreateTee(ThMEPHVACService.GetWidth(teeInfo.mainWidth),
                                                   ThMEPHVACService.GetWidth(teeInfo.branch),
                                                   ThMEPHVACService.GetWidth(teeInfo.other), type);
            var mat = GetTransMat(teeInfo.trans);
            DrawShape(tee, orgDisMat * mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "Tee");
        }
        private TeeInfo GetTeeInfo(EntityModifyParam info, TeeType type)
        {
            var points = info.portWidths.Keys.ToList();
            var inVec = (points[0] - info.centerP).GetNormal();
            double rotateAngle = ThDuctPortsShapeService.GetTeeRotateAngle(inVec);
            double inWidth = ThMEPHVACService.GetWidth(info.portWidths[points[0]]);
            var vec = (points[1] - info.centerP).GetNormal();
            var flag = (type == TeeType.BRANCH_VERTICAL_WITH_OTTER) ? 
                        ThMEPHVACService.IsCollinear(inVec, vec) : inVec.CrossProduct(vec).Z > 0;
            var otherIdx = flag ? 1 : 2;
            var branchIdx = flag ? 2 : 1;
            var branchVec = (points[branchIdx] - info.centerP).GetNormal();
            var judgeVec = (Vector3d.XAxis).RotateBy(rotateAngle, -Vector3d.ZAxis);
            var theta = branchVec.GetAngleTo(judgeVec);
            var tor = 5.0 / 180.0 * Math.PI;// 旁通和主管段夹角在5°内的三通
            var flip = false;
            if (theta > tor)
                flip = true;
            var trans = new TransInfo() { rotateAngle = rotateAngle, centerPoint = info.centerP , flip = flip};
            return new TeeInfo() { mainWidth = info.portWidths[points[0]], branch = info.portWidths[points[branchIdx]], other = info.portWidths[points[otherIdx]], trans = trans };
        }
        protected void DrawElbow(EntityModifyParam info, Matrix3d orgDisMat)
        {
            var elbowInfo = GetElbowInfo(info);
            var elbow = ThDuctPortsFactory.CreateElbow(elbowInfo.openAngle, ThMEPHVACService.GetWidth(elbowInfo.ductWidth));
            var mat = GetTransMat(elbowInfo.trans);
            DrawShape(elbow, orgDisMat * mat, out ObjectIdList geoIds, out ObjectIdList flgIds, out ObjectIdList centerIds);
            ThDuctPortsRecoder.CreateGroup(geoIds, flgIds, centerIds, "Elbow");
        }
        private ElbowInfo GetElbowInfo(EntityModifyParam info)
        {
            var points = info.portWidths.Keys.ToList();
            var inP = points.FirstOrDefault();
            var outP = points.LastOrDefault();
            double inWidth = ThMEPHVACService.GetWidth(info.portWidths[inP]);
            double outWidth = ThMEPHVACService.GetWidth(info.portWidths[outP]);
            var inVec = (inP - info.centerP).GetNormal();
            var outVec = (outP - info.centerP).GetNormal();
            double openAngle = Math.PI - inVec.GetAngleTo(outVec);
            double rotateAngle = ThDuctPortsShapeService.GetElbowRotateAngle(inVec);
            // -Vector3d.ZAxis->顺时针转
            // otherVec是factory造出来的弯头的另一个出口
            var otherVec = (-Vector3d.YAxis).RotateBy(inVec.GetAngleTo(outVec), -Vector3d.ZAxis);
            var judgeVec = otherVec.RotateBy(rotateAngle, -Vector3d.ZAxis);
            var tor = new Tolerance(1e-3, 1e-3);
            if (!judgeVec.IsEqualTo(outVec, tor))
                rotateAngle -= (inVec.GetAngleTo(outVec));
            var w = inWidth < outWidth ? info.portWidths[inP] : info.portWidths[outP];
            // Matrix rotate->顺时针转
            var trans = new TransInfo() { rotateAngle = rotateAngle, centerPoint = info.centerP };
            return new ElbowInfo() { openAngle = openAngle, ductWidth = w, trans = trans };
        }
        private static Matrix3d GetTransMat(TransInfo trans)
        {
            var p = new Point3d(trans.centerPoint.X, trans.centerPoint.Y, 0);
            var flipMat = trans.flip ? Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0))) : Matrix3d.Identity;
            var mat = Matrix3d.Displacement(p.GetAsVector()) *
                      Matrix3d.Rotation(trans.rotateAngle, -Vector3d.ZAxis, Point3d.Origin) * flipMat;
            return mat;
        }
    }
}