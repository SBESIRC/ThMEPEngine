using Linq2Acad;
using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundSpraySystem.Method;
using Autodesk.AutoCAD.EditorInput;
using GeometryExtensions;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Draw = ThMEPWSS.UndergroundSpraySystem.Method.Draw;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class SpraySys
    {
        public static Point3d GetInsertPt()
        {
            var opt = new PromptPointOptions("请指定环管标记起点: \n");
            var propRes = Active.Editor.GetPoint(opt);
            if (propRes.Status == PromptStatus.OK)
            {
                return propRes.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS());
            }
            return new Point3d();
        }
        public static bool GetInput(AcadDatabase acadDatabase, SprayIn sprayIn, Point3dCollection selectArea, Point3d startPt)
        {
            var database = acadDatabase.Database;

            var pipe = new SprayPipe();
            pipe.Extract(database, sprayIn);//提取管道
            var pipeLines = pipe.CreateSprayLines();//生成管道线
            pipeLines.CreatePtDic(sprayIn);//创建初始字典对

            var vertical = new Verticalpipe();
            vertical.Extract(database, sprayIn);//提取竖管

            pipeLines = pipeLines.ConnectVerticalLine(sprayIn);

            pipeLines = pipeLines.PipeLineAutoConnect(sprayIn);//自动连接

            pipeLines.CreatePtDic(sprayIn);
            
            pipeLines = pipeLines.ConnectBreakLine(sprayIn);
            pipeLines.CreatePtDic(sprayIn);
            
            pipeLines = pipeLines.PipeLineSplit(sprayIn.PtDic.Keys.ToList());
            pipeLines.CreatePtDic(sprayIn);
            
            DicTools.CreatePtTypeDic(sprayIn.PtDic.Keys.ToList(), "MainLoop", sprayIn);
            foreach (var line in pipeLines.ToList())
            {
                var spt = new Point3dEx(line.StartPoint);
                var ept = new Point3dEx(line.EndPoint);
                if (sprayIn.PtDic[spt].Count == 1 || sprayIn.PtDic[ept].Count == 1)
                {
                    var l = line.Length;
                    if(l < 10)
                    {
                        pipeLines.Remove(line);
                    }
                }
            }
            
            var pipeNo = new PipeNo();
            pipeNo.Extract(database, sprayIn);

            var pipeDN = new PipeDN();
            pipeDN.Extract(database, sprayIn);//2.4s

            var valve = new Valve();
            valve.Extract(database, sprayIn);
            valve.CreateValveLine();
            
            pipeLines = pipeLines.PipeLineSplit(valve.SignalValves);
            pipeLines = pipeLines.PipeLineSplit(valve.PressureValves);
            pipeLines = pipeLines.PipeLineSplit(valve.DieValves);
            pipeLines.CreatePtDic(sprayIn);

            DicTools.CreatePtTypeDic(valve.SignalValves, "SignalValve", sprayIn);
            DicTools.CreatePtTypeDic(valve.PressureValves, "PressureValves", sprayIn);
            DicTools.CreatePtTypeDic(valve.DieValves, "DieValves", sprayIn);

            var flowIndicator = new FlowIndicator();
            flowIndicator.Extract(database, selectArea);
            var flowPts = flowIndicator.CreatePts();
            pipeLines.PipeLineSplit(flowPts);

            pipeLines.CreatePtDic(sprayIn);
            
            DicTools.CreatePtTypeDic(flowPts, "Flow", sprayIn);

            
            var leadLine = new LeadLine();
            leadLine.Extract(database, sprayIn);//3,283ms
            sprayIn.LeadLines = leadLine.GetLines();

            Dics.CreateLeadLineDic(ref sprayIn);//3,559ms

            var pumpText = new PumpText();
            pumpText.Extract(database, sprayIn);//2,820ms
            sprayIn.PumpTexts = pumpText.GetTexts();
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(pumpText.DBObjs);
            
            sprayIn.CreateTermPtDic(textSpatialIndex);//针对包含立管的批量标注//9973ms
            
            sprayIn.CreateTermPt(textSpatialIndex);//针对存在缺省立管的标注

            var alarmValve = new AlarmValve();
            var alarmPts = alarmValve.Extract(database, selectArea);
            DicTools.CreatePtTypeDic1(alarmPts, "AlarmValve", ref sprayIn);

            var loopMarkPt = new LoopMarkPt();//环管标记点
            loopMarkPt.Extract(database, sprayIn);
            loopMarkPt.CreateStartPts(pipeLines, ref sprayIn, startPt);//获取环管的起始终止点
            if(sprayIn.LoopStartPt.Equals(new Point3d()))
            {
                return false;
            }

            DicTools.CreatePtDic(sprayIn);
            
            return true;

        }
        public static bool Processing(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var neverVisited = new HashSet<Point3dEx>();//访问标志
            var tempPath = new List<Point3dEx>();//主环路临时路径
             
            visited.Add(sprayIn.LoopStartPt);
            tempPath.Add(sprayIn.LoopStartPt);
            //主环路提取
            DepthSearch.DfsMainLoop(sprayIn.LoopStartPt, tempPath, visited, ref mainPathList, sprayIn, ref extraNodes, neverVisited);
            
            DicTools.SetPointType(sprayIn, mainPathList, extraNodes);

            spraySystem.MainLoop.AddRange(mainPathList[0]);

            //Draw.MainLoop(acadDatabase, mainPathList);
            if(LoopCheck.IsSingleLoop(spraySystem, sprayIn))//主环上存在报警阀
            {
                foreach(var path in mainPathList)
                {
                    spraySystem.MainLoops.Add(path);
                }
                BranchDeal2.Get(ref visited, sprayIn, spraySystem);
                BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
                return false;
            }

            SubLoopDeal.Get(ref visited, mainPathList, sprayIn, spraySystem);
            //Draw.SubLoop(acadDatabase, spraySystem);

            BranchLoopDeal.Get(ref visited, sprayIn, spraySystem);
            //Draw.BranchLoop(acadDatabase, spraySystem);
            SubLoopDeal.SetType(sprayIn, spraySystem);

            BranchDeal.Get(ref visited, sprayIn, spraySystem);
            BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
            return true;
        }

        public static void GetOutput(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            try
            {
                StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                MainLoop.Get(sprayOut, spraySystem, sprayIn);

            }
            catch (Exception ex)
            {
            }
            try
            {
                SubLoop.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                BranchLoop.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                Branch.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                PipeLine.Split(sprayOut);
            }
            catch (Exception ex)
            {
            }
        }

        public static void GetOutput2(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            try
            {
                StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                MainLoop2.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
           
            try
            {
                Branch.Get(sprayOut, spraySystem, sprayIn);
            }
            catch (Exception ex)
            {
            }
            try
            {
                PipeLine.Split(sprayOut);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
