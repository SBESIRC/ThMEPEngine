﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
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
            List<Line> horizontalLines = new List<Line>();
            List<VerticalPipeClass> verticalPipes = new List<VerticalPipeClass>();
            List<SubmergedPumpClass> submergedPumps = new List<SubmergedPumpClass>();
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].HorizontalPipe.ForEach(e => horizontalLines.Add(e));
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].VerticalPipes.ForEach(e => verticalPipes.Add(e));
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[layer]].SubmergedPumps.ForEach(e => submergedPumps.Add(e));
            GroupPipeLineUnitByGroupedHorizontalPipe(horizontalLines, verticalPipes, submergedPumps, layer);
            CompletePipeLineUnitInfoConstructedBasedOnHorizontalPipe(verticalPipes, layer);
            ReGenerateHorizontalPipeInPipeUnit(layer);
            ConstructPipeLineUnitForUniqueVerticalPipe(verticalPipes, layer);
            ConstructConnectedArrToStoryRecordVerticalPipeRelationshipInPipeUnit(layer);
            AppendSubmergePumpToVerticalPipe(submergedPumps, layer);
        }

        /// <summary>
        /// 通过排水横管的位置关系建立部分排水单元组
        /// </summary>
        /// <param name="horizontalLines"></param>
        private void GroupPipeLineUnitByGroupedHorizontalPipe(List<Line> horizontalLines, List<VerticalPipeClass> verticalPipes, List<SubmergedPumpClass> submergedPumps, int layer)
        {
            List<List<Line>> groupedlines = new ();
            List<Line> lines = new ();
            horizontalLines.ForEach(o => lines.Add(o));
            while (lines.Count > 0)
            {
                List<Line> linesOri = new ();
                List<Line> linesTest = new ();
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
                        if (verticalPipes[j].Circle.ToRectangle().Contains(horLine.StartPoint) || verticalPipes[j].Circle.ToRectangle().Contains(horLine.EndPoint))
                        {
                            _totalPipeLineUnitsByLayerByUnit[layer][i].VerticalPipes.Add(verticalPipes[j]);
                            verticalPipes.RemoveAt(j);
                            j--;
                        }
                        else if (horLine.GetClosestPointTo(verticalPipes[j].Circle.Center, false).DistanceTo(verticalPipes[j].Circle.Center) < tolPipeToLine)
                        {
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
                    var vecStart = new Vector3d(horLine.StartPoint.X - horLine.EndPoint.X, horLine.StartPoint.Y - horLine.EndPoint.Y, 0).GetNormal().MultiplyBy(disExtendedhorLine);
                    var vecEnd = new Vector3d(horLine.EndPoint.X - horLine.StartPoint.X, horLine.EndPoint.Y - horLine.StartPoint.Y, 0).GetNormal().MultiplyBy(disExtendedhorLine);
                    var ptStart = horLine.StartPoint;
                    var ptEnd = horLine.EndPoint;
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
       
        /// <summary>
        /// 重新生成排水单元组中排水横管的几何关系
        /// </summary>
        private void ReGenerateHorizontalPipeInPipeUnit(int layer)
        {
            foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
            {
                var objs = new DBObjectCollection();
                unit.HorizontalPipes.ForEach(o => objs.Add(o));
                var processedLines = ThLaneLineMergeExtension.Merge(objs).Cast<Line>().ToList();
                unit.HorizontalPipes.Clear();
                processedLines.ForEach(o => unit.HorizontalPipes.Add(o));
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
                foreach (var unit in _totalPipeLineUnitsByLayerByUnit[layer])
                {
                    List<Line> hors = new ();
                    List<int> indexPipes = new ();
                    foreach (var hor in unit.HorizontalPipes)
                    {
                        var ptscoll = pump.Extents.ToRectangle().Vertices();
                        DBObjectCollection objs = new ();
                        objs.Add(hor);
                        if (pump.Extents.IsPointIn(hor.EndPoint) || pump.Extents.IsPointIn(hor.StartPoint))
                        {
                            hors.Add(hor);
                        }
                        else if (GetCrossObjsByPtCollection(ptscoll, objs).Count > 0)
                        {
                            hors.Add(hor);
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
                                unit.VerticalPipes[indexPipes[0]].AppendedSubmergedPump = pump;
                                cond_VertPipeFound = true;
                                break;
                            }
                            else
                            {
                                int index = 0;
                                double minDis = pump.Extents.CenterPoint().DistanceTo(unit.VerticalPipes[indexPipes[0]].Circle.Center);
                                for (int p = 1; p < indexPipes.Count; p++)
                                {
                                    if (pump.Extents.CenterPoint().DistanceTo(unit.VerticalPipes[indexPipes[p]].Circle.Center) < minDis)
                                    {
                                        minDis = pump.Extents.CenterPoint().DistanceTo(unit.VerticalPipes[indexPipes[p]].Circle.Center);
                                        index = p;
                                    }
                                }
                                unit.VerticalPipes[indexPipes[index]].AppendedSubmergedPump = pump;
                                cond_VertPipeFound = true;
                                break;
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
                        if (pump.Extents.IsPointIn(hor.EndPoint) || pump.Extents.IsPointIn(hor.StartPoint))
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
                            double nowDis = unit.VerticalPipes[i].Circle.Center.DistanceTo(pump.Extents.CenterPoint());
                            dis = dis < nowDis ? dis : nowDis;
                            ind = dis < nowDis ? ind : i;
                        }
                        if (ind > -1)
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
            Modeldatas.FloorDict[Modeldatas.FloorListDatas[0]].HorizontalPipe.ForEach(e => horLines.Add(e));
            List<DrainWellClass> addedDrainWells = new();
            DBObjectCollection objs = new();
            horLines.ForEach(o => objs.Add(o));
            foreach (var well in drainWells)
            {
                var k = well.Extents;
                k.TransformBy(Matrix3d.Scaling(scaleFactor, k.CenterPoint()));
                var crosslines = GetCrossObjsByPtCollection(k.ToRectangle().Vertices(), objs).Cast<Line>().ToList();
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
                        double dis = horLine.GetClosestPointTo(well.Extents.CenterPoint(), false).DistanceTo(well.Extents.CenterPoint());
                        if (dis < mindisinunit)
                        {
                            mindisinunit = dis;
                            hor = horLine;
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
                        double dis1 = well.Extents.CenterPoint().DistanceTo(j.Circle.Center);
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
                double tolSearchVertPipe = 200;
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
                        for (int k = 0; k < _pipeLineSystemUnits.Count; k++)
                        {
                            if (unit.VerticalPipes.Count > 0 && _pipeLineSystemUnits[k].PipeLineUnits.Count >= i && _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes.Count > 0)
                            {
                                bool cond_Search = false;
                                foreach (var pipe in _pipeLineSystemUnits[k].PipeLineUnits[i - 1].VerticalPipes)
                                {
                                    if (unit.VerticalPipes[0].Circle.Center.TransformBy(mat).DistanceTo(pipe.Circle.Center) < disSearchRange)
                                    {
                                        cond_Search = true;
                                    }
                                }
                                if (cond_Search)
                                {
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
                        for (int i = 0; i < crossedCount - 1; i++)
                        {
                            int cond_QuitCycle = 0;
                            for (int p = 0; p < crossedPipedIndexes.Count; p++)
                            {
                                if (unit.PipeLineUnits[w].VerticalPipes.Count <= crossedPipedIndexes[p] && unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] == 1)
                                {
                                    cond_QuitCycle = 1;
                                    unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] = 0;
                                }
                            }
                            if (cond_QuitCycle == 0)
                            {
                                for (int p = 0; p < crossedCount; p++)
                                {
                                    if (unit.PipeLineUnits[w].VerticalPipes[crossedPipedIndexes[p]].AppendedSubmergedPump != null)
                                    {
                                        unit.CrossLayerConnectedArrs[w][crossedPipedIndexesPreFloor[p], crossedPipedIndexes[p]] = 0;
                                    }
                                }
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
                    double dis = j.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(pipe.Circle.Center);
                    if (dis < mindis)
                    {
                        mindis = dis;
                        index = i;
                    }
                }
                var horLine = pipeLineUnit.HorizontalPipes[index];
                if (!(horLine.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(horLine.StartPoint) < 10 || horLine.GetClosestPointTo(pipe.Circle.Center, false).DistanceTo(horLine.EndPoint) < 10))
                {
                    Point3d ptmp = horLine.GetClosestPointTo(pipe.Circle.Center, false);
                    pipeLineUnit.HorizontalPipes.RemoveAt(index);
                    pipeLineUnit.HorizontalPipes.Add(new Line(horLine.StartPoint, ptmp));
                    pipeLineUnit.HorizontalPipes.Add(new Line(ptmp, horLine.EndPoint));
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
                if (line.Layer == "W-DRAI-DOME-PIPE") layer = "W-DRAI-DOME-PIPE";
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
                lines.Add(horLine);
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
                pipelineUnit.HorizontalPipes.Add(line);
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
                            double dis = pump.Extents.ToRectangle().GetClosePoint(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[i].Circle.Center).DistanceTo(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[i].Circle.Center);
                            double tol = 300;
                            for (int p = 0; p < pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes.Count; p++)
                            {
                                if (p != i)
                                {
                                    double testDis = pump.Extents.ToRectangle().GetClosePoint(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p].Circle.Center).DistanceTo(pipeLineSystemUnits[e].PipeLineUnits[j].VerticalPipes[p].Circle.Center);
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
        private List<Line> AppendIntersectedLinesToSelf(List<Line> linesOri, List<Line> linesTest, List<VerticalPipeClass> verticalPipes, List<SubmergedPumpClass> submergedPumps)
        {
            List<Line> result = new ();
            while (true)
            {
                int cond_QuitCycle = 0;
                List<Line> linesToAdd = new List<Line>();
                for (int i = 0; i < linesOri.Count; i++)
                {
                    for (int j = 0; j < linesTest.Count; j++)
                    {
                        if (IsIntersected(linesOri[i], linesTest[j], 200, verticalPipes,submergedPumps))
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
                if (pt.CreateSquare(tol).IsContains(p))
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