using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.LaneLine;
using NFox.Cad;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThArrangementEngine:IDisposable
    {
        protected List<Line> DxLines { get; set; }
        protected List<Line> FdxLines { get; set; }
        protected ThLightArrangeParameter ArrangeParameter;
        protected ThRacewayParameter RacewayParameter { get; set; }
        public ThArrangementEngine(
            ThLightArrangeParameter arrangeParameter,
            ThRacewayParameter racewayParameter)
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
            ArrangeParameter = arrangeParameter;
            RacewayParameter = racewayParameter;
        }
        public abstract void Arrange(List<ThRegionBorder> regionBorders);
        protected List<Line> Trim(List<Line> lines,Polyline regionBorder)
        {
            Polyline bufferPoly = regionBorder.Buffer(-50)[0] as Polyline;
            List<Line> results = new List<Line>();
            lines.ForEach(o =>
            {
                var objs = bufferPoly.Trim(o);
                objs.Cast<Entity>()
                .Where(c=>c is Curve).ToList().ForEach(m =>
                {
                    if (m is Line line)
                    {
                        results.Add(line);
                    }
                    else if (m is Polyline polyline)
                    {
                        var lineObjs = new DBObjectCollection();
                        polyline.Explode(lineObjs);
                        lineObjs.Cast<Line>().ToList().ForEach(n => results.Add(n));
                    }
                });
            });
            return results;
        }
        protected List<Line> WashClone(List<Line> lines)
        {
            List<Line> results = new List<Line>();
            lines.ForEach(o => results.Add(o.WashClone() as Line));
            return results;
        }
        public void Dispose()
        {            
        }
        protected void Preprocess(ThRegionBorder regionBorder)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var precessEngine=new ThLightLinePreprocessEngine())
            {
                Export();
                precessEngine.RemoveLength = ThGarageLightCommon.ThShortLightLineLength;

                var dxWashLines = WashClone(regionBorder.DxCenterLines);
                var ss = dxWashLines.Select(o => o.Length);
                var fdxWashLines = WashClone(regionBorder.FdxCenterLines);                
                //用房间轮廓线对车道中心线进行打断，线的端点距离边界500
                var dxTrimLines = Trim(dxWashLines, regionBorder.RegionBorder);
                var shortenPara = new ThShortenParameter
                {
                    Border = regionBorder.RegionBorder,
                    DxLines = dxTrimLines,
                    FdxLines = fdxWashLines,
                    Distance = ThGarageLightCommon.RegionBorderBufferDistance
                };
                var dxLines = ThShortenLineService.Shorten(shortenPara);
                if (dxLines.Count == 0)
                {
                    return;
                }
                dxLines = precessEngine.Preprocess(dxLines);
                //fdxWashLines = precessEngine.Preprocess(fdxWashLines);                
                //单排取消过滤无需布灯的短线(20210104)
                if (!ArrangeParameter.IsSingleRow)
                {
                    //T形短线取消，对于T形的主边不做处理
                    dxLines = ThFilterTTypeCenterLineService.Filter(dxLines, ArrangeParameter.MinimumEdgeLength);
                    dxLines = ThFilterMainCenterLineService.Filter(dxLines, ArrangeParameter.RacywaySpace / 2.0);
                }
                //var s = shortDxLines.Select(x => x.Length).ToList();
                //var cutResult = precessEngine.Cut(shortDxLines, fdxWashLines);
                DxLines.AddRange(dxLines);
                var s = DxLines.Select(x => x.Length).ToList();
                FdxLines.AddRange(fdxWashLines);
            }
        }
        protected ObjectIdList Print(List<ThLightEdge> lightEdges)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objIds = new ObjectIdList();
                lightEdges.Where(o => o.IsDX).ToList().ForEach(m =>
                  {
                      var normalLine = m.Edge.Clone() as Line;
                      using (var fixedPrecision = new ThCADCoreNTSFixedPrecision())
                      {
                          normalLine = m.Edge.Normalize();
                      }
                      m.LightNodes.ForEach(n =>
                    {
                        if (!string.IsNullOrEmpty(n.Number))
                        {
                            DBText code = new DBText();
                            code.TextString = n.Number;
                            var alignPt = n.Position + normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                            .GetPerpendicularVector()
                            .GetNormal()
                            .MultiplyBy(ArrangeParameter.Width/2.0 + 100 + ArrangeParameter.LightNumberTextHeight / 2.0);
                            code.Height = ArrangeParameter.LightNumberTextHeight;
                            code.WidthFactor = ArrangeParameter.LightNumberTextWidthFactor;
                            code.Position = alignPt;
                            //文字旋转角度
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
                            //TypedValueList codeValueList = new TypedValueList
                            //      {
                            //    { (int)DxfCode.ExtendedDataAsciiString, n.Number},
                            //      };
                            //XDataTools.AddXData(codeId, ThGarageLightCommon.ThGarageLightAppName, codeValueList);
                        }
                        var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                            RacewayParameter.LaneLineBlockParameter.Layer,
                            ThGarageLightCommon.LaneLineLightBlockName,
                            n.Position, new Scale3d(ArrangeParameter.PaperRatio), normalLine.Angle);
                        TypedValueList blkValueList = new TypedValueList
                                  {
                                { (int)DxfCode.ExtendedDataAsciiString, n.Number},
                                { (int)DxfCode.ExtendedDataAsciiString, m.Pattern},
                                { (int)DxfCode.ExtendedDataReal, normalLine.Angle},
                                  };
                        objIds.Add(blkId);
                        XDataTools.AddXData(blkId, ThGarageLightCommon.ThGarageLightAppName, blkValueList);
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

                var numberTextLayer=acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.NumberTextParameter.Layer));
                var numberTextLTR = numberTextLayer.Item as LayerTableRecord;
                numberTextLTR.UpgradeOpen();
                numberTextLTR.LinetypeObjectId = numberTextLT.Item.Id;
                numberTextLTR.DowngradeOpen();

                var portLineLayer = acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.PortLineParameter.Layer));
                var portLineLTR= portLineLayer.Item as LayerTableRecord;
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
