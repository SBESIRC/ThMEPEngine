using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using QuickGraph.Algorithms;
using ThMEPEngineCore.LaneLine;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Engine;
using Autodesk.AutoCAD.EditorInput;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPLighting
{
    public class ThMEPGarageLightingCmds
    {
        [CommandMethod("TIANHUACAD", "THDXC", CommandFlags.Modal)]
        public void ThDxc()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                short colorIndex = 2;
                var polyline = PolylineJig(colorIndex);
                if (polyline == null)
                {
                    return;
                }                
                ThLayerTool.CreateLayer(ThGarageLightCommon.DxCenterLineLayerName,
                    Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex));
                polyline.Layer = ThGarageLightCommon.DxCenterLineLayerName;
                acdb.ModelSpace.Add(polyline);
            }
        }
        [CommandMethod("TIANHUACAD", "THFDXC", CommandFlags.Modal)]
        public void ThFdxc()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                short colorIndex = 1;
                var polyline = PolylineJig(colorIndex);
                if (polyline == null)
                {
                    return;
                }                
                ThLayerTool.CreateLayer(ThGarageLightCommon.FdxCenterLineLayerName,
                    Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, colorIndex));
                polyline.Layer = ThGarageLightCommon.FdxCenterLineLayerName;
                acdb.ModelSpace.Add(polyline);
            }
        }
        [CommandMethod("TIANHUACAD", "THCDZMBZ", CommandFlags.Modal)]
        public void THCDZMBZ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                //获取参数
                var arrangeParameter = GetUiParameters();
                var regionBorders = GetFireRegionBorders();
                if (regionBorders.Count == 0)
                {
                    return;
                }
                regionBorders.ForEach(o =>
                {
                    //移动到原点
                    // 若图元离原点非常远（大于1E+10)，精度会受很大影响
                    // 为了规避这个问题，我们将图元移回原点
                    // 最后将处理结果还原到原始位置
                    var centerPt = o.RegionBorder.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(o.DxCenterLines.ToCollection());
                    transformer.Transform(o.FdxCenterLines.ToCollection());
                    transformer.Transform(o.RegionBorder);
                    o.Transformer = transformer;
                });
                //布置
                ThArrangementEngine arrangeEngine = null;
                var racewayParameter = new ThRacewayParameter();
                if (arrangeParameter.IsSingleRow)
                {
                    arrangeEngine = new ThSingleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                else
                {
                    arrangeEngine = new ThDoubleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                arrangeEngine.Arrange(regionBorders);

                ThCreateLightToDatabaseService.SetDatabaseDefaults(racewayParameter, arrangeParameter);
                regionBorders.ForEach(o =>
                {
                    //输出到当前图纸并还原回原始位置
                    var objs = new DBObjectCollection();
                    var transformer = new ThMEPOriginTransformer(o.RegionBorder.GetCentroidPoint());
                    var objIds = ThCreateLightToDatabaseService.Create(o, racewayParameter, arrangeParameter);
                    objIds.ForEach(e => objs.Add(acadDatabase.Element<Entity>(e, true)));
                    o.Transformer.Reset(objs);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THCDBH", CommandFlags.Modal)]
        public void THCDBH()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                var arrangeParameter = GetUiParameters();
                var racewayParameter = new ThRacewayParameter();
                var regionBorders = GetFireRegionBorders();
                if (regionBorders.Count == 0)
                {
                    return;
                }

                //以上是准备输入参数
                ThArrangementEngine arrangeEngine = null;                
                if (arrangeParameter.IsSingleRow)
                {
                    arrangeEngine = new ThSingleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                else
                {
                    arrangeEngine = new ThDoubleRowArrangementEngine(arrangeParameter, racewayParameter);
                }
                arrangeEngine.Arrange(regionBorders);

                //输出
                ThCreateLightToDatabaseService.SetDatabaseDefaults(racewayParameter, arrangeParameter);
                regionBorders.ForEach(o => ThCreateLightToDatabaseService.Create(o, racewayParameter, arrangeParameter));
            }
        }
        [CommandMethod("TIANHUACAD", "THCDTJ", CommandFlags.Modal)]
        public void THCDTJ()
        {
            var regionLightEdges = GetFireRegionLights();
            using (var sumEngine=new ThSumNumberEngine())
            using (var acadDatabase =AcadDatabase.Active())
            {
                sumEngine.Sum(regionLightEdges);
                sumEngine.SumInfos.ForEach(o =>
                {
                    Active.Editor.WriteLine("Id:" + o.Key.ObjectId);
                    o.Value.ForEach(v => Active.Editor.WriteLine("回路：" + v.Nubmer + "，" + "灯具数量：" + v.Count));
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THCDHL", CommandFlags.Modal)]
        public void THCDHL()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            using (var laneLineGraph =new ThLaneLineGraphEngine())
            {
                var pso = new PromptSelectionOptions()
                {
                    MessageForAdding = "\n请选择布灯的区域框线",
                };
                TypedValue[] tvs = new TypedValue[]
                {
                     new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Line)).DxfName)
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(pso, sf);
                if (result.Status == PromptStatus.OK)
                {
                    var lines = new DBObjectCollection();
                    result.Value.GetObjectIds().ForEach(o => lines.Add(acdb.Element<Line>(o)));
                    var startRes = Active.Editor.GetPoint("选择起点");
                    laneLineGraph.BuildGraph(lines, startRes.Value);
                    Func<ThEdge<ThVertex>, double> edgeWeights = e => e.Length;
                    var tryGetPaths = laneLineGraph.Graph.ShortestPathsDijkstra(edgeWeights, laneLineGraph.GraphStartVertex);
                    // query path for given vertices
                    ThVertex target = new ThVertex((lines[lines.Count - 1] as Line).StartPoint);
                    IEnumerable<ThEdge<ThVertex>> path;
                    var endRes = Active.Editor.GetPoint("选择终点");
                    if (tryGetPaths(laneLineGraph.GetVertex(endRes.Value), out path))
                    {
                        foreach (var edge in path)
                        {
                            var line = new Line(edge.Source.Position, edge.Target.Position);
                            line.ColorIndex = 1;
                            acdb.ModelSpace.Add(line);
                        }
                    }  
                }
            }
        }
        private Polyline PolylineJig(short colorIndex)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                try
                {
                    var jigger = new ThDrawPolylineJigger(colorIndex);
                    PromptResult jigRes;
                    do
                    {
                        jigRes = Active.Editor.Drag(jigger);
                        if (jigRes.Status == PromptStatus.OK)
                            jigger.AllVertexes.Add(jigger.LastVertex);
                    } while (jigRes.Status == PromptStatus.OK);
                    var wcsVertexes = jigger.WcsVertexes;
                    if (wcsVertexes.Count > 1)
                    {
                        Polyline polyline = new Polyline();
                        for (int i = 0; i < wcsVertexes.Count; i++)
                        {
                            Point3d pt3d = wcsVertexes[i];
                            Point2d pt2d = new Point2d(pt3d.X, pt3d.Y);
                            polyline.AddVertexAt(i, pt2d, 0, 0, 0);
                        }
                        return polyline;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (System.Exception ex)
                {
                    Active.Editor.WriteMessage(ex.ToString());
                }
            }
            return null;
        }
        private ThLightArrangeParameter GetUiParameters()
        {
            // From UI
            var arrangeParameter = new ThLightArrangeParameter()
            {
                Margin = 800,
                AutoCalculate = ThMEPLightingService.Instance.LightArrangeUiParameter.AutoCalculate,
                AutoGenerate = ThMEPLightingService.Instance.LightArrangeUiParameter.AutoGenerate,
                Interval = ThMEPLightingService.Instance.LightArrangeUiParameter.Interval,
                IsSingleRow = ThMEPLightingService.Instance.LightArrangeUiParameter.IsSingleRow,
                LoopNumber = ThMEPLightingService.Instance.LightArrangeUiParameter.LoopNumber,
                RacywaySpace = ThMEPLightingService.Instance.LightArrangeUiParameter.RacywaySpace,
                Width = ThMEPLightingService.Instance.LightArrangeUiParameter.Width,
            };

            // 自定义
            arrangeParameter.Margin = 800.0;
            arrangeParameter.PaperRatio = 100;
            arrangeParameter.MinimumEdgeLength = 5000;

            return arrangeParameter;
        }
        private List<ThRegionLightEdge> GetFireRegionLights()
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
                var results = new List<ThRegionLightEdge>();
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)
                {
                    var racewayParameter = new ThRacewayParameter();
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.NormalizeEx(border);
                        if (newBorder.Area > 0)
                        {
                            var lines = acdb.ModelSpace
                            .OfType<Line>()
                            .Where(l => l.Layer == racewayParameter.CenterLineParameter.Layer);
                            var centerLines = newBorder.SpatialFilter(lines.ToCollection()).Cast<Line>().ToList();

                            var blks = acdb.ModelSpace
                            .OfType<BlockReference>()
                            .Where(b => b.Layer == racewayParameter.LaneLineBlockParameter.Layer);
                            var lightBlks = newBorder.SpatialFilter(blks.ToCollection()).Cast<BlockReference>().ToList();

                            var texts = acdb.ModelSpace
                            .OfType<DBText>()
                            .Where(t => t.Layer == racewayParameter.NumberTextParameter.Layer);
                            var numberTexts = newBorder.SpatialFilter(texts.ToCollection()).Cast<DBText>().ToList();

                            var regionLightEdge = new ThRegionLightEdge
                            {
                                Lights = lightBlks,
                                Edges = centerLines,
                                Texts = numberTexts,
                                RegionBorder = newBorder,
                            };
                            results.Add(regionLightEdge);
                        }
                    });
                }
                return results;
            }
        }
        private List<ThRegionBorder> GetFireRegionBorders()
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
                var results = new List<ThRegionBorder>();
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status == PromptStatus.OK)
                {
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.NormalizeEx(border);
                        if (newBorder.Area > 0)
                        {
                            var lines = acdb.ModelSpace
                            .Where(e => ThGarageLightUtils.IsLightCableCarrierCenterline(e) || ThGarageLightUtils.IsNonLightCableCarrierCenterline(e)).ToList();
                            var dxLines = GetRegionLines(newBorder, lines.Where(l => ThGarageLightUtils.IsLightCableCarrierCenterline(l)).ToCollection());
                            var fdxLines = GetRegionLines(newBorder, lines.Where(l => ThGarageLightUtils.IsNonLightCableCarrierCenterline(l)).ToCollection());
                            if (dxLines.Count > 0)
                            {
                                var regionBorder = new ThRegionBorder
                                {
                                    RegionBorder = newBorder,
                                    DxCenterLines = dxLines,
                                    FdxCenterLines = fdxLines
                                };
                                results.Add(regionBorder);
                            }
                        }
                    });
                }
                return results;
            }
        }

        private List<Line> GetRegionLines(Polyline region, DBObjectCollection dbObjs)
        {
            return ThLaneLineEngine.Explode(region.SpatialFilter(dbObjs)).Cast<Line>().ToList();
        }
    }
}
