﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using NetTopologySuite.Geometries;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using MPChromosome = ThParkingStall.Core.InterProcess.Chromosome;
using MPGene = ThParkingStall.Core.InterProcess.Gene;
using Chromosome = ThMEPArchitecture.ParkingStallArrangement.Algorithm.Chromosome;
using Gene = ThMEPArchitecture.ParkingStallArrangement.Algorithm.Gene;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.MPartitionLayout;
using Dreambuild.AutoCAD;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using Linq2Acad;
using ThParkingStall.Core.OInterProcess;

namespace ThMEPArchitecture.MultiProcess
{
    public static class Converter
    {
        // Get DataWraper AndInit two static class
        public static DataWraper GetDataWraper(OuterBrder outerBrder, ParkingStallArrangementViewModel vm)
        {
            var dataWraper = new DataWraper();
            dataWraper.UpdateVMParameter(vm);
            VMStock.Init(dataWraper);
            dataWraper.UpdateInterParameter(outerBrder);
            InterParameter.Init(dataWraper);
            return dataWraper;
        }
        public static DataWraper GetDataWraper(LayoutData layoutData, ParkingStallArrangementViewModel vm,bool AddSegLines = true)
        {
            var dataWraper = new DataWraper();
            dataWraper.UpdateVMParameter(vm);
            VMStock.Init(dataWraper);
            dataWraper.UpdateInterParameter(layoutData);
            InterParameter.Init(dataWraper);
            if (AddSegLines)
            {
                var newSegs = InterParameterEx.AddSegLines();
                using (AcadDatabase acad = AcadDatabase.Active())
                {
                    if (acad.Layers.Contains("添加分区线"))
                        newSegs.ForEach(l => l.ToDbLine(2, "添加分区线").AddToCurrentSpace());
                }
                layoutData.ProcessSegLines(layoutData.SegLines.Concat(newSegs).ToList(), false, true);
                dataWraper.UpdateInterParameter(layoutData);
                InterParameter.Init(dataWraper);
            }
            return dataWraper;
        }

        public static DataWraper GetDataWraper(OLayoutData oLayoutData,ParkingStallArrangementViewModel vm)
        {
            OCached.Clear();//清空cache
            MCompute.CatchedTimes = 0;
            var dataWraper = new DataWraper();
            dataWraper.UpdateVMParameter(vm);
            VMStock.Init(dataWraper);
            dataWraper.UpdateOInterParameter(oLayoutData);
            OInterParameter.Init(dataWraper);
            return dataWraper;
        }
        private static void UpdateInterParameter(this DataWraper dataWraper, LayoutData layoutData)
        {
            var interParamWraper = new InterParamWraper();
            interParamWraper.TotalArea = layoutData.WallLine;
            interParamWraper.SegLines = layoutData.SegLines;
            interParamWraper.Buildings = layoutData.Buildings;
            interParamWraper.BoundingBoxes = layoutData.BoundingBoxes;
            interParamWraper.Ramps = layoutData.Ramps;
            interParamWraper.SeglineIndexList = layoutData.SeglineIndexList;
            interParamWraper.LowerUpperBound = layoutData.LowerUpperBound;
            interParamWraper.OuterBuildingIdxs = layoutData.OuterBuildingIdxs;
            interParamWraper.SeglineConnectToBound = layoutData.SeglineConnectToBound;
            interParamWraper.SegLineIntSecNode = layoutData.SegLineIntSecNode;
            dataWraper.interParamWraper = interParamWraper;
        }

