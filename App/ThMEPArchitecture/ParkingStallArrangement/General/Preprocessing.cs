﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using Autodesk.AutoCAD.EditorInput;
using ThMEPArchitecture.ViewModel;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    public static class Preprocessing
    {
        public static bool GetOuterBorder(AcadDatabase acadDatabase, out OuterBrder outerBrder, Serilog.Core.Logger Logger)
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder _outerBrder, Logger);
            outerBrder = _outerBrder;

            if (outerBrder.SegLines.Count == 0)//分割线数目为0
            {
                Active.Editor.WriteMessage("分割线不存在！");
                return false;
            }
            if (!rstDataExtract)
            {
                return false;
            }
            return true;
        }
        public static bool DataPreprocessing(OuterBrder outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, 
            Serilog.Core.Logger Logger = null, bool isDirectlyArrange = false, bool usePline = true)
        {
            gaPara = new GaParameter();
            layoutPara = new LayoutParameter();
            var area = outerBrder.WallLine;
            var areas = new List<Polyline>() { area };
            var buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingLines);
            gaPara = new GaParameter(outerBrder.SegLines);

            var usedLines = new HashSet<int>();
            var maxVals = new List<double>();
            var minVals = new List<double>();

            var seglineDic = new Dictionary<int, Line>();
            var index = 0;
            foreach (var line in outerBrder.SegLines)
            {
                seglineDic.Add(index++, line);
            }
            var rstSplit = WindmillSplit.Split(isDirectlyArrange, area, seglineDic, buildLinesSpatialIndex, ref maxVals, ref minVals, 
                out Dictionary<int, List<int>> seglineIndexDic, out int segAreasCnt);
            if(!rstSplit)
            {
                return false;
            }
            gaPara.Set(outerBrder.SegLines, maxVals, minVals);

            var ptDic = Intersection.GetIntersection(seglineDic);//获取分割线的交点
            var linePtDic = Intersection.GetLinePtDic(ptDic);
            var intersectPtCnt = ptDic.Count;//交叉点数目
            var directionList = new Dictionary<int, bool>();//true表示纵向，false表示横向
            foreach (var num in ptDic.Keys)

            {
                var random = new Random();
                var flag = random.NextDouble() < 0.5;
                directionList.Add(num, flag);//默认给全横向
            }
            layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, outerBrder.SegLines, ptDic, directionList, linePtDic, 
                seglineIndexDic, segAreasCnt, usePline, Logger);
            return true;

        }
    }
}
