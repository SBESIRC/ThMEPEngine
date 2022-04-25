using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundWaterSystem.Command;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
using ThMEPWSS.UndergroundWaterSystem.ViewModel;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class OptimizedDataReader
    {
        public OptimizedDataReader(Extents3d outBound, Point3d start,bool loginfo)
        {
            OutBoundVertices = outBound.ToRectangle().Vertices();
            StartPoint = start;
            LogInfo = loginfo;
            Initialize();
        }
        public Point3d StartPoint;
        public bool LogInfo;
        public Point3dCollection OutBoundVertices = new Point3dCollection();
        public List<Line> PipeLines = new List<Line>();//横管
        public List<ThMarkModel> Marks = new List<ThMarkModel>();//标注
        public List<ThRiserModel> Riser = new List<ThRiserModel>();//立管
        public List<ThDimModel> Dims = new List<ThDimModel>();//管径
        public List<ThValveModel> Valves = new List<ThValveModel>();//阀门
        public List<ThFlushPointModel> FlushPoints = new List<ThFlushPointModel>();//冲洗点位
        public ThFloorInfo GetDatas(Polyline Bound,int index)
        {
            var info = new ThFloorInfo();
            info.PipeLines = PipeLines.Where(e => Bound.Contains(e.GetCenter())).ToList();
            info.MarkList = Marks.Where(e => Bound.Contains(e.Poistion)).ToList();
            info.RiserList = Riser.Where(e => Bound.Contains(e.Position))
                .Select(e => { e.FloorIndex = index; return e; }).ToList();
            info.DimList = Dims.Where(e => Bound.Contains(e.Position)).ToList();
            info.ValveList = Valves.Where(e => Bound.Contains(e.Point)).ToList();
            info.FlushPointList = FlushPoints.Where(e => Bound.Contains(e.Point)).ToList();
            return info;
        }
        private void Initialize()
        {
            if (LogInfo) LogInfos("开始提取横管");
            //提取横管
            var pipeExtractionService = new ThPipeExtractionService();
            PipeLines = pipeExtractionService.GetPipeLines();
            var originalLine = ThUndergroundWaterSystemUtils.FindStartLine(StartPoint, PipeLines);
            PipeLines = PipeLines.Where(e => e.ColorIndex.Equals(originalLine.ColorIndex)).ToList();
            if (LogInfo) LogInfos("横管提取完成");
            //提取立管
            var riserExtractionService = new ThRiserExtracionService();
            Riser = riserExtractionService.GetRiserModelList(PipeLines, OutBoundVertices);
            if (LogInfo) LogInfos("立管提取完成");
            //提取标记
            var markExtractionService = new ThMarkExtractionService();
            Marks = markExtractionService.GetMarkModelList(OutBoundVertices);
            if (LogInfo) LogInfos("标记提取完成");
            //提取管径
            var dimExtractionService = new ThDimExtractionService();
            Dims = dimExtractionService.GetDimModelList(OutBoundVertices);
            if (LogInfo) LogInfos("管径提取完成");
            //提取阀门&&冲洗点位
            var valveExtractionService = new ThOtherDataExtractionService();
            Valves = valveExtractionService.GetValveModelList(OutBoundVertices);
            FlushPoints = valveExtractionService.GetFlushPointList(OutBoundVertices);
            if (LogInfo) LogInfos("阀门冲洗点位提取完成");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
    }
}
