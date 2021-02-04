using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThArrangementEngine : IDisposable
    {
        protected List<Line> DxLines { get; set; }
        protected List<Line> FdxLines { get; set; }
        protected ThRacewayParameter RacewayParameter { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        public ThArrangementEngine(
            ThLightArrangeParameter arrangeParameter,
            ThRacewayParameter racewayParameter)
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
            ArrangeParameter = arrangeParameter;
            RacewayParameter = racewayParameter;
        }
        public void Dispose()
        {
        }
        public abstract void Arrange(List<ThRegionBorder> regionBorders);
        protected List<Line> Trim(List<Line> lines, Polyline regionBorder)
        {
            var results = ThCADCoreNTSGeometryClipper.Clip(regionBorder, lines.ToCollection());
            return ThLaneLineEngine.Explode(results).Cast<Line>().ToList();
        }
        protected void Preprocess(ThRegionBorder regionBorder)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var precessEngine = new ThLightLinePreprocessEngine())
            {
                Export();

                // 裁剪并获取框内的车道线
                var dxTrimLines = Trim(regionBorder.DxCenterLines, regionBorder.RegionBorder);
                if (dxTrimLines.Count == 0)
                {
                    return;
                }

                // 为了避免线槽和防火卷帘冲突
                // 缩短车道线，和框线保持500的间隙
                var shortenPara = new ThShortenParameter
                {
                    Border = regionBorder.RegionBorder,
                    DxLines = dxTrimLines,
                    FdxLines = regionBorder.FdxCenterLines,
                    Distance = ThGarageLightCommon.RegionBorderBufferDistance
                };
                var dxLines = ThShortenLineService.Shorten(shortenPara);
                if (dxLines.Count == 0)
                {
                    return;
                }

                // 将车道线规整
                dxLines = precessEngine.Preprocess(dxLines);
                if (dxLines.Count == 0)
                {
                    return;
                }

                // 过滤车道线
                if (!ArrangeParameter.IsSingleRow)
                {
                    dxLines = ThFilterTTypeCenterLineService.Filter(dxLines, ArrangeParameter.MinimumEdgeLength);
                    dxLines = ThFilterMainCenterLineService.Filter(dxLines, ArrangeParameter.RacywaySpace / 2.0);
                    dxLines = ThFilterElbowCenterLineService.Filter(dxLines, ArrangeParameter.MinimumEdgeLength);
                }
                if (dxLines.Count == 0)
                {
                    return;
                }

                // 保持车道线和非车道线
                DxLines.AddRange(dxLines);
                FdxLines.AddRange(regionBorder.FdxCenterLines);
            }
        }
        protected ObjectIdList CreateLightAndNumber(List<ThLightEdge> lightEdges)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objIds = new ObjectIdList();
                lightEdges.Where(o => o.IsDX).ForEach(m =>
                {
                    var normalLine = ThGarageLightUtils.NormalizeLaneLine(m.Edge);
                    m.LightNodes.ForEach(n =>
                    {
                        if (!string.IsNullOrEmpty(n.Number))
                        {
                            DBText code = new DBText();
                            code.TextString = n.Number;
                            var alignPt = n.Position + normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                              .GetPerpendicularVector()
                              .GetNormal()
                              .MultiplyBy(ArrangeParameter.Width / 2.0 + 100 + ArrangeParameter.LightNumberTextHeight / 2.0);
                            code.Height = ArrangeParameter.LightNumberTextHeight;
                            code.WidthFactor = ArrangeParameter.LightNumberTextWidthFactor;
                            code.Position = alignPt;
                            double angle = normalLine.Angle / Math.PI * 180.0;
                            angle = ThGarageLightUtils.LightNumberAngle(angle);
                            angle = angle / 180.0 * Math.PI;
                            code.Rotation = angle;
                            code.HorizontalMode = TextHorizontalMode.TextCenter;
                            code.VerticalMode = TextVerticalMode.TextVerticalMid;
                            code.AlignmentPoint = code.Position;
                            code.ColorIndex = RacewayParameter.NumberTextParameter.ColorIndex;
                            code.Layer = RacewayParameter.NumberTextParameter.Layer;
                            code.TextStyleId = acadDatabase.TextStyles.Element(ArrangeParameter.LightNumberTextStyle).Id;
                            code.SetDatabaseDefaults(acadDatabase.Database);
                            var codeId = acadDatabase.ModelSpace.Add(code);
                            objIds.Add(codeId);

                            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                                  RacewayParameter.LaneLineBlockParameter.Layer,
                                  ThGarageLightCommon.LaneLineLightBlockName,
                                  n.Position,
                                  new Scale3d(ArrangeParameter.PaperRatio),
                                  normalLine.Angle);
                            objIds.Add(blkId);
                        }
                    });
                });
                return objIds;
            }
        }
        private void Export()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LaneLineLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThGarageLightCommon.LaneLineLightBlockName));
                acadDatabase.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(ArrangeParameter.LightNumberTextStyle), false);
                var centerLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(RacewayParameter.CenterLineParameter.LineType));
                var laneLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(RacewayParameter.LaneLineBlockParameter.LineType));
                var numberTextLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(RacewayParameter.NumberTextParameter.LineType));
                var portLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(RacewayParameter.PortLineParameter.LineType));
                var sideLineLT = acadDatabase.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(RacewayParameter.SideLineParameter.LineType));

                var centerLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.CenterLineParameter.Layer));
                var centerLineLayerLTR = centerLineLayer.Item as LayerTableRecord;
                centerLineLayerLTR.UpgradeOpen();
                centerLineLayerLTR.LinetypeObjectId = centerLineLT.Item.Id;
                centerLineLayerLTR.DowngradeOpen();

                var laneLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.LaneLineBlockParameter.Layer));
                var laneLineLTR = laneLineLayer.Item as LayerTableRecord;
                laneLineLTR.UpgradeOpen();
                laneLineLTR.LinetypeObjectId = laneLineLT.Item.Id;
                laneLineLTR.DowngradeOpen();

                var numberTextLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.NumberTextParameter.Layer));
                var numberTextLTR = numberTextLayer.Item as LayerTableRecord;
                numberTextLTR.UpgradeOpen();
                numberTextLTR.LinetypeObjectId = numberTextLT.Item.Id;
                numberTextLTR.DowngradeOpen();

                var portLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.PortLineParameter.Layer));
                var portLineLTR = portLineLayer.Item as LayerTableRecord;
                portLineLTR.UpgradeOpen();
                portLineLTR.LinetypeObjectId = portLineLT.Item.Id;
                portLineLTR.DowngradeOpen();

                var sideLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.SideLineParameter.Layer));
                var sideLineLTR = sideLineLayer.Item as LayerTableRecord;
                sideLineLTR.UpgradeOpen();
                sideLineLTR.LinetypeObjectId = sideLineLT.Item.Id;
                sideLineLTR.DowngradeOpen();
            }
        }
    }
}
