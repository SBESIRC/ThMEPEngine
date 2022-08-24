using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.LaneLine;
using ThMEPWSS.PressureDrainageSystem.Model;
using static ThMEPWSS.PressureDrainageSystem.Service.PressureDrainageSystemDiagramService;
using static ThMEPWSS.PressureDrainageSystem.Utils.PressureDrainageUtils;
namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class PipeLineSystemUnitConstructionService
    {
        public PipeLineSystemUnitConstructionService(PressureDrainageModelData modeldatas)
        {
            Modeldatas = modeldatas;
        }
        public PressureDrainageModelData Modeldatas { get; }
        private List<PipeLineSystemUnitClass> _pipeLineSystemUnits = new ();
        private List<List<PipeLineUnit>> _totalPipeLineUnitsByLayerByUnit = new ();
        
        const string RoofCrossedId = "顶板";
        
        /// <summary>
        /// CORE:创建排水单元系统
        /// </summary>
        /// <returns></returns>
        public List<PipeLineSystemUnitClass> ConstructPipeLineSystemUnits()
        {
            int layerNumber = Modeldatas.FloorListDatas.Count;
            for (int i = 0; i < layerNumber; i++)
            {
                List<PipeLineUnit> pipeLineUnitsSingleLayer = new List<PipeLineUnit>();
                _totalPipeLineUnitsByLayerByUnit.Add(pipeLineUnitsSingleLayer);
                ProcessPipeLineUnitLayer(i);
            }
            AppendDrainWellsToVerticalPipe();
            ConstructRelationshipBetweenPipeLineUnitsInDifferentLayers(layerNumber);
            ConfirmDrainageModeForEachPipeLineUnits();
            ConstructConnectedArrForCrossLayerConnectionRelationship();
            PostProcessPressureDrainageSystemUnits(_pipeLineSystemUnits);
            ConfirmOneCrossPipePerUnit(_pipeLineSystemUnits);
            _pipeLineSystemUnits = DefineStartPtInSystemUnits(_pipeLineSystemUnits);
            return _pipeLineSystemUnits;
        }

        //子函数
        /// <summary>
        /// 处理排水单元信息
        /// </summary>
        /// <param name="layer"></param>
        private void ProcessPipeLineUnitLayer(int layer)
        {
            
            List<Horizontal> horizontalLines = new List<Horizontal>();
            List<VerticalPipeClass> verticalPipes = new List<VerticalPipeClass>();
            List<SubmergedPumpClass> submergedPumps = new List<SubmergedPumpClass>();
            List<Polyline> wrappipes = new List<Polyline>();
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].HorizontalPipe.ForEach(e => horizontalLines.Add(e));
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].VerticalPipes.ForEach(e => verticalPipes.Add(e));
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].SubmergedPumps.ForEach(e => submergedPumps.Add(e));
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].Wrappipes.ForEach(e => wrappipes.Add(e));
            GroupPipeLineUnitByGroupedHorizontalPipe(horizontalLines, verticalPipes, submergedPumps, layer);
            CompletePipeLineUnitInfoConstructedBasedOnHorizontalPipe(verticalPipes, layer);
            GenerateSupplementaryVertPipeForSubmergePump(submergedPumps,layer);
            CollectWrapPipeIntoEachUnit(wrappipes,layer);
            ConfirmDrainageModeBeforeReGenerateHorizontals(layer);
            ReGenerateHorizontalPipeInPipeUnit(layer);
            ConstructPipeLineUnitForUniqueVerticalPipe(verticalPipes, layer);
            ConstructConnectedArrToStoryRecordVerticalPipeRelationshipInPipeUnit(layer);
            AppendSubmergePumpToVerticalPipe(submergedPumps, layer);
        }
        private void GenerateSupplementaryVertPipeForSubmergePump(List<SubmergedPumpClass> submergedPumps,int layer)
        {
            //在潜水泵旁生成立管时，前面600的容差判断中找到的立管是别的系统的立管，此时在该系统重新生成
            foreach (var pump in submergedPumps)
            {
                var rec = pump.Extents;
                bool generated = false;
                for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[layer].Count; i++)
                {
                    var unit = _totalPipeLineUnitsByLayerByUnit[layer][i];
                    foreach (var horLine in unit.HorizontalPipes.Select(e => e.Line))
                    {
                        if (rec.Contains(horLine.StartPoint) && rec.Contains(horLine.EndPoint))
                        {
                            double tol = 600;
                            var pipes = unit.VerticalPipes;
                            int dd = 0;
                            foreach (var k in pipes.Select(e => e.Circle))
                            {
                                if (rec.GetClosePoint(k.Center).DistanceTo(k.Center) < tol || rec.Contains(k.Center))
                                {
                                    dd = 1;
                                    break;
                                }
                            }
                            if (dd == 0)
                            {
                                using (AcadDatabase adb = AcadDatabase.Active())
                                {
                                    double toldis = 50;
                                    Point3d ptlocPipe = rec.GetCenter();
                                    foreach (var lin in unit.HorizontalPipes)
                                    {
                                        if (rec.Contains(lin.Line.StartPoint) || rec.GetClosePoint(lin.Line.StartPoint).DistanceTo(lin.Line.StartPoint) < toldis)
                                        {
                                            ptlocPipe = lin.Line.StartPoint;
                                            break;
                                        }
                                        else if (rec.Contains(lin.Line.EndPoint) || rec.GetClosePoint(lin.Line.EndPoint).DistanceTo(lin.Line.EndPoint) < toldis)
                                        {
                                            ptlocPipe = lin.Line.EndPoint;
                                            break;
                                        }
                                    }
                                    Circle ci = new Circle(ptlocPipe, Vector3d.ZAxis, 50);
                                    ci.Layer = "W-DRAI-EQPM";

                                    double mindis = 3000;
                                    int index = -1;
                                    for (int t = 0; t < unit.HorizontalPipes.Count; t++)
                                    {
                                        double curdis = unit.HorizontalPipes[t].Line.GetClosestPointTo(ci.Center, false).DistanceTo(ci.Center);
                                        if (curdis < mindis)
                                        {
                                            mindis = curdis;
                                            index = t;
                                        }
                                    }
                                    if (index != -1)
                                    {
                                        Line line = new Line(unit.HorizontalPipes[index].Line.GetClosestPointTo(ci.Center, false), ci.Center);
                                        if (line.Length > 0)
                                        {
                                            unit.HorizontalPipes.Add(new Horizontal(line, false));
                                        }
                                    }
                                    if (!adb.Layers.Contains("AdditonPipe"))
                                        adb.Database.CreateAILayer("AdditonPipe", (short)0);
                                    ci.Layer = "AdditonPipe";
                                    var pipe = new VerticalPipeClass();
                                    pipe.Circle = ci;
                                    pipe.SameTypeIdentifiers = new List<string>();
                                    unit.VerticalPipes.Add(pipe);
                                    generated = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (generated) break;
                }
            }
        }

        /// <summary>
        /// 通过排水横管的位置关系建立部分排水单元组
        /// </summary>
        /// <param name="horizontalLines"></param>
        private void GroupPipeLineUnitByGroupedHorizontalPipe(List<Horizontal> horizontalLines, List<VerticalPipeClass> verticalPipes, List<SubmergedPumpClass> submergedPumps, int layer)
        {
            List<List<Horizontal>> groupedlines = new ();
            List<Horizontal> lines = new();
            horizontalLines./*Where(e => e.Line.Length > 1).*/ForEach(o => lines.Add(o));
            int count = 0;
            while (lines.Count > 0)
            {
                count++;
                List<Horizontal> linesOri = new ();
                List<Horizontal> linesTest = new ();
                lines.ForEach(o => linesTest.Add(o));
                linesTest.RemoveAt(0);
                linesOri.Add(lines[0]);
                linesOri = AppendIntersectedLinesToSelf(linesOri, linesTest, verticalPipes, submergedPumps);
                groupedlines.Add(linesOri);
                linesOri.ForEach(o => lines.Remove(o));
            }
            for (int i = 0; i < groupedlines.Count; i++)
            {
                PipeLineUnit pipelineUnit = new ();
                pipelineUnit.HorizontalPipes = new ();
                foreach (var line in groupedlines[i])
                {
                    pipelineUnit.HorizontalPipes.Add(line);
                }
                _totalPipeLineUnitsByLayerByUnit[layer].Add(pipelineUnit);
            }
        }

        /// <summary>
        /// 完善根据排水横管关系建立的排水单元组
        /// </summary>
        /// <param name="verticalPipes"></param>
        /// <param name="submergedPumpsB1"></param>
        private void CompletePipeLineUnitInfoConstructedBasedOnHorizontalPipe(List<VerticalPipeClass> verticalPipes, int layer)
        {
            double tolPipeToLine = 10;
            List<int> indexToRemove = new ();
            for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[layer].Count; i++)
            {
                _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes = new ();
                foreach (var horLine in _totalPipeLineUnitsByLayerByUnit[layer][i].HorizontalPipes)
                {
                    for (int j = 0; j < verticalPipes.Count; j++)
                    {
                        if (verticalPipes[j].Circle.ToRectangle().Contains(horLine.Line.StartPoint) || verticalPipes[j].Circle.ToRectangle().Contains(horLine.Line.EndPoint))
                        {
                            _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Add(verticalPipes[j]);
                            verticalPipes.RemoveAt(j);
                            j--;
                        }                      
                        else if (horLine.Line.IsIntersects(verticalPipes[j].Circle.Center.CreateSquare(verticalPipes[j].Circle.Diameter)))
                        {
                            var old_condition = horLine.Line.GetClosestPointTo(verticalPipes[j].Circle.Center, false).DistanceTo(verticalPipes[j].Circle.Center) < tolPipeToLine;
                            _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Add(verticalPipes[j]);
                            verticalPipes.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            double tolIsPolyLineSame = 1;
            double disExtendedhorLine = 50;
            for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[layer].Count; i++)
            {
                foreach (var horLine in _totalPipeLineUnitsByLayerByUnit[layer][i].HorizontalPipes)
                {
                    DBObjectCollection dbObjs = new ();
                    List<Polyline> polylines = new ();
                    verticalPipes.ForEach(o => dbObjs.Add(o.Circle.Center.CreateSquare(o.Circle.Radius)));
                    verticalPipes.ForEach(o => polylines.Add(o.Circle.Center.CreateSquare(o.Circle.Radius)));
                    var vecStart = new Vector3d(horLine.Line.StartPoint.X - horLine.Line.EndPoint.X, horLine.Line.StartPoint.Y - horLine.Line.EndPoint.Y, 0).GetNormal().MultiplyBy(disExtendedhorLine);
                    var vecEnd = new Vector3d(horLine.Line.EndPoint.X - horLine.Line.StartPoint.X, horLine.Line.EndPoint.Y - horLine.Line.StartPoint.Y, 0).GetNormal().MultiplyBy(disExtendedhorLine);
                    var ptStart = horLine.Line.StartPoint;
                    var ptEnd = horLine.Line.EndPoint;
                    ptStart = ptStart.TransformBy(Matrix3d.Displacement(vecStart));
                    ptEnd = ptEnd.TransformBy(Matrix3d.Displacement(vecEnd));
                    var rec = ThDrawTool.ToRectangle(ptStart, ptEnd, 5);
                    Point3dCollection ptcoll = rec.Vertices();
                    var selectedPolyline = GetCrossObjsByPtCollection(ptcoll, dbObjs).Cast<Polyline>().ToList();
                    if (selectedPolyline.Count > 0)
                    {
                        foreach (var ply in selectedPolyline)
                        {
                            for (int k = 0; k < polylines.Count; k++)
                            {
                                if (Math.Abs(ply.Area - polylines[k].Area) < tolIsPolyLineSame && polylines[k].GetCenter().DistanceTo(ply.GetCenter()) < tolIsPolyLineSame)
                                {
                                    _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Add(verticalPipes[k]);
                                    verticalPipes.RemoveAt(k);
                                    polylines.RemoveAt(k);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CollectWrapPipeIntoEachUnit(List<Polyline> wrappipes, int layer)
        {
            var wrappipes_spacial_index = new ThCADCoreNTSSpatialIndex(wrappipes.Select(e => e).ToCollection());
            foreach (var unit in _totalPipeLineUnitsByLayerByUnit[0])
            {
                var horlines = unit.HorizontalPipes;
                var crossed = new List<Polyline>();
                foreach (var hor in horlines)
                {
                    crossed.AddRange(wrappipes_spacial_index.SelectCrossingPolygon(hor.Line.Buffer(1)).Cast<Polyline>());
                }
                if (crossed.Count > 0)
                {
                    unit.WrapPipes.AddRange(crossed);
                }
            }
        }

        /// <summary>
        /// 在重生成横管连线之前确认排水系统单元的排水方式
        /// </summary>
        /// <param name="layer"></param>
        private void ConfirmDrainageModeBeforeReGenerateHorizontals(int layer)
        {
            if (layer == 0)
            {
                foreach (var unit in _totalPipeLineUnitsByLayerByUnit[0])
                {
                    var connectedLines = unit.HorizontalPipes.Select(e => e.Line.Clone() as Line).ToList();
                    var walls = Modeldatas.WallLines;
                    var boundaries = Modeldatas.Boundaries;
                    foreach (var line in connectedLines)
                    {
                        foreach (var bound in boundaries)
                        {
                            if (line.IntersectWithEx(bound).Count > 0)
                            {
                                unit.DrainMode = 3;//穿外墙
                                break;
                            }
                        }
                        if (unit.DrainMode == 3) break;
                    }
                    if (unit.DrainMode != 3)
                    {
                        if (unit.VerticalPipes.Count > 0 && connectedLines.Count > 0)
                        {
                            double tol_extend = 200000;
                            Line far_line = connectedLines[0];
                            double max_dis = connectedLines[0].GetMidpoint().DistanceTo(unit.VerticalPipes[0].Circle.Center);
                            if (connectedLines.Count > 1)
                            {
                                for (int i = 1; i < connectedLines.Count; i++)
                                {
                                    double dis = connectedLines[i].GetMidpoint().DistanceTo(unit.VerticalPipes[0].Circle.Center);
                                    if (dis > max_dis)
                                    {
                                        max_dis = dis;
                                        far_line = connectedLines[i];
                                    }
                                }
                            }
                            if (far_line.StartPoint.DistanceTo(unit.VerticalPipes[0].Circle.Center) > far_line.EndPoint.DistanceTo(unit.VerticalPipes[0].Circle.Center))
                            {
                                far_line = new Line(far_line.EndPoint, far_line.StartPoint);
                            }
                            Point3d far_ptstart = far_line.EndPoint;
                            far_line.Extend(false, tol_extend);
                            far_line = new Line(far_ptstart, far_line.EndPoint);
                            bool crossed_inner_wall = false;
                            foreach (var inner_wall in walls)
                            {
                                if (far_line.IntersectWithEx(inner_wall).Count > 0)
                                {
                                    crossed_inner_wall = true;
                                    break;
                                }
                            }
                            if (!crossed_inner_wall)
                            {
                                unit.DrainMode = ((int)PipeLineUnit.UnitDrainMode.CROSSOUTDOOR);//穿外墙
                            }
                            else
                            {
                                if (unit.WrapPipes.Count > 0) unit.DrainMode = ((int)PipeLineUnit.UnitDrainMode.CROSSINDOOR);//穿侧墙
                                else unit.DrainMode = ((int)PipeLineUnit.UnitDrainMode.CROSSROOF);//穿顶板
                            }

                            ////0617
                            //if (unit.WrapPipes.Count > 0) unit.DrainMode = ((int)PipeLineUnit.UnitDrainMode.CROSSINDOOR);//穿侧墙
                            //else unit.DrainMode = ((int)PipeLineUnit.UnitDrainMode.CROSSROOF);//穿顶板
                        }
                    }
                }
            }
            return;
            //以下为老代码-20220615
            //if (layer == 0)
            //{
            //    foreach (var unit in _totalPipeLineUnitsByLayerByUnit[0])
            //    {
            //        foreach (var pipe in unit.VerticalPipes)
            //        {
            //            if (pipe.Label != null && pipe.Label.Contains(RoofCrossedId))
            //            {
            //                pipe.isUnitStart = true;
            //                double cond_QuitCycle = 0;
            //                foreach (var k in unit.VerticalPipes)
            //                {
            //                    if (k.AppendedDrainWell != null)
            //                    {
            //                        unit.DrainMode = 2;//穿顶板进水井
            //                        cond_QuitCycle += 1;
            //                        break;
            //                    }
            //                }
            //                if (cond_QuitCycle == 0)
            //                {
            //                    unit.DrainMode = 1;//穿顶板
            //                }
            //                break;
            //            }
            //        }
            //        if (unit.DrainMode != 1 && unit.DrainMode != 2)
            //        {
            //            var connectedLines = unit.HorizontalPipes.Select(e => e.Clone() as Line).ToList();
            //            var walls = Modeldatas.WallLines;
            //            var boundaries = Modeldatas.Boundaries;
            //            foreach (var line in connectedLines)
            //            {
            //                foreach (var bound in boundaries)
            //                {
            //                    if (line.IntersectWithEx(bound).Count > 0)
            //                    {
            //                        unit.DrainMode = 3;//穿外墙
            //                        break;
            //                    }
            //                }
            //                if (unit.DrainMode == 3) break;
            //            }
            //            if (unit.DrainMode != 3 && connectedLines.Count > 0)
            //            {
            //                if (unit.VerticalPipes.Count > 0)
            //                {
            //                    double tol_extend = 200000;
            //                    Line far_line = connectedLines[0];
            //                    double max_dis = connectedLines[0].GetMidpoint().DistanceTo(unit.VerticalPipes[0].Circle.Center);
            //                    if (connectedLines.Count > 1)
            //                    {
            //                        for (int i = 1; i < connectedLines.Count; i++)
            //                        {
            //                            double dis = connectedLines[i].GetMidpoint().DistanceTo(unit.VerticalPipes[0].Circle.Center);
            //                            if (dis > max_dis)
            //                            {
            //                                max_dis = dis;
            //                                far_line = connectedLines[i];
            //                            }
            //                        }
            //                    }
            //                    if (far_line.StartPoint.DistanceTo(unit.VerticalPipes[0].Circle.Center) > far_line.EndPoint.DistanceTo(unit.VerticalPipes[0].Circle.Center))
            //                    {
            //                        far_line = new Line(far_line.EndPoint, far_line.StartPoint);
            //                    }
            //                    Point3d far_ptstart = far_line.EndPoint;
            //                    far_line.Extend(false, tol_extend);
            //                    far_line = new Line(far_ptstart, far_line.EndPoint);
            //                    bool crossed_inner_wall = false;
            //                    foreach (var bound in walls)
            //                    {
            //                        if (far_line.IntersectWithEx(bound).Count > 0)
            //                        {
            //                            crossed_inner_wall = true;
            //                            break;
            //                        }
            //                    }
            //                    if (crossed_inner_wall) unit.DrainMode = 4;
            //                    else unit.DrainMode = 3;
            //                }
            //                //foreach (var line in connectedLines)
            //                //{
            //                //    foreach (var bound in walls)
            //                //    {
            //                //        if (line.IntersectWithEx(bound).Count > 0)
            //                //        {
            //                //            unit.DrainMode = 4;//穿侧墙
            //                //            break;
            //                //        }
            //                //    }
            //                //    if (unit.DrainMode == 4) break;
            //                //}
            //            }
            //        }
            //        unit.DrainMode = unit.DrainMode == 0 ? 4 : unit.DrainMode;//暂时默认其它方式均为穿外墙
            //    }
            //}
        }

        /// <summary>
        /// 重新生成排水单元组中排水横管的几何关系
        /// </summary>
        private void ReGenerateHorizontalPipeInPipeUnit(int layer)
        {
            foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
            {
                var objs = new DBObjectCollection();
                unit.HorizontalPipes.ForEach(o => objs.Add(o.Line));
                var processedLines = ThLaneLineMergeExtension.Merge(objs).Cast<Line>().ToList();
                unit.OriginalHorizontalPipes = unit.HorizontalPipes.Select(e => e).ToList();
                unit.HorizontalPipes.Clear();
                processedLines.ForEach(o => unit.HorizontalPipes.Add(new Horizontal(o,false)));
            }
            foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
            {
                BreakHorizontalLineAtVerticalPipe(unit);
            }
            foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
            {
                if (unit.HorizontalPipes.Count > 1)
                {
                    ReDrawHorizontalPipeInPipeUnit(unit);
                }
            }
        }
       
        /// <summary>
        /// 为单独的立管（直接穿顶板排水）创建排水单元组
        /// </summary>
        private void ConstructPipeLineUnitForUniqueVerticalPipe(List<VerticalPipeClass> verticalPipes, int layer)
        {
            foreach (var pipe in verticalPipes)
            {
                PipeLineUnit pipelineUnit = new ();
                pipelineUnit.VerticalPipes = new ();
                pipelineUnit.VerticalPipes.Add(pipe);
                _totalPipeLineUnitsByLayerByUnit[layer].Add(pipelineUnit);
            }
        }
       
        /// <summary>
        /// 创建二维数组来记录每个排水单元内立管之间的连接关系
        /// </summary>
        /// <param name="layer"></param>
        private void ConstructConnectedArrToStoryRecordVerticalPipeRelationshipInPipeUnit(int layer)
        {
            for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[layer].Count; i++)
            {
                if (_totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Count > 0)
                {
                    List<Point3d> pointsVerticalPipe = new ();
                    _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.ForEach(o => pointsVerticalPipe.Add(o.Circle.Center));
                    int verticalPipeCount = _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Count;
                    int[,] arr = new int[verticalPipeCount, verticalPipeCount];
                    for (int k = 0; k < verticalPipeCount; k++)
                    {
                        for (int p = 0; p < verticalPipeCount; p++)
                        {
                            arr[k, p] = 1;
                        }
                    }
                    for (int j = 0; j < verticalPipeCount; j++)
                    {
                        arr[j, j] = 0;
                    }
                    _totalPipeLineUnitsByLayerByUnit[layer][i].VertPipeConnectedArr = arr;
                }
            }
        }
       
        /// <summary>
        /// 将潜水泵添加到对应立管的属性值中
        /// </summary>
        /// <param name="submergedPumps"></param>
        /// <param name="layer"></param>
        private void AppendSubmergePumpToVerticalPipe(List<SubmergedPumpClass> submergedPumps, int layer)
        {
            foreach (var pump in submergedPumps)
            {
                bool cond_VertPipeFound = false;
                //系统做个排序，优先找潜水泵与横管对接的系统而非潜水泵穿过横管的系统
                _totalPipeLineUnitsByLayerByUnit[layer] = _totalPipeLineUnitsByLayerByUnit[layer].OrderBy(e =>
                 {
                     var lines = e.OriginalHorizontalPipes;
                     var distance = double.PositiveInfinity;
                     if (lines == null) return distance;
                     var rec = pump.Extents;
                     foreach (var line in lines)
                     {
                         if (pump.Extents.Contains(line.Line.StartPoint) || pump.Extents.Contains(line.Line.EndPoint))
                         {
                             distance = 0;
                         }
                         else
                         {
                             distance = Math.Min(distance, pump.Extents.GetClosePoint(line.Line.StartPoint).DistanceTo(line.Line.StartPoint));
                             distance = Math.Min(distance, pump.Extents.GetClosePoint(line.Line.EndPoint).DistanceTo(line.Line.EndPoint));
                         }
                     }
                     return distance;
                 }).ToList();
                foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
                {
                    List<Line> hors = new ();
                    List<int> indexPipes = new ();
                    if (unit.OriginalHorizontalPipes == null) continue;
                    foreach (var hor in unit.OriginalHorizontalPipes)
                    {
                        var ptscoll = pump.Extents.Vertices();
                        DBObjectCollection objs = new ();
                        objs.Add(hor.Line);
                        if (pump.Extents.IsPointIn(hor.Line.EndPoint) || pump.Extents.IsPointIn(hor.Line.StartPoint))
                        {
                            hors.Add(hor.Line);
                        }
                        //else if (GetCrossObjsByPtCollection(ptscoll, objs).Count > 0)
                        //{
                        //    hors.Add(hor);
                        //}
                        else if (pump.Extents.Intersects(hor.Line))
                        {
                            hors.Add(hor.Line);
                        }
                    }
                    if (hors.Count > 0)//如果有横管穿过
                    {
                        foreach (var hor in hors)
                        {
                            for (int i = 0; i < unit.VerticalPipes.Count; i++)
                            {
                                var resizeFactor = 1.5;
                                double length = unit.VerticalPipes[i].Circle.Radius * resizeFactor;
                                Point3d p1 = unit.VerticalPipes[i].Circle.Center.TransformBy(Matrix3d.Displacement(new Vector3d(-length, -length, 0)));
                                Point3d p2 = unit.VerticalPipes[i].Circle.Center.TransformBy(Matrix3d.Displacement(new Vector3d(length, length, 0)));
                                Extents3d ext = new Extents3d(p1, p2);
                                if (ext.IsPointIn(hor.StartPoint) || ext.IsPointIn(hor.EndPoint))
                                {
                                    indexPipes.Add(i);
                                }
                            }
                        }
                        if (indexPipes.Count > 0)//找到对应立管
                        {
                            if (indexPipes.Count == 1)
                            {
                                if (unit.VerticalPipes[indexPipes[0]].AppendedSubmergedPump == null)
                                {
                                    unit.VerticalPipes[indexPipes[0]].AppendedSubmergedPump = pump;
                                    cond_VertPipeFound = true;
                                    break;
                                }
                            }
                            else
                            {
                                int index = 0;
                                double minDis = pump.Extents.GetCenter().DistanceTo(unit.VerticalPipes[indexPipes[0]].Circle.Center);
                                for (int p = 1; p < indexPipes.Count; p++)
                                {
                                    if (pump.Extents.GetCenter().DistanceTo(unit.VerticalPipes[indexPipes[p]].Circle.Center) < minDis)
                                    {
                                        minDis = pump.Extents.GetCenter().DistanceTo(unit.VerticalPipes[indexPipes[p]].Circle.Center);
                                        index = p;
                                    }
                                }
                                if (unit.VerticalPipes[indexPipes[0]].AppendedSubmergedPump == null)
                                {
                                    unit.VerticalPipes[indexPipes[index]].AppendedSubmergedPump = pump;
                                    cond_VertPipeFound = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (cond_VertPipeFound)
                {
                    continue;
                }
                double cond_QuitCycle = 0;
                foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
                {
                    foreach (var hor in unit.HorizontalPipes)
                    {
                        if (pump.Extents.IsPointIn(hor.Line.EndPoint) || pump.Extents.IsPointIn(hor.Line.StartPoint))
                        {
                            cond_QuitCycle += 1;
                            break;
                        }
                    }
                    if (cond_QuitCycle > 0)
                    {
                        double dis = 10000;//给一个潜水泵立管的搜索范围
                        int ind = -1;
                        for (int i = 0; i < unit.VerticalPipes.Count; i++)
                        {
                            double nowDis = unit.VerticalPipes[i].Circle.Center.DistanceTo(pump.Extents.GetCenter());
                            dis = dis < nowDis ? dis : nowDis;
                            ind = dis < nowDis ? ind : i;
                        }
                        if (ind > -1 && unit.VerticalPipes[ind].AppendedSubmergedPump==null)
                        {

                            unit.VerticalPipes[ind].AppendedSubmergedPump = pump;
                            break;

                        }
                        else
                        {
                            cond_QuitCycle = 0;
                        }
                    }
                    if (cond_QuitCycle > 0)
                    {
                        break;
                    }
                }
                if (cond_QuitCycle > 0)
                {
                    continue;
                }
            }
        }
       
        /// <summary>
        /// 将排水井信息添加到相邻近的立管属性中
        /// </summary>
        private void AppendDrainWellsToVerticalPipe()
        {
            double scaleFactor = 1.2;
            List<DrainWellClass> drainWells = new ();
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[0]].DrainWells.ForEach(e => drainWells.Add(e));
            List<Line> horLines = new();
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[0]].HorizontalPipe.ForEach(e => horLines.Add(e.Line));
            List<DrainWellClass> addedDrainWells = new();
            DBObjectCollection objs = new();
            horLines.ForEach(o => objs.Add(o));
            foreach (var well in drainWells)
            {
                var k = well.Extents;
                k.TransformBy(Matrix3d.Scaling(scaleFactor, k.GetCenter()));
                var crosslines = GetCrossObjsByPtCollection(k.Vertices(), objs).Cast<Line>().ToList();
                if (crosslines.Count > 1)
                {
                    for (int p = 0; p < crosslines.Count - 1; p++)
                    {
                        addedDrainWells.Add(well);
                    }
                }
            }
            addedDrainWells.ForEach(o => drainWells.Add(o));
            List<int> indexes = new();
            foreach (var well in drainWells)
            {
                double mindis = double.PositiveInfinity;
                int index = -1;
                Line hor = new ();
                Line hor_real = new ();
                for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[0].Count; i++)
                {
                    double mindisinunit = double.PositiveInfinity;
                    foreach (var horLine in _totalPipeLineUnitsByLayerByUnit[0][i].HorizontalPipes)
                    {
                        double dis = horLine.Line.GetClosestPointTo(well.Extents.GetCenter(), false).DistanceTo(well.Extents.GetCenter());
                        if (dis < mindisinunit)
                        {
                            mindisinunit = dis;
                            hor = horLine.Line;
                        }
                    }
                    if (mindisinunit < mindis && !indexes.Contains(i))
                    {
                        mindis = mindisinunit;
                        index = i;
                        hor_real = hor;
                    }
                }
                if (index != -1)
                {
                    indexes.Add(index);
                }
                double mindisVertical = double.PositiveInfinity;
                int indexVertical = -1;
                if (index != -1)
                {
                    for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[0][index].VerticalPipes.Count; i++)
                    {
                        var j = _totalPipeLineUnitsByLayerByUnit[0][index].VerticalPipes[i];
                        double dis1 = well.Extents.GetCenter().DistanceTo(j.Circle.Center);
                        double dis2 = hor_real.GetClosestPointTo(j.Circle.Center, false).DistanceTo(j.Circle.Center);
                        double dis = Math.Min(dis1, dis2);
                        if (dis < mindisVertical)
                        {
                            mindisVertical = dis;
                            indexVertical = i;
                        }
                    }
                }
                if (indexVertical != -1)
                {
                    _totalPipeLineUnitsByLayerByUnit[0][index].VerticalPipes[indexVertical].AppendedDrainWell = well;
                    _totalPipeLineUnitsByLayerByUnit[0][index].VerticalPipes[indexVertical].IsInitialDrainWell = true;
                    _totalPipeLineUnitsByLayerByUnit[0][index].VerticalPipes[indexVertical].IsNexttoDainWell = true;
                    _totalPipeLineUnitsByLayerByUnit[0][index].DrainWellPipeIndex = indexVertical;
                }
            }
        }
      
        /// <summary>
        /// 将每层的不同排水单元跨楼层对应起来组合成单元排水系统
        /// </summary>
        /// <param name="layerNumber"></param>
        private void ConstructRelationshipBetweenPipeLineUnitsInDifferentLayers(int layerNumber)
        {

            for (int i = 0; i < _totalPipeLineUnitsByLayerByUnit[0].Count; i++)
            {
                PipeLineSystemUnitClass pipeLineSystemUnit = new ();
                pipeLineSystemUnit.PipeLineUnits = new ();
                pipeLineSystemUnit.LayerNumbers = layerNumber;
                pipeLineSystemUnit.FloorLocPoints = new ();
                for (int j = 0; j < layerNumber; j++)
                {
                    pipeLineSystemUnit.FloorLocPoints.Add(Modeldatas.FloorLocPoints[j]);
                }
                pipeLineSystemUnit.PipeLineUnits.Add(_totalPipeLineUnitsByLayerByUnit[0][i]);
                pipeLineSystemUnit.DrainWellPipeIndex = _totalPipeLineUnitsByLayerByUnit[0][i].DrainWellPipeIndex;
                _pipeLineSystemUnits.Add(pipeLineSystemUnit);
            }
            if (layerNumber > 1)
            {
                double disSearchRange = 10000;//给一个潜水泵立管的搜索范围
                double tolSearchVertPipe = 100;
                for (int i = 1; i < layerNumber; i++)
                {
                    Point3d pt0 = Modeldatas.FloorLocPoints[i - 1];
                    Point3d pti = Modeldatas.FloorLocPoints[i];
                    Vector3d vec = new Vector3d(pti.X - pt0.X, pti.Y - pt0.Y, 0);
                    vec = vec.Negate();
                    Matrix3d mat = Matrix3d.Displacement(vec);
                    for (int j = 0; j < _totalPipeLineUnitsByLayerByUnit[i].Count; j++)
                    {                  
                        double cond_QuitCycle = 0;
                        var unit = _totalPipeLineUnitsByLayerByUnit[i][j];
                        var originalHors = unit.OriginalHorizontalPipes;
                        if (originalHors == null)
                        {
                            if (i == layerNumber - 1)
                                continue;
                            else
                            {
                                unit.OriginalHorizontalPipes = new List<Horizontal>();
                                originalHors = new List<Horizontal>();
                            }
                        }
                        var originalHorsSpacialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
                        originalHorsSpacialIndex = new ThCADCoreNTSSpatialIndex(originalHors.Select(e => e.Line.Buffer(1)).ToCollection());
                        for (int k = 0; k < _pipeLineSystemUnits.Count; k++)
                        {
                            if (unit.VerticalPipes.Count > 0 && _pipeLineSystemUnits[k].PipeLineUnits.Count >= i && _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes.Count > 0)
                            {
                                //bool cond_Search = false;
                                //foreach (var pipe in _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes)
                                //{
                                //    if (unit.VerticalPipes[0].Circle.Center.TransformBy(mat).DistanceTo(pipe.Circle.Center) < disSearchRange)
                                //    {
                                //        cond_Search = true;
                                //    }
                                //}
                                var pump = new SubmergedPumpClass();
                                foreach (var curPipe in unit.VerticalPipes)
                                {
                                    if (curPipe.AppendedSubmergedPump != null) pump = curPipe.AppendedSubmergedPump;
                                }
                                var pumpline = new Line();
                                foreach (var line in originalHors.Select(e => e.Line))
                                {
                                    var rec = pump.Extents;
                                    if (rec == null) continue;
                                    if (rec.Contains(line.StartPoint) && rec.Contains(line.EndPoint))
                                    {
                                        pumpline = line;
                                    }
                                }
                                if (pumpline.Length > 0)
                                {
                                    var connectedLines = FindSeriesLine(pumpline.StartPoint, originalHors.Select(e => e.Line).ToList());
                                    connectedLines.AddRange(FindSeriesLine(pumpline.EndPoint, originalHors.Select(e => e.Line).ToList()));
                                    var connectedLinesSpacialIndex = new ThCADCoreNTSSpatialIndex(connectedLines.Select(e => e.Buffer(1)).ToCollection());
                                    foreach (var curPipe in unit.VerticalPipes)
                                    {
                                        if (connectedLinesSpacialIndex.SelectCrossingPolygon(curPipe.Circle.Center.CreateSquare(curPipe.Circle.Diameter * 2)).Cast<Polyline>().Count() == 0)
                                        {
                                            curPipe.CanUsedToJudgeCrossLayer = false;
                                        }
                                    }
                                }
                                foreach (var curPipe in unit.VerticalPipes)
                                {
                                    if (!curPipe.CanUsedToJudgeCrossLayer) continue;
                                    if (curPipe.IsGenerated) continue;
                                    var piperec = curPipe.Circle.Center.CreateSquare(curPipe.Circle.Diameter * 2);
                                    if (originalHorsSpacialIndex.SelectCrossingPolygon(piperec).Count > 0 || originalHorsSpacialIndex.SelectAll().Count==0)
                                    {
                                        foreach (var parPipe in _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes)
                                        {                                    
                                            var cond_distance = curPipe.Circle.Center.TransformBy(mat).DistanceTo(parPipe.Circle.Center) < tolSearchVertPipe;
                                            var id_curpipe = curPipe.Identifier == null ? "" : curPipe.Identifier;
                                            var id_parpipe = parPipe.Identifier == null ? "" : parPipe.Identifier;
                                            var cond_match_mark = id_curpipe.Equals(id_parpipe);
                                            if (cond_distance && cond_match_mark)
                                            {
                                                cond_QuitCycle += 1;
                                                if (_pipeLineSystemUnits[k].PipeLineUnits.Count == i)
                                                {
                                                    _pipeLineSystemUnits[k].PipeLineUnits.Add(unit);
                                                }
                                                else
                                                {
                                                    int num_a = unit.HorizontalPipes.Count;
                                                    for (int w = 0; w < num_a; w++)
                                                    {
                                                        _pipeLineSystemUnits[k].PipeLineUnits[i].HorizontalPipes.Add(unit.HorizontalPipes[w]);
                                                    }
                                                    int num_b = unit.VerticalPipes.Count;
                                                    for (int w = 0; w < num_b; w++)
                                                    {
                                                        _pipeLineSystemUnits[k].PipeLineUnits[i].VerticalPipes.Add(unit.VerticalPipes[w]);
                                                    }
                                                    int[,] arr1 = _pipeLineSystemUnits[k].PipeLineUnits[i].VertPipeConnectedArr;
                                                    int[,] arr2 = unit.VertPipeConnectedArr;
                                                    int num = arr1.GetLength(0) + arr2.GetLength(0);
                                                    int[,] arr = new int[num, num];
                                                    for (int w = 0; w < arr1.GetLength(0); w++)
                                                    {
                                                        for (int m = 0; m < arr1.GetLength(0); m++)
                                                        {
                                                            if (arr1[w, m] == 1)
                                                            {
                                                                arr[w, m] = 1;
                                                            }
                                                        }
                                                    }
                                                    for (int w = 0; w < arr2.GetLength(0); w++)
                                                    {
                                                        for (int m = 0; m < arr2.GetLength(0); m++)
                                                        {
                                                            if (arr2[w, m] == 1)
                                                            {
                                                                arr[w + arr1.GetLength(0), m + arr1.GetLength(0)] = 1;
                                                            }
                                                        }
                                                    }
                                                    _pipeLineSystemUnits[k].PipeLineUnits[i].VertPipeConnectedArr = arr;
                                                }
                                                break;
                                            }
                                            if (cond_QuitCycle > 0) { break; }
                                        }
                                        if (cond_QuitCycle > 0) { break; }
                                    }
                                }
                                if (/*cond_Search*/false)
                                {
                                    foreach (var ppe in unit.VerticalPipes)
                                    {
                                        var p = ppe.Circle.Center;
                                        if (p.DistanceTo(new Point3d(611313.5, 341095.9, 0)) < 10)
                                        {
                                            ;
                                        }
                                    }
                                    foreach (var curPipe in unit.VerticalPipes)
                                    {
                                        foreach (var parPipe in _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes)
                                        {
                                            if (curPipe.Circle.Center.TransformBy(mat).DistanceTo(parPipe.Circle.Center) < tolSearchVertPipe)
                                            {
                                                cond_QuitCycle += 1;
                                                if (_pipeLineSystemUnits[k].PipeLineUnits.Count == i)
                                                {
                                                    _pipeLineSystemUnits[k].PipeLineUnits.Add(unit);
                                                }
                                                else
                                                {
                                                    int num_a = unit.HorizontalPipes.Count;
                                                    for (int w = 0; w < num_a; w++)
                                                    {
                                                        _pipeLineSystemUnits[k].PipeLineUnits[i].HorizontalPipes.Add(unit.HorizontalPipes[w]);
                                                    }
                                                    int num_b = unit.VerticalPipes.Count;
                                                    for (int w = 0; w < num_b; w++)
                                                    {
                                                        _pipeLineSystemUnits[k].PipeLineUnits[i].VerticalPipes.Add(unit.VerticalPipes[w]);
                                                    }
                                                    int[,] arr1 = _pipeLineSystemUnits[k].PipeLineUnits[i].VertPipeConnectedArr;
                                                    int[,] arr2 = unit.VertPipeConnectedArr;
                                                    int num = arr1.GetLength(0) + arr2.GetLength(0);
                                                    int[,] arr = new int[num, num];
                                                    for (int w = 0; w < arr1.GetLength(0); w++)
                                                    {
                                                        for (int m = 0; m < arr1.GetLength(0); m++)
                                                        {
                                                            if (arr1[w, m] == 1)
                                                            {
                                                                arr[w, m] = 1;
                                                            }
                                                        }
                                                    }
                                                    for (int w = 0; w < arr2.GetLength(0); w++)
                                                    {
                                                        for (int m = 0; m < arr2.GetLength(0); m++)
                                                        {
                                                            if (arr2[w, m] == 1)
                                                            {
                                                                arr[w + arr1.GetLength(0), m + arr1.GetLength(0)] = 1;
                                                            }
                                                        }
                                                    }
                                                    _pipeLineSystemUnits[k].PipeLineUnits[i].VertPipeConnectedArr = arr;
                                                }
                                                break;
                                            }
                                            if (cond_QuitCycle > 0) { break; }
                                        }
                                        if (cond_QuitCycle > 0) { break; }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断每个排水系统单元的最终排水方式
        /// </summary>
        private void ConfirmDrainageModeForEachPipeLineUnits()
        {
            foreach (var unit in _pipeLineSystemUnits)
            {
                foreach (var pipe in unit.PipeLineUnits[0].VerticalPipes)
                {
                    if (pipe.AppendedDrainWell != null)
                    {
                        unit.DrainWell = pipe.AppendedDrainWell;
                    }
                }
                unit.DrainageMode = unit.PipeLineUnits[0].DrainMode != 0 ? unit.PipeLineUnits[0].DrainMode : 1;
            }
        }

        /// <summary>
        /// 判断每个排水系统单元的最终排水方式
        /// </summary>
        private void ConfirmDrainageModeForEachPipeLineUnitsBackUp()
        {
            foreach (var unit in _pipeLineSystemUnits)
            {
                foreach (var pipe in unit.PipeLineUnits[0].VerticalPipes)
                {
                    if (pipe.AppendedDrainWell != null)
                    {
                        unit.DrainWell = pipe.AppendedDrainWell;
                    }
                }
                foreach (var pipe in unit.PipeLineUnits[0].VerticalPipes)
                {
                    if (pipe.Label != null && pipe.Label.Contains(RoofCrossedId))
                    {
                        pipe.isUnitStart = true;
                        double cond_QuitCycle = 0;
                        foreach (var k in unit.PipeLineUnits[0].VerticalPipes)
                        {
                            if (k.AppendedDrainWell != null)
                            {
                                unit.DrainageMode = 2;//穿顶板进水井
                                cond_QuitCycle += 1;
                                break;
                            }
                        }
                        if (cond_QuitCycle == 0)
                        {
                            unit.DrainageMode = 1;//穿顶板
                        }
                        break;
                    }
                    else
                    {
                        var connectedLines= unit.PipeLineUnits[0].HorizontalPipes;
                        var walls = Modeldatas.WallLines;
                        var boundaries = Modeldatas.Boundaries;
                        foreach (var line in connectedLines)
                        {
                            foreach (var bound in boundaries)
                            {
                                if (line.Line.IntersectWithEx(bound).Count>0)
                                {
                                    unit.DrainageMode = 3;
                                    break;
                                }
                            }
                            if (unit.DrainageMode == 3) break;
                        }
                        if (unit.DrainageMode != 3 && connectedLines.Count>0)
                        {
                            foreach (var line in connectedLines)
                            {
                                foreach (var bound in walls)
                                {
                                    if (line.Line.IntersectWithEx(bound).Count > 0)
                                    {
                                        unit.DrainageMode = 4;
                                        break;
                                    }
                                }
                                if (unit.DrainageMode == 4) break;
                            }
                        }
                    }
                }
                unit.DrainageMode = unit.DrainageMode == 0 ? 3 : unit.DrainageMode;//暂时默认其它方式均为穿外墙
            }
        }
      
        /// <summary>
        /// 创建识别跨层之间管路连接关系的二维数组
        /// </summary>
        private void ConstructConnectedArrForCrossLayerConnectionRelationship()
        {
            foreach (var unit in _pipeLineSystemUnits)
            {
                unit.CrossLayerConnectedArrs = new List<int[,]>();
                int[,] ArrZeroOne = new int[1, unit.PipeLineUnits[0].VerticalPipes.Count];
                for (int i = 0; i < unit.PipeLineUnits[0].VerticalPipes.Count; i++)
                {
                    if (unit.PipeLineUnits[0].VerticalPipes[i].Label != null && unit.PipeLineUnits[0].VerticalPipes[i].Label.Contains(RoofCrossedId))
                    {
                        ArrZeroOne[0, i] = 1;
                    }
                    else if (unit.PipeLineUnits[0].VerticalPipes[i].AppendedDrainWell != null)
                    {
                        ArrZeroOne[0, i] = 2;
                    }
                    else
                    {
                        ArrZeroOne[0, i] = 0;
                    }
                }
                unit.CrossLayerConnectedArrs.Add(ArrZeroOne);
                double tolCrossedPipe = 300;//判断上下楼层立管对应的容差
                for (int i = 1; i < unit.PipeLineUnits.Count; i++)
                {
                    int[,] arr = new int[unit.PipeLineUnits[i - 1].VerticalPipes.Count, unit.PipeLineUnits[i].VerticalPipes.Count];
                    Point3d ptlocpre = unit.FloorLocPoints[i - 1];
                    Point3d ptloccurrent = unit.FloorLocPoints[i];
                    Vector3d vec = new Vector3d(ptloccurrent.X - ptlocpre.X, ptloccurrent.Y - ptlocpre.Y, 0);
                    Matrix3d mat = Matrix3d.Displacement(vec);
                    for (int j = 0; j < unit.PipeLineUnits[i - 1].VerticalPipes.Count; j++)
                    {
                        for (int k = 0; k < unit.PipeLineUnits[i].VerticalPipes.Count; k++)
                        {
                            Point3d pt1 = unit.PipeLineUnits[i - 1].VerticalPipes[j].Circle.Center;
                            Point3d pt2 = unit.PipeLineUnits[i].VerticalPipes[k].Circle.Center;
                            pt1 = pt1.TransformBy(mat);
                            if (pt1.DistanceTo(pt2) < tolCrossedPipe)
                            {
                                arr[j, k] = 1;
                            }
                            else
                            {
                                arr[j, k] = 0;
                            }
                        }
                    }
                    unit.CrossLayerConnectedArrs.Add(arr);
                }
            }
        }
       
        /// <summary>
        /// 对排水系统单元数据后处理
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void PostProcessPressureDrainageSystemUnits(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            AppendDrainWellsToSystemUnits(pipeLineSystemUnits);
            RemoveUnitWhileNoVerticalPipe(pipeLineSystemUnits);
            RemoveUnitWhileNoSubmergePump(pipeLineSystemUnits);
            CalculateSubmergedPipeDiameter(pipeLineSystemUnits);
            RecogniseParallelVerticalPipesInTheSameWell(pipeLineSystemUnits);
            return;
        }
       
        /// <summary>
        /// 确保每个排水系统单元每层只有一个穿越楼层的立管
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void ConfirmOneCrossPipePerUnit(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            foreach (var unit in pipeLineSystemUnits)
            {
                for (int w = 0; w < unit.CrossLayerConnectedArrs.Count; w++)
                {
                    var arr = unit.CrossLayerConnectedArrs[w];
                    int length_a = arr.GetLength(0);
                    int length_b = arr.GetLength(1);
                    int crossedCount = 0;
                    List<int> crossedPipedIndexes = new List<int>();
                    List<int> crossedPipedIndexesPreFloor = new List<int>();
                    for (int i = 0; i < length_a; i++)
                    {
                        for (int k = 0; k < length_b; k++)
                        {
                            if (arr[i, k] == 1)
                            {
                                crossedCount += 1;
                                crossedPipedIndexesPreFloor.Add(i);
                                crossedPipedIndexes.Add(k);
                            }
                        }
                    }
                    if (crossedCount > 1)
                    {
                        bool processed = false;
                        for (int i = 0; i < crossedCount - 1; i++)
                        {
                            int cond_QuitCycle = 0;
                            for (int p = 0; p < crossedPipedIndexes.Count; p++)
                            {
                                if (unit.PipeLineUnits[w].VerticalPipes.Count <= crossedPipedIndexes[p] && unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] == 1)
                                {
                                    cond_QuitCycle = 1;
                                    processed = true;
                                    unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] = 0;
                                }
                            }
                            if (cond_QuitCycle == 0)
                            {
                                for (int p = 0; p < crossedCount; p++)
                                {
                                    if (unit.PipeLineUnits[w].VerticalPipes[crossedPipedIndexes[p]].AppendedSubmergedPump != null)
                                    {
                                        processed = true;
                                        unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] = 0;
                                    }
                                }
                            }
                        }
                        if (!processed)
                        {
                            for (int i = 1; i < crossedPipedIndexes.Count; i++)
                            {
                                unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[i], crossedPipedIndexes[i]] = 0;
                            }
                        }
                    }
                }
            }
        }
      
        /// <summary>
        /// 定义排水系统单元的起始点
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        /// <returns></returns>
        private List<PipeLineSystemUnitClass> DefineStartPtInSystemUnits(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            for (int i = 0; i < pipeLineSystemUnits.Count; i++)
            {
                pipeLineSystemUnits[i].SameUnitsStartPt = new List<Point3d>();
                int index = FindIndexStartVerticalPipe(pipeLineSystemUnits[i]) > -1 ? FindIndexStartVerticalPipe(pipeLineSystemUnits[i]) : 0;
                pipeLineSystemUnits[i].SameUnitsStartPt.Add(pipeLineSystemUnits[i].PipeLineUnits[0].VerticalPipes[index].Circle.Center);
            }
            return pipeLineSystemUnits;
        }

        //二级子函数
        /// <summary>
        /// 如果立管处的排水横管线无端点，在此处打断设置端点
        /// </summary>
        /// <param name="pipeLineUnit"></param>
        private void BreakHorizontalLineAtVerticalPipe(PipeLineUnit pipeLineUnit)
        {
            foreach (var pipe in pipeLineUnit.VerticalPipes)
            {
                int index = -1;
                double mindis = 10000;
                for (int i = 0; i < pipeLineUnit.HorizontalPipes.Count; i++)
                {
                    var j = pipeLineUnit.HorizontalPipes[i];
                    double dis = j.Line.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(pipe.Circle.Center);
                    if (dis < mindis)
                    {
                        mindis = dis;
                        index = i;
                    }
                }
                var horLine = pipeLineUnit.HorizontalPipes[index];
                if (!(horLine.Line.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(horLine.Line.StartPoint) < 10 || horLine.Line.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(horLine.Line.EndPoint) < 10))
                {
                    Point3d ptmp = horLine.Line.GetClosestPointTo(pipe.Circle.Center, false);
                    pipeLineUnit.HorizontalPipes.RemoveAt(index);
                    pipeLineUnit.HorizontalPipes.Add(new Horizontal(new Line(horLine.Line.StartPoint, ptmp),false));
                    pipeLineUnit.HorizontalPipes.Add(new Horizontal(new Line(ptmp, horLine.Line.EndPoint),false));
                }
            }
        }
       
        /// <summary>
        /// 重绘排水单元中的横管，将具有相同端点且端点不连接立管的两条横管直线合并为一条
        /// </summary>
        /// <param name="pipelineUnit"></param>
        private void ReDrawHorizontalPipeInPipeUnit(PipeLineUnit pipelineUnit)
        {
            string layer = "0";
            foreach (var line in pipelineUnit.HorizontalPipes)
            {
                if (line.Line.Layer == "W-DRAI-DOME-PIPE") layer = "W-DRAI-DOME-PIPE";
            }
            double tol = 100;//两横管直线具有相同端点的容差
            List<Point3d> ptsVtcalPipe = new List<Point3d>();
            List<Line> lines = new List<Line>();
            foreach (var pipe in pipelineUnit.VerticalPipes)
            {
                ptsVtcalPipe.Add(pipe.Circle.Center);
            }
            foreach (var horLine in pipelineUnit.HorizontalPipes)
            {
                lines.Add(horLine.Line);
            }
            double cond_QuitCycle = 0;
            do
            {
                cond_QuitCycle = 0;
                double tolConnectedline = 10;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines.Count > 1)
                    {
                        if (!IsAdjacentToPointList(lines[i].StartPoint, ptsVtcalPipe, tol))
                        {
                            List<Line> listLinetemp = new List<Line>();
                            lines.ForEach(o => listLinetemp.Add(o));
                            listLinetemp.RemoveAt(i);
                            int indextemp = GetClosestLineIndex(lines[i].StartPoint, listLinetemp, tolConnectedline);
                            if (indextemp != -1)
                            {
                                var pt1 = listLinetemp[indextemp].StartPoint;
                                var pt2 = listLinetemp[indextemp].EndPoint;
                                Point3d ptmp1 = pt1.DistanceTo(lines[i].StartPoint) > pt2.DistanceTo(lines[i].StartPoint) ? pt2 : pt1;
                                Point3d ptmp2 = pt1.DistanceTo(lines[i].StartPoint) > pt2.DistanceTo(lines[i].StartPoint) ? pt1 : pt2;
                                if (!IsAdjacentToPointList(ptmp1, ptsVtcalPipe, tol))
                                {
                                    cond_QuitCycle += 1;
                                    Line line = new Line(lines[i].EndPoint, ptmp2);
                                    if (i < indextemp)
                                    {
                                        lines.RemoveAt(indextemp + 1);
                                        lines.RemoveAt(i);
                                    }
                                    else
                                    {
                                        lines.RemoveAt(i);
                                        lines.RemoveAt(indextemp);
                                    }
                                    lines.Add(line);
                                    break;
                                }
                            }
                        }
                        if (!IsAdjacentToPointList(lines[i].EndPoint, ptsVtcalPipe, tol))
                        {
                            List<Line> listLinetemp = new List<Line>();
                            lines.ForEach(o => listLinetemp.Add(o));
                            listLinetemp.RemoveAt(i);
                            int indextemp = GetClosestLineIndex(lines[i].EndPoint, listLinetemp, tolConnectedline);
                            if (indextemp != -1)
                            {
                                var pt1 = listLinetemp[indextemp].StartPoint;
                                var pt2 = listLinetemp[indextemp].EndPoint;
                                Point3d ptmp1 = pt1.DistanceTo(lines[i].EndPoint) > pt2.DistanceTo(lines[i].EndPoint) ? pt2 : pt1;
                                Point3d ptmp2 = pt1.DistanceTo(lines[i].EndPoint) > pt2.DistanceTo(lines[i].EndPoint) ? pt1 : pt2;
                                if (!IsAdjacentToPointList(ptmp1, ptsVtcalPipe, tol))
                                {
                                    cond_QuitCycle += 1;
                                    Line line = new Line(lines[i].StartPoint, ptmp2);
                                    if (i < indextemp)
                                    {
                                        lines.RemoveAt(indextemp + 1);
                                        lines.RemoveAt(i);
                                    }
                                    else
                                    {
                                        lines.RemoveAt(i);
                                        lines.RemoveAt(indextemp);
                                    }
                                    lines.Add(line);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            while (cond_QuitCycle > 0);
            pipelineUnit.HorizontalPipes.Clear();
            if (layer == "W-DRAI-DOME-PIPE")
            {
                lines.ForEach(o => o.Layer = layer);
            }
            foreach (var line in lines)
            {
                pipelineUnit.HorizontalPipes.Add(new Horizontal(line,false));
            }
        }
       
        /// <summary>
        /// 将排水井信息补充到排水系统单元
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void AppendDrainWellsToSystemUnits(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            foreach (var systemUnit in pipeLineSystemUnits)
            {
                foreach (var pipe in systemUnit.PipeLineUnits[0].VerticalPipes)
                {
                    if (pipe.AppendedDrainWell != null)
                    {
                        foreach (var p in systemUnit.PipeLineUnits[0].VerticalPipes)
                        {
                            p.AppendedDrainWell = pipe.AppendedDrainWell;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 移除没有立管的排水系统单元
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void RemoveUnitWhileNoVerticalPipe(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            for (int i = 0; i < pipeLineSystemUnits.Count; i++)
            {
                int verticalPipeCount = 0;
                foreach (var unit in pipeLineSystemUnits[i].PipeLineUnits)
                {
                    if (unit.VerticalPipes.Count > 0)
                    {
                        verticalPipeCount = 1;
                        break;
                    }
                }
                if (verticalPipeCount == 0)
                {
                    pipeLineSystemUnits.RemoveAt(i);
                    i--;
                }
            }
            return;
        }
        
        /// <summary>
        /// 移除整个系统无潜水泵的单元组
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void RemoveUnitWhileNoSubmergePump(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            for (int i = 0; i < pipeLineSystemUnits.Count; i++)
            {
                int pumpCount = 0;
                foreach (var unit in pipeLineSystemUnits[i].PipeLineUnits)
                {
                    foreach (var pipe in unit.VerticalPipes)
                    {
                        if (pipe.AppendedSubmergedPump != null)
                        {
                            pumpCount += 1;
                            break;
                        }
                    }
                    if (pumpCount > 0)
                    {
                        break;
                    }
                }
                if (pumpCount == 0)
                {
                    pipeLineSystemUnits.RemoveAt(i);
                    i--;
                }
            }
            return;
        }
       
        /// <summary>
        /// 计算潜水泵立管管径
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void CalculateSubmergedPipeDiameter(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            foreach (var systemunit in pipeLineSystemUnits)
            {
                foreach (var unit in systemunit.PipeLineUnits)
                {
                    foreach (var pipe in unit.VerticalPipes)
                    {
                        if (pipe.AppendedSubmergedPump != null)
                        {
                            var pump = pipe.AppendedSubmergedPump;
                            pipe.AppendusedpumpCount = CalculateUsedPump(pump.Allocation);
                            if (pump.paraQ != 0)
                            {
                                pipe.Diameter = CalculatePipeDiameter(pump.paraQ);
                            }
                            pipe.totalQ = pipe.AppendusedpumpCount * pump.paraQ;
                            pipe.totalUsedPump = pipe.AppendusedpumpCount;
                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// 识别出一个集水井中多台潜水泵的情况
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        private void RecogniseParallelVerticalPipesInTheSameWell(List<PipeLineSystemUnitClass> pipeLineSystemUnits)
        {
            for (int e = 0; e < pipeLineSystemUnits.Count; e++)
            {
                for (int j = 0; j < pipeLineSystemUnits[e].PipeLineUnits.Count; j++)
                {
                    for (int i = 0; i < pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes.Count; i++)
                    {
                        var pump = pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[i].AppendedSubmergedPump;
                        if (pump == null)
                        {
                            continue;
                        }
                        if (pump.PumpCount > 1 && pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes.Count > 1)
                        {
                            List<VerticalPipeClass> adjacentPipes = new List<VerticalPipeClass>();
                            List<int> adjacentPipeIndexes = new List<int>();
                            double dis = pump.Extents.GetClosePoint(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[i].Circle.Center).DistanceTo(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[i].Circle.Center);
                            double tol = 300;
                            for (int p = 0; p < pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes.Count; p++)
                            {
                                if (p != i)
                                {
                                    double testDis = pump.Extents.GetClosePoint(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p].Circle.Center).DistanceTo(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p].Circle.Center);
                                    if (Math.Abs(dis - testDis) < tol)
                                    {
                                        adjacentPipes.Add(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p]);
                                        adjacentPipeIndexes.Add(p);
                                    }
                                    else if (pump.Extents.IsPointIn(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p].Circle.Center))
                                    {
                                        adjacentPipes.Add(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p]);
                                        adjacentPipeIndexes.Add(p);
                                    }
                                }
                            }
                            ProcessVerticalPipesInSameWell(pipeLineSystemUnits, ref e, ref j, ref i, ref adjacentPipes, ref adjacentPipeIndexes);
                        }
                    }
                }
            }
        }
       
        /// <summary>
        /// 对同一集水井中的立管进行数据处理
        /// </summary>
        /// <param name="pipeLineSystemUnits"></param>
        /// <param name="pipeLineSystemUnitsIndex"></param>
        /// <param name="unitIndex"></param>
        /// <param name="systemUnitIndex"></param>
        /// <param name="adjacentPipes"></param>
        /// <param name="adjacentPipeIndexes"></param>
        private void ProcessVerticalPipesInSameWell(List<PipeLineSystemUnitClass> pipeLineSystemUnits, ref int pipeLineSystemUnitsIndex, ref int unitIndex, ref int systemUnitIndex, ref List<VerticalPipeClass> adjacentPipes, ref List<int> adjacentPipeIndexes)
        {
            if (adjacentPipes.Count == 0)
            {
                return;
            }
            adjacentPipeIndexes.Sort();
            List<int> indexestoremove = new();
            adjacentPipeIndexes.ForEach(index => indexestoremove.Add(index));
            for (int r = 0; r < adjacentPipes.Count; r++)
            {
                int adjacentremainedindex = -1;
                double mindis = double.PositiveInfinity;
                for (int p = 0; p < pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes.Count; p++)
                {
                    if (pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes[p].Circle.Center.DistanceTo(pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes[adjacentPipeIndexes[adjacentPipeIndexes.Count - 1]].Circle.Center) < mindis && !indexestoremove.Contains(p))
                    {
                        adjacentremainedindex = p;
                    }
                }
                int iniverticalcount = pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes.Count;
                if (adjacentremainedindex != -1)
                {
                    if (unitIndex > 0)
                    {
                        for (int p = 0; p < pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex - 1].VerticalPipes.Count; p++)
                        {
                            if (pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex][p, adjacentPipeIndexes[adjacentPipeIndexes.Count - 1]] == 1)
                            {
                                pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex][p, adjacentremainedindex] = 1;
                            }
                        }
                    }
                    if (pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits.Count > unitIndex + 1)
                    {
                        for (int p = 0; p < pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes.Count; p++)
                        {
                            if (pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex + 1][p, adjacentPipeIndexes[adjacentPipeIndexes.Count - 1]] == 1)
                            {
                                pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex + 1][p, adjacentremainedindex] = 1;
                            }
                        }
                    }
                }
                pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VerticalPipes.RemoveAt(adjacentPipeIndexes[adjacentPipeIndexes.Count - 1]);
                var arr1 = pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VertPipeConnectedArr;
                int specialIndex = adjacentPipeIndexes[adjacentPipeIndexes.Count - 1];
                int[,] arr_tmp1 = RemoveDimensionDataInArr(arr1, 0, specialIndex);
                int[,] arr_tmp2 = RemoveDimensionDataInArr(arr_tmp1, 1, specialIndex);
                pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits[unitIndex].VertPipeConnectedArr = arr_tmp2;
                pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex] = RemoveDimensionDataInArr(pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex], 1, specialIndex);
                if (pipeLineSystemUnits[pipeLineSystemUnitsIndex].PipeLineUnits.Count > unitIndex + 1)
                {
                    pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex + 1] = RemoveDimensionDataInArr(pipeLineSystemUnits[pipeLineSystemUnitsIndex].CrossLayerConnectedArrs[unitIndex + 1], 0, specialIndex);
                }
                adjacentPipeIndexes.RemoveAt(adjacentPipeIndexes.Count - 1);
            }
        }
        
        //工具函数
        /// <summary>
        /// 从B列表中找出与A列表相交的直线并添加至A列表
        /// </summary>
        /// <param name="linesOri"></param>
        /// <param name="linesTest"></param>
        /// <returns></returns>
        private List<Horizontal> AppendIntersectedLinesToSelf(List<Horizontal> linesOri, List<Horizontal> linesTest, List<VerticalPipeClass> verticalPipes, List<SubmergedPumpClass> submergedPumps)
        {
            List<Horizontal> result = new ();
            while (true)
            {
                int cond_QuitCycle = 0;
                List<Horizontal> linesToAdd = new List<Horizontal>();
                for (int i = 0; i < linesOri.Count; i++)
                {
                    for (int j = 0; j < linesTest.Count; j++)
                    {
                        if (IsIntersected(linesOri[i].Line, linesTest[j].Line, 200, verticalPipes,submergedPumps))
                        {
                            cond_QuitCycle += 1;
                            linesToAdd.Add(linesTest[j]);
                        }
                    }
                }
                if (cond_QuitCycle == 0)
                {
                    break;
                }
                linesToAdd = linesToAdd.Distinct().ToList();
                linesToAdd.ForEach(o => linesTest.Remove(o));
                linesToAdd.ForEach(o => linesOri.Add(o));
            }
            linesOri.ForEach(o => result.Add(o));
            return result;
        }
       
        /// <summary>
        /// 判断一个点是否与列表中的点邻近
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="pts"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        private bool IsAdjacentToPointList(Point3d pt, List<Point3d> pts, double tol)
        {
            foreach (var p in pts)
            {
                if (pt.CreateSquare(tol).EntityContains(p))
                {
                    return true;
                }
            }
            return false;
        }
       
        /// <summary>
        /// 找出列表中与指定点最近的直线索引值
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        private int GetClosestLineIndex(Point3d pt, List<Line> lines, double tol)
        {
            double mindis = lines[0].GetDistToPoint(pt, false);
            int index = 0;
            if (lines.Count > 0)
            {
                for (int i = 1; i < lines.Count; i++)
                {
                    if (lines[i].GetDistToPoint(pt, false) < mindis)
                    {
                        mindis = lines[i].GetDistToPoint(pt, false);
                        index = i;
                    }
                }
            }
            if (mindis > tol)
            {
                return -1;
            }
            return index;
        }
      
        /// <summary>
        /// 删除二维数组[,]中的某一行/列
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="dimension"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int[,] RemoveDimensionDataInArr(int[,] arr, int dimension, int index)
        {
            int length_0 = dimension == 0 ? arr.GetLength(0) - 1 : arr.GetLength(0);
            int length_1 = dimension == 1 ? arr.GetLength(1) - 1 : arr.GetLength(1);
            int[,] arr_tmp = new int[length_0, length_1];
            if (dimension == 0)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        if (i < index)
                        {
                            arr_tmp[i, j] = arr[i, j];
                        }
                        else if (i > index)
                        {
                            arr_tmp[i - 1, j] = arr[i, j];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        if (j < index)
                        {
                            arr_tmp[i, j] = arr[i, j];
                        }
                        else if (j > index)
                        {
                            arr_tmp[i, j-1] = arr[i, j];
                        }
                    }
                }
            }
            return arr_tmp;
        }
    }
}