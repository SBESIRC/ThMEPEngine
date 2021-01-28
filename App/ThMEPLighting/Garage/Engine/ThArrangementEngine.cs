﻿using System;
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
            List<Line> results = new List<Line>();
            lines.ForEach(o =>
            {
                var objs = regionBorder.Trim(o);
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
                var fdxWashLines = WashClone(regionBorder.FdxCenterLines);
                var dxResults = ThLaneLineMergeExtension.Merge(dxWashLines.ToCollection());
                var dxObjs = new DBObjectCollection();
                foreach(Entity ent in dxResults)
                {
                    if(ent is Line line)
                    {
                        if (line.Length > ThGarageLightCommon.ThShortLightLineLength)
                        {
                            dxObjs.Add(line);
                        }
                    }
                }     
                dxResults = ThLaneLineMergeExtension.Noding(dxObjs);
                dxWashLines = dxResults.Cast<Line>().ToList();
                //单排取消过滤无需布灯的短线(20210104)
                if (!ArrangeParameter.IsSingleRow)
                {
                    //T形短线取消，对于T形的主边不做处理
                    dxWashLines = ThFilterTTypeCenterLineService.Filter(dxWashLines, ArrangeParameter.MinimumEdgeLength);
                    dxWashLines = ThFilterMainCenterLineService.Filter(dxWashLines, ArrangeParameter.RacywaySpace / 2.0);
                }

                //用房间轮廓线对车道中心线进行打断，线的端点距离边界500
                var dxTrimLines = Trim(dxWashLines, regionBorder.RegionBorder);

                var shortenPara = new ThShortenParameter
                {
                    Border = regionBorder.RegionBorder,
                    DxLines = dxTrimLines,
                    FdxLines = fdxWashLines,
                    Distance = ThGarageLightCommon.RegionBorderBufferDistance
                };
                var shortDxLines = ThShortenLineService.Shorten(shortenPara);

                shortDxLines =precessEngine.Preprocess(shortDxLines);
                fdxWashLines = precessEngine.Preprocess(fdxWashLines);

                var cutResult = precessEngine.Cut(shortDxLines, fdxWashLines);
                DxLines.AddRange(cutResult.Item1);
                FdxLines.AddRange(cutResult.Item2);
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
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.CenterLineParameter.Layer));
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.LaneLineBlockParameter.Layer));
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.NumberTextParameter.Layer));
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.PortLineParameter.Layer));
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(RacewayParameter.SideLineParameter.Layer));
            }
        }
    }
}
