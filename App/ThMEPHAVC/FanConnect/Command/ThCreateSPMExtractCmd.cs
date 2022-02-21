using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;
using DotNetARX;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;
using NFox.Cad;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThCreateSPMExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public ThWaterPipeConfigInfo ConfigInfo { set; get; }//界面输入信息
        public void Dispose()
        {
            //
        }
        public ThCreateSPMExtractCmd()
        {
            CommandName = "THSPM";
            ActionName = "生成水管路由平面";
        }
        public override void SubExecute()
        {
            try
            {
                using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                using (var database = AcadDatabase.Active())
                {
                    ThFanConnectUtils.ImportBlockFile();
                    //选择起点
                    var startPt = ThFanConnectUtils.SelectPoint();
                    startPt = startPt.ToPoint2d().ToPoint3d();
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    //提取水管路由
                    var pipes = ThEquipElementExtractService.GetFanPipes(startPt);

                    if (pipes.Count == 0)
                    {
                        return;
                    }
                    if(pipes.ToCollection().Polygonize().Count > 0)
                    {
                        Active.Editor.WriteMessage("水管路由存在环路，请检查修改\n");
                        return;
                    }
                    //提取水管连接点
                    var fcus = ThEquipElementExtractService.GetFCUModels(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    foreach(var f in fcus)
                    {
                        f.FanPoint = f.FanPoint.ToPoint2d().ToPoint3d();
                        f.FanObb = f.FanObb.ToNTSLineString().ToDbPolyline();
                    }
                    if (fcus.Count == 0)
                    {
                        return;
                    }
                    ///处理数据--删除不必要的线（图纸上不删除）
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    var handlePipeService = new ThHandleFanPipeService()
                    {
                        StartPoint = startPt,
                        AllFan = fcus,
                        AllLine = pipes
                    };
                    var tmpTree = handlePipeService.HandleFanPipe(mt);
                    if (tmpTree == null)
                    {
                        return;
                    }
                    var badLines = handlePipeService.GetBadLine(tmpTree, mt);
                    var rightLines = handlePipeService.GetRightLine(tmpTree,mt);//已经处理好的线
                    double space = 300.0;
                    if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                    {
                        space = 150.0;
                    }
                    //构建Tree
                    ThFanTreeModel treeModel = new ThFanTreeModel(startPt, rightLines, space);
                    if (treeModel.RootNode == null)
                    {
                        return;
                    }
                    //标记4通结点
                    ThFanConnectUtils.FindFourWay(treeModel.RootNode);
                    //
                    foreach (var fcu in fcus)
                    {
                        ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu);
                    }
                    if(ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        double fanWidth = 600;
                        //if(ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                        //{
                        //    fanWidth = 1200.0;
                        //}
                        foreach (var fcu in fcus)
                        {
                            if(fcu.IsConnected)
                                ThFanConnectUtils.UpdateFan(fcu, fanWidth);
                        }
                    }

                    //扩展管路
                    ThWaterPipeExtendService pipeExtendServiece = new ThWaterPipeExtendService();
                    pipeExtendServiece.ConfigInfo = ConfigInfo;
                    pipeExtendServiece.PipeExtend(treeModel);
                    //计算流量
                    ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                    if (pointTreeModel.RootNode == null)
                    {
                        return;
                    }
                    //标记流量
                    ThWaterPipeMarkService pipeMarkServiece = new ThWaterPipeMarkService();
                    pipeMarkServiece.ConfigInfo = ConfigInfo;
                    pipeMarkServiece.PipeMark(pointTreeModel);
                    if (ConfigInfo.WaterSystemConfigInfo.IsGenerValve && ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        //插入阀门
                        ThAddValveService addValveServiece = new ThAddValveService();
                        addValveServiece.ConfigInfo = ConfigInfo;
                        addValveServiece.AddValve(treeModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
    }
}
