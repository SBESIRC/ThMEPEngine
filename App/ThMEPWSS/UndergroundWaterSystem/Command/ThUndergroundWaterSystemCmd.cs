using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Service;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using ThMEPWSS.UndergroundWaterSystem.ViewModel;

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
                    var floorInfoExtractionService = new ThFloorInfoExtractionService();
                    for(int i = 0; i < InfoModel.FloorList.Count;i++)
                    {
                        InfoModel.FloorList[i].FloorInfo = floorInfoExtractionService.GetFloorInfo(InfoModel.FloorList[i], i);
                    }
                    //处理楼层立管数据
                    var floorHandleService = new ThFloorHandleService();
                    floorHandleService.MatchRiserMark(InfoModel.FloorList);
                    var risers = floorHandleService.MergeRiser(InfoModel.FloorList);
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
