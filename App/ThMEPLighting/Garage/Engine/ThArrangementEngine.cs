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
        protected List<Line> Trim(List<Line> lines,Polyline regionBorder,double bufferDis)
        {
            List<Line> results = new List<Line>();
            var bufferObjs = regionBorder.Buffer(bufferDis);
            var bufferBorder=bufferObjs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
            lines.ForEach(o =>
            {
                var objs = bufferBorder.Trim(o);
                objs.Cast<Curve>().ForEach(m =>
                {
                    if (m is Line line)
                    {
                        results.Add(line);
                    }
                    else if (m is Polyline polyline)
                    {
                        var lineObjs = new DBObjectCollection();
                        polyline.Explode(lineObjs);
                        lineObjs.Cast<Line>().ForEach(n => results.Add(n));
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
            {
                var dxWashLines = WashClone(regionBorder.DxCenterLines);
                var fdxWashLines = WashClone(regionBorder.FdxCenterLines);

                //用房间轮廓线对车道中心线进行打断，线的端点距离边界500
                var dxTrimLines = Trim(dxWashLines, regionBorder.RegionBorder,
                    ThGarageLightCommon.RegionBorderBufferDistance);                

                //共线，重叠处理，分割处理
                var fdxMergeLines = ThLaneLineSimplifier.LineMerge(
                    fdxWashLines, ThGarageLightCommon.RepeatedPointDistance);
                var dxMergeLines =ThLaneLineSimplifier.LineMerge(
                    dxTrimLines, ThGarageLightCommon.RepeatedPointDistance);
                FdxLines.AddRange(fdxMergeLines);
                //分割
                using (var splitLineEngine = new ThSplitLineEngine(dxMergeLines))
                {
                    splitLineEngine.Split();                   
                    splitLineEngine.Results.ForEach(o=> DxLines.AddRange(o.Value));                    
                }
                //过滤无需布灯的短线
                DxLines = ThRemoveShortCenterLineService.Remove(DxLines, ArrangeParameter.MinimumEdgeLength);
            }
        }
        protected ObjectIdList Print(List<ThLightEdge> lightEdges)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.LaneLineLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThGarageLightCommon.LaneLineLightBlockName));
                var objIds = new ObjectIdList();
                lightEdges.Where(o=>o.IsDX).ForEach(m =>
                {                    
                    var normalLine = m.Edge.Normalize();
                    m.LightNodes.ForEach(n =>
                    {
                        if (!string.IsNullOrEmpty(n.Number))
                        {
                            DBText code = new DBText();
                            code.TextString = n.Number;
                            var alignPt = n.Position + normalLine.StartPoint.GetVectorTo(normalLine.EndPoint)
                            .GetPerpendicularVector()
                            .GetNormal()
                            .MultiplyBy(ThGarageLightCommon.NumberTextAlighHeight);
                            code.Height = 500.0;
                            code.Position = alignPt;
                            code.Rotation = normalLine.Angle;
                            code.HorizontalMode = TextHorizontalMode.TextCenter;
                            code.VerticalMode = TextVerticalMode.TextVerticalMid;
                            code.AlignmentPoint = code.Position;
                            code.ColorIndex = RacewayParameter.NumberTextParameter.ColorIndex;
                            code.Layer = RacewayParameter.NumberTextParameter.Layer;
                            code.SetDatabaseDefaults(acadDatabase.Database);
                            var codeId = acadDatabase.ModelSpace.Add(code);
                            objIds.Add(codeId);
                            TypedValueList codeValueList = new TypedValueList
                            {
                                { (int)DxfCode.ExtendedDataAsciiString, n.Number},
                            };
                            XDataTools.AddXData(codeId, ThGarageLightCommon.ThGarageLightAppName, codeValueList);
                        }
                        var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                            RacewayParameter.LaneLineBlockParameter.Layer,
                            ThGarageLightCommon.LaneLineLightBlockName,
                            n.Position, new Scale3d(ArrangeParameter.PaperRatio), normalLine.Angle);
                            TypedValueList blkValueList = new TypedValueList
                            {
                                { (int)DxfCode.ExtendedDataAsciiString, n.Number},
                                { (int)DxfCode.ExtendedDataAsciiString, m.Pattern}
                            };
                        objIds.Add(blkId);
                        XDataTools.AddXData(blkId, ThGarageLightCommon.ThGarageLightAppName, blkValueList);
                    });
                });
                return objIds;
            }
        }
    }
}
