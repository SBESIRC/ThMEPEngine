using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Catel.Linq;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Method;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Service.MultiBranchLoop;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    internal class SpraySysWithAcrossFloor
    {
        public static bool GetInput(AcadDatabase acadDatabase, SprayIn sprayIn, Point3dCollection selectArea, Point3d startPt)
        {
            var database = acadDatabase.Database;

            var pipe = new SprayPipe();
            pipe.Extract(database, selectArea);//提取管道
            var pipeLines = pipe.CreateSprayLines();//生成管道线
          
            //pipeLines.CreatePtDic(sprayIn);//创建初始字典对
            var vertical = new VerticalPipeNew();
            vertical.Extract(database, selectArea, sprayIn);//提取竖管

            var leadLine = new LeadLineNew();//提取引线
            leadLine.Extract(database, selectArea);//3,283ms
            sprayIn.LeadLines = leadLine.GetLines();
            var alarmValve = new AlarmValveTCH();
            var alarmPts = alarmValve.Extract(database, selectArea);

            pipeLines = LineTools.DealPipeLines(pipeLines, alarmPts, sprayIn);

            pipeLines.CreatePtDic(sprayIn);//创建初始字典对
            DicTools.CreatePtTypeDic(sprayIn.PtDic.Keys.ToList(), "MainLoop", sprayIn);
            foreach (var line in pipeLines.ToList())
            {
                var spt = new Point3dEx(line.StartPoint);
                var ept = new Point3dEx(line.EndPoint);
                if (sprayIn.PtDic[spt].Count == 1 || sprayIn.PtDic[ept].Count == 1)
                {
                    var l = line.Length;
                    if (l < 10)
                    {
                        pipeLines.Remove(line);
                    }
                }
            }

            var valve = new Valve();
            valve.Extract(database, sprayIn);//提取阀门
            valve.CreateValveLine();
            PipeLineTool.PipeLineSplitByValve(sprayIn, valve, ref pipeLines);//基于阀门对管线进行打断操作

            var flowIndicator = new FlowIndicator();
            flowIndicator.Extract(database, selectArea);
            var flowPts = flowIndicator.CreatePts(sprayIn);
 
            var objs = flowIndicator.CreatBlocks();
            pipeLines.PipeLineSplit(flowPts);

            pipeLines.CreatePtDic(sprayIn);
            DicTools.CreatePtTypeDic(flowPts, "Flow", sprayIn);
            DicTools.CreatFlowBlocks(objs, sprayIn);

            Dics.CreateLeadLineDic(ref sprayIn);//3,559ms

            var DNLineEngine = new PipeDnLineNew();//提取管径标注线
            DNLineEngine.Extract(database, selectArea);
            var SlashPts = DNLineEngine.ExtractSlash();//斜线点集合
            var leadLineDic = DNLineEngine.ExtractleadLine(SlashPts);
            var segLineDic = DNLineEngine.ExtractSegLine(leadLineDic);

            var DNPipeEngine = new PipeDnNew();//提取管径标注
            var PipeDN = DNPipeEngine.Extract(database, sprayIn);
            var pipeDNSpatialIndex = new ThCADCoreNTSSpatialIndex(PipeDN);
            PtDic.CreateBranchDNDic(sprayIn, pipeDNSpatialIndex);

            sprayIn.SlashDic = DNPipeEngine.GetSlashDic(leadLineDic, segLineDic);//斜点标注对
            PtDic.CreateDNDic(sprayIn, PipeDN, pipeLines);//创建DN字典对

            var pumpText = new PumpTextNew(leadLine.TextDbObjs);//末端文字标注
            pumpText.Extract(database, selectArea);//2,820ms
            sprayIn.PumpTexts = pumpText.GetTexts();
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(pumpText.DBObjs);

            sprayIn.CreateTermPtDicWithAcrossFloor(textSpatialIndex, pipeDNSpatialIndex);//针对包含立管的批量标注//9973ms

            sprayIn.CreateTermPt(textSpatialIndex, true);//针对存在缺省立管的标注
            TermPtDeal.CreateTermPtWithBlock(flowPts, sprayIn, textSpatialIndex);//针对标注标在水流指示器上的case

            DicTools.CreatePtTypeDic1(alarmPts, "AlarmValve", ref sprayIn);

            var alarmText = new AlarmTchText();
            alarmText.Extract(database, sprayIn);//提取报警阀文字
            alarmText.CreateAlarmTextDic(sprayIn, alarmPts, textSpatialIndex);//生成报警阀文字字典对

            var loopMarkPt = new LoopMarkPt();//环管标记点
            loopMarkPt.Extract(database, selectArea);
            loopMarkPt.CreateStartPts(pipeLines, sprayIn, startPt);//获取环管的起始终止点
            if (sprayIn.LoopStartPt.Equals(new Point3dEx()))
            {
                return false;
            }

            DicTools.CreatePtDic(sprayIn);//针对跨层
            return true;
        }

        /// <summary>
        /// 判断跨楼层类型 1：主环跨楼层；0：报警阀跨楼层
        /// </summary>
        public static bool AcrossFloorTypeCheck(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var allPts = sprayIn.ThroughPt;
            var dbPts = new List<DBPoint>();
            allPts.ForEach(p => dbPts.Add(new DBPoint(p._pt)));
            var ptSpatialIndex = new ThCADCoreNTSSpatialIndex(dbPts.ToCollection());
            var ptsls = new List<List<Point3dEx>>();
            foreach (var rect in sprayIn.FloorRectDic.Values)
            {
                var pts = new List<Point3dEx>();
                if (CheckSprayType.HasAlarmValveWithoutStartPt(sprayIn.LoopStartPt, rect, sprayIn.AlarmTextDic.Keys.ToList()))
                {
                    var rstPts = ptSpatialIndex.SelectCrossingPolygon(rect);
                    foreach (var p in rstPts)
                    {
                        var ptex = new Point3dEx((p as DBPoint).Position);
                        pts.Add(ptex);
                    }
                }
                ptsls.Add(pts);
            }
            return MainLoopDeal.MainLoopDfs(ptsls, sprayIn);//判断其他楼层是否存在主环
        }


        public static bool Processing(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var tempPath = new List<Point3dEx>();//主环路临时路径

            visited.Add(sprayIn.LoopStartPt);
            tempPath.Add(sprayIn.LoopStartPt);
            //主环路提取
            Dfs.DfsMainLoopWithAcrossFloor(sprayIn.LoopStartPt, tempPath, ref visited, ref mainPathList, sprayIn, ref extraNodes);
            spraySystem.MainLoop.AddRange(mainPathList[0]);
            DicTools.SetPointType(sprayIn, mainPathList, extraNodes);

            BranchLoopDeal.GetWithAcrossFloor(ref visited, sprayIn, spraySystem);
            BranchDealWithAcorssFloor.Get(ref visited, sprayIn, spraySystem);
            BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
            return true;
        }

        public static void GetOutput(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoopWithAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            BranchLoopAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            BranchAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }


        public static bool Processing2(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem)
        {
            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            var tempPath = new List<Point3dEx>();//主环路临时路径

            visited.Add(sprayIn.LoopStartPt);
            tempPath.Add(sprayIn.LoopStartPt);
            //主环路提取
            Dfs.DfsMainLoopWithAcrossFloor(sprayIn.LoopStartPt, tempPath, ref visited, ref mainPathList, sprayIn, ref extraNodes);
            DicTools.SetPointType(sprayIn, mainPathList, extraNodes);
            spraySystem.MainLoop.AddRange(mainPathList[0]);
            BranchLoopDeal.GetInCurrentFloor(ref visited, sprayIn, spraySystem);//获取当前层的支环，不跨层
            BranchLoopDeal.GetWithAcrossFloor2(ref visited, sprayIn, spraySystem);//获取支环上的支环
            BranchDealWithAcorssFloor.Get(ref visited, sprayIn, spraySystem);
            BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
            return true;
        }

        public static void GetOutput2(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            MainLoopWithAcrossFloor.Get(sprayOut, spraySystem, sprayIn);

            BranchLoop1.Get(sprayOut, spraySystem, sprayIn);
            BranchLoop2.Get(sprayOut, spraySystem, sprayIn);
            BranchLoop3.Get(sprayOut, spraySystem, sprayIn);
            BranchAcrossFloor.Get(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }
    }
}
