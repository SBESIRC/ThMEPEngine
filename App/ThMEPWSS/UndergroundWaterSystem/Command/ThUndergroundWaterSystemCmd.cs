using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPWSS.Common;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using ThMEPWSS.UndergroundWaterSystem.ViewModel;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Command
{
    public class ThUndergroundWaterSystemCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterSystemInfoModel InfoModel { set; get; }
        public bool LogInfo = false;
        public ThUndergroundWaterSystemCmd()
        {
            ActionName = "生成";
            CommandName = "THDXJSXT";
        }
        public void Dispose() { }

        public int GetFloorIndex(Point3d startPt)
        {
            int index = -1;
            for (int i = 0; i < InfoModel.FloorList.Count; i++)
            {
                var floor = InfoModel.FloorList[i];
                if (floor.FloorArea.Contains(startPt))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        public override void SubExecute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    //选择一个起点
                    var startPt = ThUndergroundWaterSystemUtils.SelectPoint("\n请选择水管起点位置\n");
                    if (startPt.IsEqualTo(new Point3d())) return;
                    startPt = startPt.ToPoint2D().ToPoint3d();
                    //选择一个基点,用来插入系统图
                    var basePt = ThUndergroundWaterSystemUtils.SelectPoint("\n请选择系统图插入基点\n");
                    if (basePt.IsEqualTo(new Point3d())) return;
                    if (InfoModel.FloorList.Count == 0) return;
                    //计时
                    Stopwatch swatch = new Stopwatch();
                    swatch.Start();
                    basePt = basePt.ToPoint2D().ToPoint3d();
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    //导入必要的模块
                    ThImportService thImportService = new ThImportService();
                    thImportService.Import();
                    //
                    if (LogInfo) LogInfos("计算开始", true);
                    //楼层框定定位点
                    var StoryFrameBasePoint = new List<Point3d>();
                    var frames = FramedReadUtil.ReadAllFloorFramed();
                    foreach (var frame in frames)
                        StoryFrameBasePoint.Add(new Point3d(frame.datumPoint.X, frame.datumPoint.Y, 0));
                    var tmps = new List<Point3d>();
                    foreach (var pl in InfoModel.FloorList.Select(e => e.FloorArea))
                        foreach (var p in StoryFrameBasePoint)
                            if (pl.Contains(p)) { tmps.Add(p); break; }
                    StoryFrameBasePoint = tmps;
                    //读取楼层信息_速度优化
                    var StartMarkInfo = "";
                    Extents3d ext = new Extents3d();
                    for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    {
                        var pl = CreatePolyFromPoints(
                            InfoModel.FloorList[i].FloorArea.Vertices().Cast<Point3d>().ToArray());
                        ext.AddExtents(pl.GeometricExtents);
                    }
                    OptimizedDataReader dataReader = new OptimizedDataReader(ext, startPt, LogInfo);
                    StartMarkInfo = dataReader.StartMarkInfo;
                    if (LogInfo) LogInfos("数据提取完成");
                    for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    {
                        InfoModel.FloorList[i].FloorInfo = dataReader.GetDatas(CreatePolyFromPoints(
                            InfoModel.FloorList[i].FloorArea.Vertices().Cast<Point3d>().ToArray()), i);
                    }
                    if (LogInfo) LogInfos("数据分层完成");
                    //初始化提取的数据中与管线、阀门等绘图相关的图层信息
                    ThLayerInitializeService layerInitializeService = new ThLayerInitializeService();
                    layerInitializeService.Initialize(InfoModel);
                    //处理楼层立管数据
                    var floorHandleService = new ThFloorHandleService();
                    floorHandleService.MatchRiserMark(InfoModel.FloorList);
                    var risers = floorHandleService.MergeRiser(InfoModel.FloorList, StoryFrameBasePoint);
                    if (LogInfo) LogInfos("处理完立管数据");
                    //处理横管
                    var riserpoints = new List<Point3d>();
                    foreach (var riser in risers)
                        riserpoints.AddRange(riser.RiserPts);
                    for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    {
                        ThPipeLineHandleService pipeLineHandleService = new ThPipeLineHandleService();
                        InfoModel.FloorList[i].FloorInfo.PipeLines = pipeLineHandleService.ConnectLinesWithSpacing(
                            InfoModel.FloorList[i].FloorInfo.PipeLines, riserpoints);
                    }
                    //构造树
                    var pipeTree = new ThPipeTree(startPt, InfoModel.FloorList, risers, mt);
                    if (pipeTree.RootNode == null)
                    {
                        Active.Editor.WriteLine("请输入有效数据：请检测横管等元素图层是否正确，请确认横管是否为天正元素，请确认是否有其它数据格式问题……");
                        return;
                    }
                    if (LogInfo) LogInfos("开始绘图");
                    //通过树绘制系统图 
                    var systemMapeService = new ThSystemMapService();
                    systemMapeService.StartMarkInfo = StartMarkInfo;
                    systemMapeService.FloorHeight = InfoModel.FloorLineSpace;
                    systemMapeService.FloorList = InfoModel.FloorList;
                    systemMapeService.RiserList = risers;
                    systemMapeService.Mt = mt;
                    systemMapeService.DrawMap(basePt, pipeTree);
                    swatch.Stop();
                    if (LogInfo) LogInfos("绘图完成");
                    Active.Editor.WriteLine("系统图绘制完成，用时" + swatch.Elapsed.TotalSeconds + "秒。");
                    return;
                }
            }
            catch (Exception ex)
            {
                 Active.Editor.WriteMessage(ex.Message);
            }
        }
    }
}
