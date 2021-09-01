using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Garage;
using ThMEPLighting.Common;
using QuickGraph.Algorithms;
using Autodesk.AutoCAD.Runtime;
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
                var pts = ThGarageInteractionUtils.PolylineJig(colorIndex);
                if (pts.Count <= 1)
                {
                    return;
                }
                // 添加到图纸中
                var dx = ThDrawTool.CreatePolyline(pts,false);
                acdb.ModelSpace.Add(dx);
                // 设置到指定图层
                acdb.Database.AddLayer(ThGarageLightCommon.DxCenterLineLayerName);
                acdb.Database.SetLayerColor(ThGarageLightCommon.DxCenterLineLayerName, colorIndex);
                dx.Layer = ThGarageLightCommon.DxCenterLineLayerName;
            }
        }
        [CommandMethod("TIANHUACAD", "THFDXC", CommandFlags.Modal)]
        public void ThFdxc()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                short colorIndex = 1;
                var pts = ThGarageInteractionUtils.PolylineJig(colorIndex);
                if (pts.Count <= 1)
                {
                    return;
                }
                // 添加到图纸中
                var fdx = ThDrawTool.CreatePolyline(pts, false);
                acdb.ModelSpace.Add(fdx);
                // 设置到指定图层
                acdb.Database.AddLayer(ThGarageLightCommon.FdxCenterLineLayerName);
                acdb.Database.SetLayerColor(ThGarageLightCommon.FdxCenterLineLayerName, colorIndex);
                fdx.Layer = ThGarageLightCommon.FdxCenterLineLayerName;
            }
        }
        [CommandMethod("TIANHUACAD", "THCDZMBZ", CommandFlags.Modal)]
        public void THCDZMBZ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var ov = new ThAppTools.ManagedSystemVariable("GROUPDISPLAYMODE", 0))
            {
                //获取参数
                var arrangeParameter = ThGarageInteractionUtils.GetUiParameters();
                var regionBorders = ThGarageInteractionUtils.GetFireRegionBorders();
                if (regionBorders.Count == 0)
                {
                    return;
                }

                //清除选择的框线内之前布置的结果
                var clearService = new ThClearPreviouResultService();
                clearService.Clear(acadDatabase.Database, regionBorders.Select(o => o.RegionBorder).ToList());
                
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
                    var createService = new ThCreateLightToDatabaseService(o, racewayParameter, arrangeParameter);
                    createService.Create();
                    createService.ObjIds.ForEach(e => objs.Add(acadDatabase.Element<Entity>(e, true)));
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
                var arrangeParameter = ThGarageInteractionUtils.GetUiParameters();
                var racewayParameter = new ThRacewayParameter();
                var lightRegions = ThGarageInteractionUtils.GetFireRegionLights();
                if (lightRegions.Count == 0)
                {
                    return;
                }
                //Open for write
                lightRegions.ForEach(o =>
                {
                    o.Edges.ForEach(e => acadDatabase.Element<Entity>(e.Id, true));
                    o.Lights.ForEach(e => acadDatabase.Element<Entity>(e.Id, true));
                    o.Texts.ForEach(e => acadDatabase.Element<Entity>(e.Id, true));
                });

                //删除文字
                var distance = arrangeParameter.Width / 2.0 + 100 + arrangeParameter.LightNumberTextHeight / 2.0;
                for(int i=0;i<lightRegions.Count;i++)
                {
                    ThEliminateNumberTextService.Eliminate(lightRegions[i], distance);
                }

                //偏移
                lightRegions.ForEach(o =>
                {
                    //移动到原点
                    // 若图元离原点非常远（大于1E+10)，精度会受很大影响
                    // 为了规避这个问题，我们将图元移回原点
                    // 最后将处理结果还原到原始位置
                    var centerPt = o.RegionBorder.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(o.Edges.ToCollection());
                    transformer.Transform(o.Lights.ToCollection());
                    transformer.Transform(o.Texts.ToCollection());
                    transformer.Transform(o.LaneLines.ToCollection());
                    transformer.Transform(o.RegionBorder);
                    o.Transformer = transformer;
                });

                //布灯
                ThLoopArrangementEngine arrangeEngine = null;
                if (arrangeParameter.IsSingleRow)
                {
                    arrangeEngine = new ThSingleRowLoopArrangementEngine(arrangeParameter);
                }
                else
                {
                    arrangeEngine = new ThDoubleRowLoopArrangementEngine(arrangeParameter);
                }
                arrangeEngine.Arrange(lightRegions);

                //输出
                ThCreateLightToDatabaseService.SetDatabaseDefaults(racewayParameter, arrangeParameter);
                lightRegions.ForEach(o =>
                {
                    //输出到当前图纸并还原回原始位置
                    var objs = new DBObjectCollection();                    
                    var objIds = ThCreateLightToDatabaseService.CreateNumberTexts(o, racewayParameter, arrangeParameter);
                    objIds.ForEach(e => objs.Add(acadDatabase.Element<Entity>(e, true)));
                    o.Edges.ForEach(e => objs.Add(e));
                    o.Lights.ForEach(e => objs.Add(e));
                    o.LaneLines.ForEach(e => objs.Add(e));
                    o.Texts.Where(e => !e.IsErased).ForEach(e => objs.Add(e));
                    var transformer = new ThMEPOriginTransformer(o.RegionBorder.GetCentroidPoint());
                    o.Transformer.Reset(objs);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THCDTJ", CommandFlags.Modal)]
        public void THCDTJ()
        {
            var regionLightEdges = ThGarageInteractionUtils.GetFireRegionLights();
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
        [CommandMethod("TIANHUACAD", "THTestExendLine", CommandFlags.Modal)]
        public void THTestExendLine()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())            
            {
                var datas = new List<Tuple<Curve, Curve, Curve>>();
               while(true)
                {
                    var centerPER = AcHelper.Active.Editor.GetEntity("\n选择中心线");
                    if(centerPER.Status!=PromptStatus.OK)
                    {
                        break;
                    }
                    var firstPER = AcHelper.Active.Editor.GetEntity("\n选择中心线对应的1号线");
                    if (firstPER.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    var secondPER = AcHelper.Active.Editor.GetEntity("\n选择中心线对应的2号线");
                    if (secondPER.Status != PromptStatus.OK)
                    {
                        break;
                    }
                    var centerCurve = acdb.Element<Curve>(centerPER.ObjectId);
                    var firstCurve = acdb.Element<Curve>(firstPER.ObjectId);
                    var secondCurve = acdb.Element<Curve>(secondPER.ObjectId);
                    datas.Add(Tuple.Create(centerCurve, firstCurve, secondCurve));
                }
                var results = ThExtendService.Extend(datas, 5);

                results.Select(o => o.Item1.Clone() as Entity).ToList().CreateGroup(acdb.Database, 2);
                results.Select(o => o.Item2.Clone() as Entity).ToList().CreateGroup(acdb.Database, 1);
                results.Select(o => o.Item3.Clone() as Entity).ToList().CreateGroup(acdb.Database, 3);
            }
        }
    }
}