        private static void UpdateOInterParameter(this DataWraper dataWraper,OLayoutData oLayoutData)
        {
            var oWraper = new OParamWraper();
            oWraper.TotalArea = oLayoutData.WallLine;
            oWraper.SegLines = oLayoutData.SegLines;
            oWraper.Buildings = oLayoutData.Buildings;
            oWraper.Ramps = oLayoutData.Ramps;
            oWraper.seglineIndex = oLayoutData.SeglineIndex;
            oWraper.borderLines = oLayoutData.BorderLines;
            dataWraper.oParamWraper = oWraper;
        }
        private static void UpdateInterParameter(this DataWraper dataWraper, OuterBrder outerBrder)
        {
            var interParamWraper = new InterParamWraper();
            interParamWraper.TotalArea = outerBrder.WallLine.ToNTSPolygon().RemoveHoles();
            interParamWraper.SegLines = outerBrder.SegLines.Select(segLine => segLine.ExtendLineEx(1, 3)).
                Select(l => l.ToNTSLineSegment()).ToList();
            var entities = outerBrder.BuildingObjs.ExplodeBlocks();
            var buildingBounds = new List<LineString>();
            foreach (var ent in entities)
            {
                if (ent is Polyline pline)
                {
                    var bound = pline.ToNTSLineString();
                    buildingBounds.Add(bound);
                }
            }
            interParamWraper.Buildings = buildingBounds.GetPolygons();
            interParamWraper.Buildings = interParamWraper.Buildings.Select(build => build.RemoveHoles()).ToList();
            interParamWraper.BoundingBoxes = new List<Polygon>();
            foreach (BlockReference blk in outerBrder.BuildingWithoutRampObjs)
            {
                interParamWraper.BoundingBoxes.Add(blk.GetRect().ToNTSPolygon());
            }
            interParamWraper.BoundingBoxes = interParamWraper.BoundingBoxes.Select(box => box.RemoveHoles()).ToList();
            interParamWraper.Ramps = outerBrder.GetRamps();
            interParamWraper.SeglineIndexList = outerBrder.SegLines.Select(l =>l.ToNTSLineSegment()).ToList().GetSegLineIntsecList();
            interParamWraper.LowerUpperBound = interParamWraper.SegLines.GetLowerUpperBound(dataWraper);
            dataWraper.interParamWraper = interParamWraper;

        }
        public static void UpdateVMParameter(this DataWraper datawraper, ParkingStallArrangementViewModel vm)
        {
            //平行车位尺寸,长度
            datawraper.ParallelSpotLength = vm.ParallelSpotLength; //mm
            //平行车位尺寸,宽度
            datawraper.ParallelSpotWidth = vm.ParallelSpotWidth; //mm
            //垂直车位尺寸, 长度
            datawraper.VerticalSpotLength = vm.VerticalSpotLength; //mm
            //垂直车位尺寸, 宽度
            datawraper.VerticalSpotWidth = vm.VerticalSpotWidth; //mm
            //道路宽
            datawraper.RoadWidth = vm.RoadWidth; //mm
            //平行于车道方向柱子尺寸
            datawraper.ColumnSizeOfParalleToRoad = vm.ColumnSizeOfParalleToRoad; //mm
            //垂直于车道方向柱子尺寸
            datawraper.ColumnSizeOfPerpendicularToRoad = vm.ColumnSizeOfPerpendicularToRoad; //mm
            //柱子完成面尺寸
            datawraper.ColumnAdditionalSize = vm.ColumnAdditionalSize; //mm
            //柱子完成面是否影响车道净宽
            datawraper.ColumnAdditionalInfluenceLaneWidth = vm.ColumnAdditionalInfluenceLaneWidth;
            //最大柱间距,需要改成柱间距
            datawraper.ColumnWidth = vm.ColumnWidth; //mm
            //背靠背模块：缩进200
            datawraper.DoubleRowModularDecrease200 = vm.DoubleRowModularDecrease200;
            //尽端环通
            datawraper.AllowLoopThroughEnd = vm.AllowLoopThroughEnd;
            //背靠背长度限制
            datawraper.DisAllowMaxLaneLength = vm.DisAllowMaxLaneLength;
            //背靠背模块：柱子沿车道法向偏移距离
            datawraper.ColumnShiftDistanceOfDoubleRowModular = vm.ColumnShiftDistanceOfDoubleRowModular; //mm
            //背靠背模块是否使用中柱
            datawraper.MidColumnInDoubleRowModular = vm.MidColumnInDoubleRowModular;
            //单排模块：柱子沿车道法向偏移距离
            datawraper.ColumnShiftDistanceOfSingleRowModular = vm.ColumnShiftDistanceOfSingleRowModular; //mm
            //以下两个参数尚未传入
            ////车位碰撞参数D1（侧面）
            //datawraper.D1;
            ////车位碰撞参数D2（尾部）
            //datawraper.D2;
            //迭代次数
            datawraper.IterationCount = vm.IterationCount;
            //排布模式
            datawraper.RunMode = ParameterStock.RunMode;
            //横向优先_纵向车道计算长度调整_背靠背模块
            datawraper.LayoutScareFactor_Intergral = ParameterStock.LayoutScareFactor_Intergral;
            //横向优先_纵向车道计算长度调整_车道近段垂直生成相邻车道模块
            datawraper.LayoutScareFactor_Adjacent = ParameterStock.LayoutScareFactor_Adjacent;
            //横向优先_纵向车道计算长度调整_建筑物之间的车道生成模块
            datawraper.LayoutScareFactor_betweenBuilds = ParameterStock.LayoutScareFactor_betweenBuilds;
            //横向优先_纵向车道计算长度调整_孤立的单排垂直式模块
            datawraper.LayoutScareFactor_SingleVert = ParameterStock.LayoutScareFactor_SingleVert;
            //孤立的单排垂直式模块生成条件控制_非单排模块车位预计数与孤立单排车位的比值
            datawraper.SingleVertModulePlacementFactor = ParameterStock.SingleVertModulePlacementFactor;
            datawraper.SpeedUpMode = vm.SpeedUpMode;
        }
        public static List<Ramp> GetRamps(this OuterBrder outerBrder)
        {
            // 注意：目前坡道用的还是外包框，还未更改
            var ramps = new List<Ramp>();
            outerBrder.RampLists.ForEach(r => ramps.Add(new Ramp(r.InsertPt.ToNTSPoint(), r.Ramp.ToNTSPolygon())));
            //ramps.ForEach(ramp => ramp.Area.RemoveHoles());
            return ramps;
        }
        public static ChromosomeCollection GetChromosomeCollection(this List<Chromosome> population)
        {
            var collection = new ChromosomeCollection();
            foreach(var chromosome in population)
            {
                collection.Append(chromosome.GetMPChromosome());
            }
            return collection;
        }
        public static MPGene GetMPGene(this Gene gene)
        {
            return new MPGene(gene.Value, gene.VerticalDirection, gene.StartValue, gene.EndValue);
        }
        public static MPChromosome GetMPChromosome(this Chromosome chromosome)
        {
            var mpChromosome = new MPChromosome();
            foreach(var gene in chromosome.Genome)
            {
                mpChromosome.Append(gene.GetMPGene());
            }
            return mpChromosome;
        }


