using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundSpraySystem.Method;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    public class AlarmValveSystem
    {
        public static void GetInput(AcadDatabase acadDatabase, SprayIn sprayIn, Point3dCollection selectArea)
        {
            var database = acadDatabase.Database;

            var pipe = new SprayPipe();
            pipe.Extract(database, sprayIn);//提取管道
            var pipeLines = pipe.CreateSprayLines();//生成管道线
            pipeLines = LineMerge.CleanLaneLines(pipeLines);
            pipeLines.CreatePtDic(sprayIn);//创建初始字典对
            var vertical = new VerticalPipeNew();
            vertical.Extract(database, selectArea, sprayIn);//提取竖管

            var leadLine = new LeadLine();
            leadLine.Extract(database, sprayIn);//3,283ms
            sprayIn.LeadLines = leadLine.GetLines();
            Dics.CreateLeadLineDic(ref sprayIn);//3,559ms

            pipeLines = pipeLines.ConnectVerticalLine(sprayIn);
            pipeLines = pipeLines.PipeLineAutoConnect(sprayIn);//自动连接

            pipeLines.CreatePtDic(sprayIn);
            pipeLines = pipeLines.ConnectBreakLine(sprayIn);
            pipeLines.CreatePtDic(sprayIn);
            pipeLines = pipeLines.PipeLineSplit(sprayIn.PtDic.Keys.ToList());
            pipeLines.CreatePtDic(sprayIn);

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

            DicTools.CreatePtTypeDic(flowPts, "Flow", sprayIn);
            DicTools.CreatFlowBlocks(objs, sprayIn);

            var pumpText = new PumpTextNew(leadLine.TextDbObjs);
            pumpText.Extract(database, selectArea);//2,820ms
            sprayIn.PumpTexts = pumpText.GetTexts();
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(pumpText.DBObjs);

            sprayIn.CreateTermPt(textSpatialIndex);//针对存在缺省立管的标注

            DicTools.CreatePtDic(sprayIn);
        }

        public static void Processing(AcadDatabase acadDatabase, SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            var visited = new HashSet<Point3dEx>();
            BranchDeal.AlarmValveGet(ref visited, sprayIn, spraySystem, sprayOut);
            //BranchDeal.GetThrough(ref visited, sprayIn, spraySystem);
        }

        public static void GetOutput(SprayIn sprayIn, SpraySystem spraySystem, SprayOut sprayOut)
        {
            StoreyLine.Get(sprayOut, spraySystem, sprayIn);
            Branch.AlarmValveGet(sprayOut, spraySystem, sprayIn);
            StoreyLine.Get2(sprayOut, spraySystem, sprayIn);
            PipeLine.Split(sprayOut);
        }
    }
}
