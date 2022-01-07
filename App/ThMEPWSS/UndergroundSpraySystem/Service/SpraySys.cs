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
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class SpraySys
    {
        public static bool GetInsertPt(AcadDatabase acadDatabase, out Point3d insertPt)
        {
            insertPt = new Point3d();
            var database = acadDatabase.Database;
            var opt = new PromptPointOptions("\n请指定环管标记起点");
            var propRes = Active.Editor.GetPoint(opt);
            if (propRes.Status == PromptStatus.OK)
            {
                insertPt = propRes.Value.Point3dZ0().TransformBy(Active.Editor.UCS2WCS());
                var loopMarkPt = new LoopMarkPt();//环管标记点
                if(loopMarkPt.Extract(database, insertPt))
                {
                    return true;
                }
            }
            return false;

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
            var objs = flowIndicator.CreatBlocks();
            pipeLines.PipeLineSplit(flowPts);

            pipeLines.CreatePtDic(sprayIn);
            using (var db = AcadDatabase.Active())
            {
                var groups = db.Groups;
                var entIds = groups.SelectMany(g => g.GetAllEntityIds().ToList());
                var ents = entIds.Select(e => db.Element<Entity>(e)).Where(e => e.Layer.Contains("SPRL-PIPE")).ToCollection();
                var pts = new List<Point3dEx>();
                foreach (var ent in ents)
                {
                    var lines = new DBObjectCollection();
                    (ent as Entity).Explode(lines);
                    foreach (var line in lines)
                    {
                        if (line is Polyline)
                        {
                            ;
                            var pt1 = new Point3dEx((line as Polyline).StartPoint);
                            pts.Add(pt1);
                        }
                        if (line is Line)
                        {
                            var pt1 = new Point3dEx((line as Line).StartPoint);
                            var pt2 = new Point3dEx((line as Line).EndPoint);
                            if (sprayIn.PtDic.ContainsKey(pt1))
                            {
                                if (sprayIn.PtDic[pt1].Count == 1)
                                {
                                    if (!sprayIn.PtDic[pt1][0].Equals(pt2))
                                    {
                                        sprayIn.PtDic[pt1].Add(pt2);
                                        if (!sprayIn.PtTypeDic.ContainsKey(pt2))
                                        {
                                            sprayIn.PtTypeDic[pt2] = "MainLoop";
                                        }
                                        if (!sprayIn.PtTypeDic.ContainsKey(pt1))
                                        {
                                            sprayIn.PtTypeDic[pt1] = "MainLoop";
                                        }
                                        if (!sprayIn.PtDic.ContainsKey(pt2))
                                        {
                                            sprayIn.PtDic.Add(pt2, new List<Point3dEx>() { pt1 });
                                        }
                                        break;
                                    }

                                }
                            }
                            if (sprayIn.PtDic.ContainsKey(pt2))
                            {
                                if (sprayIn.PtDic[pt2].Count == 1)
                                {
                                    if (!sprayIn.PtDic[pt2][0].Equals(pt1))
                                    {
                                        sprayIn.PtDic[pt2].Add(pt1);
                                        if (!sprayIn.PtTypeDic.ContainsKey(pt2))
                                        {
                                            sprayIn.PtTypeDic[pt2] = "MainLoop";
                                        }
                                        if (!sprayIn.PtTypeDic.ContainsKey(pt1))
                                        {
                                            sprayIn.PtTypeDic[pt1] = "MainLoop";
                                        }
                                        if (!sprayIn.PtDic.ContainsKey(pt1))
                                        {
                                            sprayIn.PtDic.Add(pt1, new List<Point3dEx>() { pt2 });
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            DicTools.CreatePtTypeDic(flowPts, "Flow", sprayIn);
            DicTools.CreatFlowBlocks(objs, sprayIn);

            var leadLine = new LeadLine();
            leadLine.Extract(database, sprayIn);//3,283ms
            sprayIn.LeadLines = leadLine.GetLines();

            Dics.CreateLeadLineDic(ref sprayIn);//3,559ms

            var DNLineEngine = new ThExtractPipeDNLine();//提取管径标注线
            DNLineEngine.Extract(database, selectArea);
            var SlashPts = DNLineEngine.ExtractSlash();//斜线点集合
            var leadLineDic = DNLineEngine.ExtractleadLine(SlashPts);
            var segLineDic = DNLineEngine.ExtractSegLine(leadLineDic);

            var DNPipeEngine = new ThExtractPipeDN();//提取管径标注
            var PipeDN = DNPipeEngine.Extract(database, selectArea);
            var pipeDNSpatialIndex = new ThCADCoreNTSSpatialIndex(PipeDN);
            PtDic.CreateBranchDNDic(sprayIn, pipeDNSpatialIndex);

            sprayIn.SlashDic = DNPipeEngine.GetSlashDic(leadLineDic, segLineDic);//斜点标注对
            PtDic.CreateDNDic(sprayIn, PipeDN, pipeLines);//创建DN字典对

            var pumpText = new PumpText();
            pumpText.Extract(database, sprayIn);//2,820ms
            sprayIn.PumpTexts = pumpText.GetTexts();
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(pumpText.DBObjs);

            sprayIn.CreateTermPtDic(textSpatialIndex, pipeDNSpatialIndex);//针对包含立管的批量标注//9973ms

            sprayIn.CreateTermPt(textSpatialIndex);//针对存在缺省立管的标注

            var alarmValve = new AlarmValveTCH();
            var alarmPts = alarmValve.Extract(database, selectArea);
            //pipeLines.PipeLineAutoConnect(sprayIn, alarmPts);//自动连接

            DicTools.CreatePtTypeDic1(alarmPts, "AlarmValve", ref sprayIn);

            var alarmText = new AlarmText();
            alarmText.Extract(database, sprayIn);//提取报警阀文字
            alarmText.CreateAlarmTextDic(sprayIn, alarmPts);//生成报警阀文字字典对

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
            Draw.MainLoop(acadDatabase, mainPathList);
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
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoop.Get(sprayOut, spraySystem, sprayIn);
            SubLoop.Get(sprayOut, spraySystem, sprayIn);
            BranchLoop.Get(sprayOut, spraySystem, sprayIn);
            Branch.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }

        public static void GetOutput2(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoop2.Get(sprayOut, spraySystem, sprayIn);
            Branch.Get(sprayOut, spraySystem, sprayIn);
            StoreyLine.Get2(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }
    }
}
