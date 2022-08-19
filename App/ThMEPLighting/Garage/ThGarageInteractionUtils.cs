using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage
{
    public static class ThGarageInteractionUtils
    {
        private const double ArcTesslateLength = 10.0;
        private const double SmallAreaTolerance = 1.0;
        private const double FrameArcTesslateLength = 1000.0; // 防火分区,或车道线分区等
        private const double FrameExtendLength = 1000.0; // 防火分区,或车道线分区等
        public static List<ThRegionBorder> GetFireRegionBorders(List<string> laneLineLayers)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var results = new List<ThRegionBorder>();
                var borders = GetRegionBorders();
                if (borders.Count == 0)
                {
                    return results;
                }
                #region ----------获取图纸中的元素----------
                var allCenterLines = GetCenterLines(acdb); // 中心线(线槽)
                var allSideLines = GetSideLines(acdb); // 边线(线槽)                            
                var allLightBlks = GetLightBlks(acdb); // 灯块
                var allNumberTexts = GetNumberTexts(acdb); // 编号文字
                var allLaneLines = GetLaneLines(acdb, laneLineLayers); // 车道中心线
                var allJumpWires = GetJumpWires(acdb); // 跳线
                var allFdxLines = GetFdxLines(acdb); // 非灯线
                var allSingleRowCableTrunkingCenterLines = GetSingleRowCabelTrunkingCenterLines(acdb); // 单排线槽中心线
                var allTCHCableTrays = GetTCHCableTrays(acdb); // 单排线槽中心线
                #endregion
                #region ----------移动到原点位置-----------
                var basePt = borders[0].GetBorderBasePt();
                var transformer = new ThMEPOriginTransformer(basePt);
                Transform(allCenterLines, transformer);
                Transform(allSideLines, transformer);
                Transform(allLightBlks, transformer);
                Transform(allNumberTexts, transformer);
                Transform(allLaneLines, transformer);
                Transform(allJumpWires, transformer);
                Transform(allFdxLines, transformer);
                Transform(allSingleRowCableTrunkingCenterLines, transformer);
                Transform(allTCHCableTrays, transformer);
                #endregion
                #region ----------获取单个框的元素----------
                borders.ForEach(o =>
                {
                    var newBorder = o.Clone() as Entity;
                    var borderTransformer = new ThMEPOriginTransformer(newBorder.GetBorderBasePt());
                    transformer.Transform(newBorder);

                    var regionBorder = new ThRegionBorder
                    {
                        RegionBorder = o.Clone() as Entity,
                        Transformer = borderTransformer,
                        DxCenterLines = GetRegionLaneLines(newBorder, allLaneLines),
                        FdxCenterLines = GetRegionLines(newBorder, allFdxLines),
                        SingleRowLines = GetRegionLines(newBorder, allSingleRowCableTrunkingCenterLines),
                        SideLines = newBorder.SpatialFilter(allSideLines).OfType<Line>().ToList(),
                        Texts = newBorder.SpatialFilter(allNumberTexts).OfType<DBText>().ToList(),
                        JumpWires = newBorder.SpatialFilter(allJumpWires).OfType<Curve>().ToList(),
                        CenterLines = newBorder.SpatialFilter(allCenterLines).OfType<Line>().ToList(),
                        Lights = newBorder.SpatialFilter(allLightBlks).OfType<BlockReference>().ToList(),
                        TCHCableTrays = newBorder.SpatialFilter(allTCHCableTrays).OfType<Entity>().ToList(),
                    };

                    // 将获取到的灯线、非灯线Z坐标归零
                    regionBorder.RegionBorder.ProjectOntoXYPlane();
                    regionBorder.DxCenterLines.ForEach(l => l.ProjectOntoXYPlane());
                    regionBorder.FdxCenterLines.ForEach(l => l.ProjectOntoXYPlane());
                    regionBorder.SingleRowLines.ForEach(l => l.ProjectOntoXYPlane());

                    results.Add(regionBorder);
                });
                #endregion
                #region -----------移动到原位置-------------
                results.ForEach(b =>
                {
                    b.DxCenterLines.ForEach(o => transformer.Reset(o));
                    b.FdxCenterLines.ForEach(o => transformer.Reset(o));
                });
                Reset(allCenterLines, transformer);
                Reset(allSideLines, transformer);
                Reset(allLightBlks, transformer);
                Reset(allTCHCableTrays, transformer);
                Reset(allNumberTexts, transformer);
                Reset(allLaneLines, transformer);
                Reset(allJumpWires, transformer);
                Reset(allFdxLines, transformer);
                Reset(allSingleRowCableTrunkingCenterLines, transformer);
                #endregion
                return results;
            }
        }

        private static Point3d GetBorderBasePt(this Entity regionBorder)
        {
            return regionBorder is Polyline poly ? poly.StartPoint :
                    (regionBorder as MPolygon).Shell().StartPoint;
        }
        public static void OpenLayers(this Database db, List<string> layers)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                acdb.Database.UnLockLayer(layers);
                acdb.Database.UnFrozenLayer(layers);
            }
        }
        private static List<Entity> GetRegionBorders()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "\n请选择布灯的区域框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var results = new List<Entity>();
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var psr = Active.Editor.GetSelection(options, filter);
                if (psr.Status == PromptStatus.OK)
                {
                    psr.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.NormalizeEx(border);
                        if (newBorder.Area > 1.0)
                        {
                            results.Add(newBorder);
                        }
                        else
                        {
                            // 进一步处理
                            newBorder = ThMEPFrameService.Rebuild(border, FrameExtendLength, FrameArcTesslateLength);
                            if (newBorder.Area > 1.0)
                            {
                                results.Add(newBorder);
                            }
                        }
                    });
                }
                return results;
            }
        }

        private static DBObjectCollection GetCenterLines(AcadDatabase acdb)
        {
            // 中心线(线槽)
            return acdb.ModelSpace
                .OfType<Line>()
                .Where(l => l.Layer == ThCableTrayParameter.Instance.CenterLineParameter.Layer)
                .ToCollection();
        }

        private static DBObjectCollection GetSideLines(AcadDatabase acdb)
        {
            // 边线(线槽)
            return acdb.ModelSpace
            .OfType<Line>()
            .Where(l => l.Layer == ThCableTrayParameter.Instance.SideLineParameter.Layer)
            .ToCollection();
        }

        private static DBObjectCollection GetLightBlks(AcadDatabase acdb)
        {
            // 灯块
            return acdb.ModelSpace
            .OfType<BlockReference>()
            .Where(b => !b.BlockTableRecord.IsNull)
            .Where(b => b.Layer == ThCableTrayParameter.Instance.LaneLineBlockParameter.Layer)
            .Where(b => b.GetEffectiveName() == ThGarageLightCommon.LaneLineLightBlockName).ToCollection();
        }

        private static DBObjectCollection GetNumberTexts(AcadDatabase acdb)
        {
            // 编号文字
            return acdb.ModelSpace
            .OfType<DBText>()
            .Where(t => t.Layer == ThCableTrayParameter.Instance.NumberTextParameter.Layer)
            .ToCollection();
        }

        private static DBObjectCollection GetLaneLines(AcadDatabase acdb, List<string> layers)
        {
            // 车道中心线
            return acdb.ModelSpace
                .Where(e => ThGarageLightUtils.IsLightCableCarrierCenterline(e, layers))
                .ToCollection();
        }

        private static DBObjectCollection GetFdxLines(AcadDatabase acdb)
        {
            // 非灯线
            return acdb.ModelSpace
                .Where(e => ThGarageLightUtils.IsNonLightCableCarrierCenterline(e))
                .ToCollection();
        }

        private static DBObjectCollection GetSingleRowCabelTrunkingCenterLines(AcadDatabase acdb)
        {
            // 单排线槽中心线
            return acdb.ModelSpace
                .Where(e => ThGarageLightUtils.IsSingleRowCabelTrunkingCenterline(e))
                .ToCollection();
        }

        private static DBObjectCollection GetTCHCableTrays(AcadDatabase acdb)
        {
            // 单排线槽中心线
            return acdb.ModelSpace
                .Where(e => ThGarageLightUtils.IsTCHCableTray(e))
                .ToCollection();
        }

        private static DBObjectCollection GetJumpWires(AcadDatabase acdb)
        {
            // 跳线
            return acdb.ModelSpace
            .Where(e => e is Line || e is Arc)
            .Where(e => e.Layer == ThCableTrayParameter.Instance.JumpWireParameter.Layer)
            .ToCollection();
        }

        private static void Transform(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            objs.UpgradeOpen();
            transformer.Transform(objs);
        }

        private static void Reset(DBObjectCollection objs, ThMEPOriginTransformer transformer)
        {
            objs.DowngradeOpen();
            transformer.Reset(objs);
        }

        /// <summary>
        /// 获取图纸上所有布置的灯块
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public static DBObjectCollection GetBlockReferences(this Database db, List<string> layers)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                var upperLayers = layers.Select(o => o.ToUpper());
                return acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull)
                    .Where(b => upperLayers.Contains(b.Layer.ToUpper()))
                    .ToCollection();
            }
        }
        public static DBObjectCollection GetDBTexts(this Database db, List<string> layers)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                var upperLayers = layers.Select(o => o.ToUpper());
                return acdb.ModelSpace
                           .OfType<DBText>()
                           .Where(b => upperLayers.Contains(b.Layer.ToUpper()))
                           .ToCollection();
            }
        }
        public static DBObjectCollection GetLines(this Database db, List<string> layers)
        {
            using (var acdb = AcadDatabase.Use(db))
            {
                var upperLayers = layers.Select(o => o.ToUpper());
                return acdb.ModelSpace
                           .OfType<Line>()
                           .Where(b => upperLayers.Contains(b.Layer.ToUpper()))
                           .ToCollection();
            }
        }
        public static DBObjectCollection GetArcs(this Database db, List<string> layers)
        {
            // 线槽端口线和侧边线图层一直
            using (var acdb = AcadDatabase.Use(db))
            {
                var upperLayers = layers.Select(o => o.ToUpper());
                return acdb.ModelSpace
                           .OfType<Arc>()
                           .Where(b => upperLayers.Contains(b.Layer.ToUpper()))
                           .ToCollection();
            }
        }

        private static List<BlockReference> GetRegionLights(Polyline region, DBObjectCollection dbObjs)
        {
            return region.SpatialFilter(dbObjs).Cast<BlockReference>().ToList();
        }
        private static List<Line> GetRegionLines(Entity region, DBObjectCollection dbObjs)
        {
            return ThLaneLineEngine.Explode(region.SpatialFilter(dbObjs)).OfType<Line>().ToList();
        }

        private static List<Line> GetRegionLaneLines(Entity region, DBObjectCollection dbObjs)
        {
            var dxCenterLines = GetRegionLines(region.BufferEx(-100.0), dbObjs);
            var spatialIndex = new ThCADCoreNTSSpatialIndex(dxCenterLines.ToCollection());
            var results = new List<Line>();
            dxCenterLines.ForEach(o =>
            {
                if (o.Length < 500.0)
                {
                    if (spatialIndex.SelectCrossingPolygon(o.StartPoint.CreateSquare(10.0)).Count == 1
                     || spatialIndex.SelectCrossingPolygon(o.EndPoint.CreateSquare(10.0)).Count == 1)
                    {
                        return;
                    }
                }
                results.Add(o);
            });

            return results;
        }

        public static ThLightArrangeParameter GetUiParameters()
        {
            // From UI
            var arrangeParameter = new ThLightArrangeParameter()
            {
                Margin = 800,
                AutoCalculate = ThMEPLightingService.Instance.LightArrangeUiParameter.AutoCalculate,
                Interval = ThMEPLightingService.Instance.LightArrangeUiParameter.Interval,
                IsSingleRow = ThMEPLightingService.Instance.LightArrangeUiParameter.IsSingleRow,
                LoopNumber = ThMEPLightingService.Instance.LightArrangeUiParameter.LoopNumber,
                DoubleRowOffsetDis = ThMEPLightingService.Instance.LightArrangeUiParameter.DoubleRowOffsetDis,
                Width = ThMEPLightingService.Instance.LightArrangeUiParameter.Width,
            };

            // 自定义
            arrangeParameter.Margin = 800.0;
            arrangeParameter.PaperRatio = 100;
            return arrangeParameter;
        }
        public static void SetDatabaseDefaults(this ThCableTrayParameter cableTrayParameter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                var centerLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(cableTrayParameter.CenterLineParameter.LineType));
                var laneLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(cableTrayParameter.LaneLineBlockParameter.LineType));
                var numberTextLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(cableTrayParameter.NumberTextParameter.LineType));
                var sideLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(cableTrayParameter.SideLineParameter.LineType));
                var jumpWireLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(cableTrayParameter.JumpWireParameter.LineType));

                var centerLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(cableTrayParameter.CenterLineParameter.Layer));
                var centerLineLayerLTR = centerLineLayer.Item as LayerTableRecord;
                centerLineLayerLTR.UpgradeOpen();
                centerLineLayerLTR.LinetypeObjectId = centerLineLT.Item.Id;
                centerLineLayerLTR.DowngradeOpen();

                var laneLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(cableTrayParameter.LaneLineBlockParameter.Layer));
                var laneLineLTR = laneLineLayer.Item as LayerTableRecord;
                laneLineLTR.UpgradeOpen();
                laneLineLTR.LinetypeObjectId = laneLineLT.Item.Id;
                laneLineLTR.DowngradeOpen();

                var numberTextLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(cableTrayParameter.NumberTextParameter.Layer));
                var numberTextLTR = numberTextLayer.Item as LayerTableRecord;
                numberTextLTR.UpgradeOpen();
                numberTextLTR.LinetypeObjectId = numberTextLT.Item.Id;
                numberTextLTR.DowngradeOpen();

                var sideLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(cableTrayParameter.SideLineParameter.Layer));
                var sideLineLTR = sideLineLayer.Item as LayerTableRecord;
                sideLineLTR.UpgradeOpen();
                sideLineLTR.LinetypeObjectId = sideLineLT.Item.Id;
                sideLineLTR.DowngradeOpen();

                var jumpWireLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(cableTrayParameter.JumpWireParameter.Layer));
                var jumpWireLTR = jumpWireLayer.Item as LayerTableRecord;
                jumpWireLTR.UpgradeOpen();
                jumpWireLTR.LinetypeObjectId = jumpWireLT.Item.Id;
                jumpWireLTR.DowngradeOpen();
            }
        }

        public static void SetDatabaseDefaults(this ThLightArrangeParameter arrangeParameter)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThGarageLightCommon.LaneLineLightBlockName));
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(arrangeParameter.LightNumberTextStyle), false);
            }
        }
        public static void UnLockLayer(this Database db, List<string> layers)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                layers.ForEach(o => acadDb.Database.UnLockLayer(o));
            }
        }
        public static void UnFrozenLayer(this Database db, List<string> layers)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                layers.ForEach(o => acadDb.Database.UnFrozenLayer(o));
            }
        }
        public static void UpgradeOpen(this DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(e => e.UpgradeOpen());
        }
        public static void DowngradeOpen(this DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(e => e.DowngradeOpen());
        }
        public static void Erase(this DBObjectCollection objs)
        {
            objs.UpgradeOpen();
            objs.OfType<Entity>().ForEach(e => e.Erase());
            objs.DowngradeOpen();
        }
    }
}
