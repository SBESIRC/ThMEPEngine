﻿using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class GetInput
    {
        public static readonly HashSet<Entity> entities = new HashSet<Entity>();
        public static void GetFireHydrantSysInput(AcadDatabase acadDatabase, ref FireHydrantSystemIn fireHydrantSysIn, 
            Point3dCollection selectArea, Point3d startPt)
        {
            var lineList = new List<Line>();//管段列表
            var pointList = new List<Point3dEx>();//点集
            var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

            var verticalEngine = new Vertical();//提取立管
            var hydrantDB = verticalEngine.Extract(acadDatabase, selectArea);
            fireHydrantSysIn.VerticalPosition = verticalEngine.CreatePointList();

            var fireHydrantEngine = new ThExtractFireHydrant();//提取室内消火栓平面
            fireHydrantEngine.Extract(acadDatabase.Database, selectArea);
            var fhSpatialIndex = new ThCADCoreNTSSpatialIndex(fireHydrantEngine.DBobjs);
            fireHydrantEngine.CreateVerticalHydrantDic(fireHydrantSysIn.VerticalPosition, fireHydrantSysIn);

            var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
            var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
            PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);
            var stopEngine = new ThExtractStopLine();

            var stopPts = stopEngine.Extract(acadDatabase.Database, selectArea);

            PipeLineList.ConnectClosedPt(ref lineList, fireHydrantSysIn);
            PipeLineList.PipeLineAutoConnect(ref lineList, ref fireHydrantSysIn);//管线自动连接
            PipeLineList.RemoveFalsePipe(ref lineList, fireHydrantSysIn.VerticalPosition);//删除两个点都是端点的线段
            PipeLineList.ConnectBreakLineWithoutPtdic(ref lineList, fireHydrantSysIn, ref pointList, stopPts);//连接没画好的线段

            PipeLine.PipeLineSplit(ref lineList, pointList);//管线打断                                                                           
            PtDic.CreatePtDic(ref fireHydrantSysIn, lineList);//字典对更新  
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
            nodeEngine.GetPointList(ref fireHydrantSysIn);
            
            PtDic.CreatePtDic(ref fireHydrantSysIn, lineList);//字典对更新
            
            var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
            
            var mark = markEngine.Extract(acadDatabase.Database, selectArea);
            var markAngleDic = new Dictionary<Point3d, double>();
            var pipeMarkSite = markEngine.GetPipeMarkPoisition(ref markAngleDic);
            MarkLine.GetPipeMark(ref fireHydrantSysIn, pipeMarkSite, startPt);
            var markBool = fireHydrantSysIn.GetMarkLineList(lineList, markAngleDic);

            if (!markBool)
            {
                MessageBox.Show("找不到环管标记所在直线");
                return;
            }
            var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
            var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
            var labelLine = labelEngine.CreateLabelLineList(labelDB);
            foreach (var ent in labelDB.OfType<Entity>())
            {
                entities.Add(ent);
            }
            foreach (var ent in labelLine.OfType<Entity>())
            {
                entities.Add(ent);
            }
            double textWidth = 1300;
            string textModel = "";
            var textEngine = new ThExtractLabelText();//提取文字
            var textCollection = textEngine.Extract(acadDatabase.Database, selectArea, ref textWidth, ref textModel);
            foreach (var ent in textCollection.OfType<Entity>())
            {
                entities.Add(ent);
            }
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);
            var dbText = ThTextSet.ThText(new Point3d(), textModel);
            if(dbText.TextString.Trim().Count()!=0)
            {
                textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
                entities.Add(dbText);
            }
            
            var DNLineEngine = new ThExtractPipeDNLine();//提取管径标注线
            DNLineEngine.Extract(acadDatabase.Database, selectArea);
            var SlashPts = DNLineEngine.ExtractSlash();//斜线点集合
            var leadLineDic = DNLineEngine.ExtractleadLine(SlashPts);
            var segLineDic = DNLineEngine.ExtractSegLine(leadLineDic);
                
            var DNPipeEngine = new ThExtractPipeDN();//提取管径标注
            var PipeDN = DNPipeEngine.Extract(acadDatabase.Database, selectArea);
            var pipeDNSpatialIndex = new ThCADCoreNTSSpatialIndex(PipeDN);
            PtDic.CreateBranchDNDic(ref fireHydrantSysIn, pipeDNSpatialIndex);

            fireHydrantSysIn.SlashDic = DNPipeEngine.GetSlashDic(leadLineDic, segLineDic);//斜点标注对
            PtDic.CreateDNDic(ref fireHydrantSysIn, PipeDN, lineList);//创建DN字典对

            var labelPtDic = PtDic.CreateLabelPtDic(fireHydrantSysIn.VerticalPosition, labelLine);//把在同一条标记线上的点聚集
            var labelLineDic = PtDic.CreateLabelLineDic(labelPtDic, labelLine);//找到标注线
            PtDic.CreateLeadPtDic(ref fireHydrantSysIn, labelLine);//引线添加----20s----

            var ptTextDic = PtDic.CreatePtTextDic(labelPtDic, labelLineDic, textSpatialIndex);//直接生成点和text对应

            //PtDic.CreateTermPtDicOrg(ref fireHydrantSysIn, pointList, labelLine, textSpatialIndex, ptTextDic, fhSpatialIndex);
            PtDic.CreateTermPtDic(ref fireHydrantSysIn, pointList, labelLine, textSpatialIndex, ptTextDic, fhSpatialIndex);
            fireHydrantSysIn.TextWidth = textWidth + 100;
            fireHydrantSysIn.PipeWidth = textWidth + 300;
        }

        private static void GetEntType(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptEntityOptions("\nPick entity in block:");
            var entityResult = Active.Editor.GetEntity(entOpt);

            var entId = entityResult.ObjectId;
            var dbObj = acadDatabase.Element<Entity>(entId);
            var objs = new DBObjectCollection();
            dbObj.Explode(objs);
        }
    }
}