        public static List<(double, double)> GetLowerUpperBound(this List<LineSegment> SegLines, DataWraper dataWraper)
        {
            var interParamWraper = dataWraper.interParamWraper;
            var TotalArea = interParamWraper.TotalArea;
            var Buildings = interParamWraper.Buildings;
            var Ramps = interParamWraper.Ramps;
            var lowerUpperBound = new List<(double, double)>();
            var vaildSegs = SegLines.GetVaildSegLines(TotalArea,0);//获取有效分区线,边界上线取最大值
            var ObstacleSpatialIndex = new MNTSSpatialIndex(Buildings);
            var boundLineSpatialIndex = new MNTSSpatialIndex(TotalArea.Shell.ToLineStrings().Cast<Geometry>());
            var RampSpatialIndex = new MNTSSpatialIndex(Ramps.Select(ramp => ramp.Area));
            double HorzSize = TotalArea.Coordinates.Max(c => c.X) - TotalArea.Coordinates.Min(c => c.X);
            double VertSize = TotalArea.Coordinates.Max(c => c.Y) - TotalArea.Coordinates.Min(c => c.Y);
            for (int i = 0; i < vaildSegs.Count; i++)
            {
                if(RampSpatialIndex.SelectCrossingGeometry(new LineString(new Coordinate[] { SegLines[i].P0, SegLines[i].P1 })).Count > 0)
                {
                    if (SegLines[i].IsVertical())
                        lowerUpperBound.Add((SegLines[i].P0.X, SegLines[i].P0.X));
                    else
                        lowerUpperBound.Add((SegLines[i].P0.Y, SegLines[i].P0.Y));
                }
                var vaildSeg = vaildSegs[i];
                if (vaildSeg.IsVertical())
                {
                    var MinMaxValue = vaildSeg.GetMinMaxValue(HorzSize, ObstacleSpatialIndex, boundLineSpatialIndex);
                    var value = vaildSeg.P0.X;
                    lowerUpperBound.Add((MinMaxValue.Item1 + value, MinMaxValue.Item2 + value));
                }
                else
                {
                    var MinMaxValue = vaildSeg.GetMinMaxValue(VertSize, ObstacleSpatialIndex, boundLineSpatialIndex);
                    var value = vaildSeg.P0.Y;
                    lowerUpperBound.Add((MinMaxValue.Item1 + value, MinMaxValue.Item2 + value));
                }
            }
            return lowerUpperBound;
        }
        public static (double,double) GetMinMaxValue(this LineSegment vaildSeg,double bufferSize, MNTSSpatialIndex ObstacleSpatialIndex, MNTSSpatialIndex boundLineSpatialIndex)
        {
            double maxVal = 0;
            double minVal = 0;
            var vaildSegLineStr = new LineString(new Coordinate[] { vaildSeg.P0, vaildSeg.P1 });
            var posBuffer = vaildSeg.GetHalfBuffer(bufferSize, true);
            var posObstacles = ObstacleSpatialIndex.SelectCrossingGeometry(posBuffer).Cast<Polygon>();
            if (posObstacles.Count() > 0)
            {
                var multiPolygon = new MultiPolygon(posObstacles.ToArray());
                maxVal = vaildSegLineStr.Distance(multiPolygon) - (VMStock.RoadWidth / 2);//返回最近距离- 半车道宽
            }
            else
            {
                var boundLineStrs = boundLineSpatialIndex.SelectCrossingGeometry(posBuffer).Cast<LineString>();
                if (boundLineStrs.Count() > 0)
                {
                    maxVal = boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离 
                }
            }
            var negBuffer = vaildSeg.GetHalfBuffer(bufferSize, false);
            var negObstacles = ObstacleSpatialIndex.SelectCrossingGeometry(negBuffer).Cast<Polygon>();
            if (negObstacles.Count() > 0)
            {
                var multiPolygon = new MultiPolygon(negObstacles.ToArray());
                minVal = -vaildSegLineStr.Distance(multiPolygon) + (VMStock.RoadWidth / 2);//返回最近距离- 半车道宽
            }
            else
            {
                var boundLineStrs = boundLineSpatialIndex.SelectCrossingGeometry(negBuffer).Cast<LineString>();
                if (boundLineStrs.Count() > 0)
                {
                    minVal = -boundLineStrs.Max(lstr => vaildSegLineStr.Distance(lstr));//返回最大距离
                }
            }
            return (Math.Min(0,minVal),Math.Max(0,maxVal));
        }
    }
}
