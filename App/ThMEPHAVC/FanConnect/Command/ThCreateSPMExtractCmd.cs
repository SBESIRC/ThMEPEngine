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
                    if (startPt.IsEqualTo(new Point3d()))
                    {
                        return;
                    }
                    //提取水管路由
                    var mt = Matrix3d.Displacement(startPt.GetVectorTo(Point3d.Origin));
                    var pipes = ThEquipElementExtractService.GetFanPipes(startPt);
                    if(pipes.Count == 0)
                    {
                        return;
                    }
                    //提取水管连接点
                    var fcus = ThEquipElementExtractService.GetFCUModels(ConfigInfo.WaterSystemConfigInfo.SystemType);
                    if (fcus.Count == 0)
                    {
                        return;
                    }
                    if(pipes == null)
                    {
                        return;
                    }
                    foreach (var p in pipes)
                    {
                        p.TransformBy(mt);
                    }
                    //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
                    ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
                    var lineColl = cleanServiec.CleanNoding(pipes.ToCollection());
                    if( lineColl.Polygonize().Count >0)
                    {
                        Active.Editor.WriteMessage("水管路由存在环路，请检查修改\n");
                        return;
                    }
                    foreach (var p in pipes)
                    {
                        p.TransformBy(mt.Inverse());
                    }
                    var tmpLines = new List<Line>();
                    foreach (var l in lineColl)
                    {
                        tmpLines.Add(l as Line);
                    }
                    var lines = ThFanConnectUtils.CleanLaneLines(tmpLines);
                    foreach (var l in lines)
                    {
                        l.TransformBy(mt.Inverse());
                    }
                    double space = 300.0;
                    if (ConfigInfo.WaterSystemConfigInfo.SystemType == 1)//冷媒系统
                    {
                        space = 150.0;
                    }
                    //构建Tree
                    ThFanTreeModel treeModel = new ThFanTreeModel(startPt, lines, space);
                    if (treeModel.RootNode == null)
                    {
                        return;
                    }
                    //标记4通结点
                    ThFanConnectUtils.FindFourWay(treeModel.RootNode);
                    //
                    foreach (var fcu in fcus)
                    {
                        ThFanConnectUtils.FindFcuNode(treeModel.RootNode, fcu.FanPoint);
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
