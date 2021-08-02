using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetInput
    {
        public static void GetFireHydrantSysInput(AcadDatabase acadDatabase, ref FireHydrantSystemIn fireHydrantSysIn, Point3dCollection selectArea)
        {
            var lineList = new List<Line>();//管段列表
            var pointList = new List<Point3dEx>();//点集
            var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

            var hydrantEngine = new ThExtractHydrant();//提取消火栓管段末端
            var hydrantDB = hydrantEngine.Extract(acadDatabase, selectArea);
            fireHydrantSysIn.hydrantPosition = hydrantEngine.CreatePointList();

            var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
            var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
            PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

            PipeLineList.PipeLineAutoConnect(ref lineList, ref fireHydrantSysIn);//管线自动连接

            PipeLineList.RemoveFalsePipe(ref lineList, fireHydrantSysIn.hydrantPosition);//删除两个点都是端点的线段
            PipeLineList.ConnectBreakLine(ref lineList, fireHydrantSysIn, ref pointList);//连接没画好的线段
            PipeLine.PipeLineSplit(ref lineList, pointList);//管线打断                                                                             //PipeLineList.ConnectBreakLine(ref lineList, fireHydrantSysIn, ref pointList);
              
            var valveEngine = new ThExtractValveService();//提取阀
            var valveDB = valveEngine.Extract(acadDatabase.Database, selectArea);
            //假定同一图纸只存在一种类型的阀
            fireHydrantSysIn.ValveIsBkReference = valveDB.Cast<Entity>().Where(e => e is BlockReference).Any();
            var valveList = new List<Line>();

            var gateValveEngine = new ThExtractGateValveService();//提取闸阀
            var gateValveDB = gateValveEngine.Extract(acadDatabase.Database, selectArea);
            fireHydrantSysIn.GateValves = gateValveEngine.GetGateValveSite(gateValveDB);

            var casingEngine = new ThExtractCasing();//提取套管
            var casingPts = casingEngine.Extract(acadDatabase.Database, selectArea);
            var casingSpatialIndex = new ThCADCoreNTSSpatialIndex(casingPts);

            PipeLine.AddValveLine(valveDB, ref fireHydrantSysIn, ref pointList, ref lineList, ref valveList, casingSpatialIndex);

            var nodeEngine = new ThExtractNodeTag();//提取消火栓环管节点标记
            var nodeDB = nodeEngine.Extract(acadDatabase.Database, selectArea);
            fireHydrantSysIn.nodeList = nodeEngine.GetPointList();
            fireHydrantSysIn.angleList = nodeEngine.GetAngle();
            fireHydrantSysIn.markList = nodeEngine.GetMark();

            PtDic.CreatePtDic(ref fireHydrantSysIn, lineList);//管线添加
                
            var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
            var mark = markEngine.Extract(acadDatabase.Database, selectArea);
            var pipeMarkSite = markEngine.GetPipeMarkPoisition();
            MarkLine.GetMarkLineList(ref fireHydrantSysIn, pipeMarkSite, lineList);

            var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
            var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
            var labelLine = labelEngine.CreateLabelLineList();
                
            double textWidth = 1300;
            double textHeight = 525;
            var textEngine = new ThExtractLabelText();//提取文字
            var textCollection = textEngine.Extract(acadDatabase.Database, selectArea, ref textWidth);
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);

            var DNLineEngine = new ThExtractPipeDNLine();//提取管径标注线
            var DNLine = DNLineEngine.Extract(acadDatabase.Database, selectArea);
            var SlashPts = DNLineEngine.ExtractSlash();//斜线点集合
            var leadLineDic = DNLineEngine.ExtractleadLine(SlashPts);
            var segLineDic = DNLineEngine.ExtractSegLine(leadLineDic);
                
            var DNPipeEngine = new ThExtractPipeDN();//提取管径标注
            var PipeDN = DNPipeEngine.Extract(acadDatabase.Database, selectArea);

            fireHydrantSysIn.SlashDic = DNPipeEngine.GetSlashDic(leadLineDic, segLineDic);//斜点标注对
            PtDic.CreateDNDic(ref fireHydrantSysIn, PipeDN, lineList);//创建DN字典对
                
            var fireHydrantEngine = new ThExtractFireHydrant();//提取室内消火栓平面
            fireHydrantEngine.Extract(acadDatabase.Database, selectArea);
            var fhSpatialIndex = new ThCADCoreNTSSpatialIndex(fireHydrantEngine.DBobjs);

            var labelPtDic = PtDic.CreateLabelPtDic(fireHydrantSysIn.hydrantPosition, labelLine);//把在同一条标记线上的点聚集
            var labelLineDic = PtDic.CreateLabelLineDic(labelPtDic, labelLine);//找到标注线
            PtDic.CreateLeadPtDic(ref fireHydrantSysIn, labelLine);//引线添加

            var ptTextDic = PtDic.CreatePtTextDic(labelPtDic, labelLineDic, textSpatialIndex);//直接生成点和text对应
                
            PtDic.CreateTermPtDic(ref fireHydrantSysIn, pointList, labelLine, textSpatialIndex, ptTextDic, fhSpatialIndex);
            fireHydrantSysIn.textWidth = textWidth * 350 / 525;
            fireHydrantSysIn.pipeWidth = textWidth + 200;
        }
    }
}
