using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Diagnostics;
using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanConnect.Service;
using ThMEPHVAC.FanConnect.ViewModel;

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
                using (var doclock = Active.Document.LockDocument())
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

                    //if (pipes.ToCollection().Polygonize().Count > 0)
                    //{
                    //    Active.Editor.WriteMessage("水管路由存在环路，请检查修改\n");
                    //    return;
                    //}

                    //提取水管连接点
                    var fcus = ThEquipElementExtractService.GetFCUModels(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    if (fcus.Count == 0)
                    {
                        return;
                    }
                    ///处理数据--删除不必要的线（图纸上不删除）
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    //mt = Matrix3d.Displacement(Point3d.Origin.GetVectorTo(Point3d.Origin));

                    var handlePipeService = new ThHandleFanPipeService()
                    {
                        StartPoint = startPt,
                        AllFan = fcus,
                        AllLine = pipes
                    };

                    var tempLine = handlePipeService.CleanPipe(mt);
                    if (tempLine.ToCollection().Polygonize().Count > 0)
                    {
                        Active.Editor.WriteMessage("水管路由存在环路，请检查修改\n");
                        return;
                    }

                    var tmpTree = handlePipeService.HandleFanPipe(mt, tempLine);
                    if (tmpTree == null)
                    {
                        return;
                    }
                    var badLines = handlePipeService.GetBadLine(tmpTree, mt);
                    var rightLines = handlePipeService.GetRightLine(tmpTree, mt);//已经处理好的线



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
                    if (ConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe)
                    {
                        double fanWidth = 600;
                        //if(ConfigInfo.WaterSystemConfigInfo.PipeSystemType == 1)
                        //{
                        //    fanWidth = 1200.0;
                        //}
                        foreach (var fcu in fcus)
                        {
                            if (fcu.IsConnected)
                                ThFanConnectUtils.UpdateFan(fcu, fanWidth);
                        }
                    }

                    //扩展管路
                    ThWaterPipeExtendService pipeExtendServiece = new ThWaterPipeExtendService();
                    pipeExtendServiece.ConfigInfo = ConfigInfo;
                    pipeExtendServiece.PipeExtend(treeModel);

                    ////计算流量
                    //ThPointTreeModel pointTreeModel = new ThPointTreeModel(treeModel.RootNode, fcus);
                    //if (pointTreeModel.RootNode == null)
                    //{
                    //    return;
                    //}
                    //pointTreeModel.RemEndNode(pointTreeModel.RootNode, PIPELEVEL.LEVEL2);

                    //计算流量
                    DrawUtils.ShowGeometry(rightLines, "l0rightline");
                    var pipeTreeNodes = treeModel.RootNode.GetAllTreeNode();
                    var lines = pipeTreeNodes.Select(x => x.Item.PLine).ToList();
                    DrawUtils.ShowGeometry(lines, "l0pline");
                    var breakLine = ThPointTreeModelService.BreakLine(lines, mt);
                    DrawUtils.ShowGeometry(breakLine, "l0breakline");
                    var flowCalTree = ThPointTreeModelService.BuildTree(breakLine, startPt);
                    if (flowCalTree != null)
                    {
                        ThPointTreeModelService.CalNodeLevel(flowCalTree);
                        ThPointTreeModelService.CheckMarkForLevel(flowCalTree);
                        ThPointTreeModelService.CalNodeFlowValue(flowCalTree, fcus);
                        ThPointTreeModelService.CalNodeDimValue(flowCalTree, ConfigInfo.WaterSystemConfigInfo.FrictionCoeff);
                        ThPointTreeModelService.CheckDimChange(flowCalTree);

                        ThPointTreeModelService.PrintTree(flowCalTree, "l0node");

                        //标记流量
                        ThWaterPipeMarkService pipeMarkServiece = new ThWaterPipeMarkService();
                        pipeMarkServiece.ConfigInfo = ConfigInfo;
                        //pipeMarkServiece.PipeMark(pointTreeModel);
                        pipeMarkServiece.CreateMark(flowCalTree, pipeTreeNodes);
                    }

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
