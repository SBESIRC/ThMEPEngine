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
        [CommandMethod("TIANHUACAD", "THCDZMBZ", CommandFlags.Modal)]
        public void THCDZMBZ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                //输入参数
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

                //创建块的索引，偏于后期查询
                arrangeParameter.LightBlockQueryService= ThQueryLightBlockService.Create(
                        regionBorders.Select(o => o.RegionBorder).ToList());
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
            var results = new List<ThRegionLightEdge>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var pso = new PromptSelectionOptions()
                {
                    MessageForAdding = "\n请选择布灯的区域框线",
                };
                TypedValue[] tvs = new TypedValue[]
                {
                     new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Polyline)).DxfName)
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(pso, sf);
                if (result.Status == PromptStatus.OK)
                {
                    var racewayParameter = new ThRacewayParameter();
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.Normalize(border);
                        var lineTvs = new TypedValueList
                            {
                                { (int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName},
                                { (int)DxfCode.Start, RXClass.GetClass(typeof(Line)).DxfName},
                                { (int)DxfCode.LayerName, racewayParameter.CenterLineParameter.Layer}
                            };
                        var centerLines = newBorder.GetEntities(lineTvs).Cast<Line>().ToList();

                        var blkTvs = new TypedValueList
                            {
                                { (int)DxfCode.ExtendedDataRegAppName, ThGarageLightCommon.ThGarageLightAppName},
                                { (int)DxfCode.Start, RXClass.GetClass(typeof(BlockReference)).DxfName},
                                { (int)DxfCode.LayerName, racewayParameter.LaneLineBlockParameter.Layer}
                            };
                        var lightBlks = newBorder.GetEntities(blkTvs).Cast<BlockReference>().ToList();

                        var regionLightEdge = new ThRegionLightEdge
                        {
                            RegionBorder = newBorder,
                            Lights = lightBlks,
                            Edges = centerLines
                        };
                        results.Add(regionLightEdge);
                    });
                }
            }
            return results;
        }
        private List<ThRegionBorder> GetFireRegionBorders()
        {
            var results = new List<ThRegionBorder>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var pso = new PromptSelectionOptions()
                {
                    MessageForAdding = "\n请选择布灯的区域框线",
                };
                TypedValue[] tvs = new TypedValue[]
                {
                     new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Polyline)).DxfName)
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(pso, sf);
                if (result.Status == PromptStatus.OK)
                {
                    result.Value.GetObjectIds().ForEach(o =>
                    {
                        var border = acdb.Element<Polyline>(o);
                        var newBorder = ThMEPFrameService.Normalize(border);
                        var dxLines = GetRegionLines(newBorder,
                            new List<string> { ThGarageLightCommon.DxCenterLineLayerName },
                            new List<Type> { typeof(Line), typeof(Polyline) });
                        var fdxLines = GetRegionLines(newBorder,
                        new List<string> { ThGarageLightCommon.FdxCenterLineLayerName },
                        new List<Type> { typeof(Line), typeof(Polyline) });
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
                    });
                }
            }
            return results;
        }
        private List<Line> GetRegionLines(Polyline region, List<string> layers, List<Type> types)
        {
            var curves = region.GetRegionCurves(layers, types)
                .Where(k => k is Line || k is Polyline)
                .Cast<Curve>().ToCollection();
            return ThLaneLineEngine.Explode(curves).Cast<Line>().ToList();
        }
    }
}
