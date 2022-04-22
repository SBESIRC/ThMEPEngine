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
        public ThUndergroundWaterSystemCmd()
        {
            ActionName = "生成";
            CommandName = "THDXJSXT";
        }
        public void Dispose()
        {
            //
        }
        public int GetFloorIndex(Point3d startPt)
        {
            int index = -1;
            for(int i = 0; i < InfoModel.FloorList.Count;i++)
            {
                var floor = InfoModel.FloorList[i];
                if(floor.FloorArea.Contains(startPt))
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
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    startPt = startPt.ToPoint2D().ToPoint3d();
                    //选择一个基点,用来插入系统图
                    var basePt = ThUndergroundWaterSystemUtils.SelectPoint("\n请选择体统图基点\n");
                    if (basePt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    basePt = basePt.ToPoint2D().ToPoint3d();
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    //导入必要的模块
                    ThImportService thImportService = new ThImportService();
                    thImportService.Import();
                    //提取每个楼层的所需要的元素
                    if (InfoModel.FloorList.Count == 0)
                    {
                        //未读取到楼层数据
                        return;
                    }
                    ////
                    //string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    //FileStream fs = new FileStream(dir + "\\WaterDebug.txt", FileMode.Create);
                    //StreamWriter sw = new StreamWriter(fs);
                    //sw.WriteLine("计算开始" + DateTime.Now.ToString());
                    //sw.Close();
                    //fs.Close();
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
                    Extents3d ext = new Extents3d();
                    for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    {
                        var pl = CreatePolyFromPoints(
                            InfoModel.FloorList[i].FloorArea.Vertices().Cast<Point3d>().ToArray());
                        ext.AddExtents(pl.GeometricExtents);
                    }
                    OptimizedDataReader dataReader = new OptimizedDataReader(ext, startPt);
                    for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    {
                        InfoModel.FloorList[i].FloorInfo = dataReader.GetDatas(CreatePolyFromPoints(
                            InfoModel.FloorList[i].FloorArea.Vertices().Cast<Point3d>().ToArray()),i);
                    }
                    //读取楼层信息
                    //var floorInfoExtractionService = new ThFloorInfoExtractionService();
                    //for (int i = 0; i < InfoModel.FloorList.Count; i++)
                    //{
                    //    InfoModel.FloorList[i].FloorInfo = floorInfoExtractionService.GetFloorInfo(InfoModel.FloorList[i], i);
                    //}
                    //初始化提取的数据中与管线、阀门等绘图相关的图层信息
                    ThLayerInitializeService layerInitializeService = new ThLayerInitializeService();
                    layerInitializeService.Initialize(InfoModel);
                    //处理楼层立管数据
                    var floorHandleService = new ThFloorHandleService();
                    floorHandleService.MatchRiserMark(InfoModel.FloorList);
                    var risers = floorHandleService.MergeRiser(InfoModel.FloorList, StoryFrameBasePoint);
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
                        //生成数据错
                        return;
                    }
                    //通过树绘制系统图 
                    var systemMapeService = new ThSystemMapService();
                    systemMapeService.FloorHeight = InfoModel.FloorLineSpace;
                    systemMapeService.FloorList = InfoModel.FloorList;
                    systemMapeService.RiserList = risers;
                    systemMapeService.Mt = mt;
                    systemMapeService.DrawMap(basePt,pipeTree);
                    sw.Stop();
                    Active.Editor.WriteLine("系统图绘制完成，用时" + sw.Elapsed.TotalSeconds + "秒。");
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
