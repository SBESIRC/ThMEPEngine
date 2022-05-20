using AcHelper;
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
using ThMEPWSS.UndergroundSpraySystem.General;
using Draw = ThMEPWSS.UndergroundSpraySystem.Method.Draw;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public class GetInput
    {
        public static bool GetFireHydrantSysInput(AcadDatabase acadDatabase, ref FireHydrantSystemIn fireHydrantSysIn, 
            Point3dCollection selectArea, Point3d startPt)
        {
            var lineList = new List<Line>();//管段列表
            var pointList = new List<Point3dEx>();//点集
            var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

            var verticalEngine = new Vertical();//提取立管
            var hydrantDB = verticalEngine.Extract(acadDatabase, selectArea);
            fireHydrantSysIn.VerticalPosition = verticalEngine.CreatePointList();

            var fireHydrantEngine = new ThExtractFireHydrant();//提取室内消火栓平面
            fireHydrantSysIn.HydrantWithReel = fireHydrantEngine.Extract(acadDatabase.Database, selectArea);
            var fhSpatialIndex = new ThCADCoreNTSSpatialIndex(fireHydrantEngine.DBobjs);
            fireHydrantEngine.CreateVerticalHydrantDic(fireHydrantSysIn.VerticalPosition, fireHydrantSysIn);

            var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
            var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
            PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

            //Tools.DrawLines(lineList,  "刚提取环管");
         
            if (PipeLine.hasSitong(fireHydrantSysIn))
            {
                return false;
            }

            var stopEngine = new ThExtractStopLine();
            var stopPts = stopEngine.Extract(acadDatabase.Database, selectArea);
            PipeLineList.ConnectWithVertical(ref lineList, fireHydrantSysIn);
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
            var markAngleDic = new Dictionary<Point3dEx, double>();
            var pipeMarkSite = markEngine.GetPipeMarkPoisition(ref markAngleDic);
            MarkLine.GetPipeMark(ref fireHydrantSysIn, pipeMarkSite, startPt);
            var markBool = fireHydrantSysIn.GetMarkLineList(lineList, markAngleDic);

            if (!markBool)
            {
                MessageBox.Show("找不到环管标记所在直线");
                return false;
            }
            var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
            var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
            var labelLine = labelEngine.CreateLabelLineList(labelDB);
           
            double textWidth = 1300;
            string textModel = "";
            var textEngine = new ThExtractLabelText();//提取文字
            var textCollection = textEngine.Extract(acadDatabase.Database, selectArea, ref textWidth, ref textModel);
          
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);
            var dbText = ThTextSet.ThText(new Point3d(), textModel);
            if(dbText.TextString.Trim().Count()!=0)
            {
                textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
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

            CreatePtDic(fireHydrantSysIn);

            return true;
        }

        public static void CreatePtDic(FireHydrantSystemIn fireHydrantSysIn)
        {
            double maxDist = 1000;
            double minDist = 400;
            var ptOffsetDic = new Dictionary<Point3dEx, Point3d>();
            foreach (var pt in fireHydrantSysIn.VerticalPosition)
            {
                if (ptOffsetDic.ContainsKey(pt))
                {
                    continue;
                }
                var point = pt._pt;
                var f1 = pt._pt.GetFloor(fireHydrantSysIn.FloorRect);
                if (f1.Equals(""))
                {
                    continue;
                }
                var jizhunPt = fireHydrantSysIn.FloorPt[f1];
                var offsetPt = new Point3d(point.X - jizhunPt.X, point.Y - jizhunPt.Y, 0);
                ptOffsetDic.Add(pt, offsetPt);
            }
            var usedPt = new List<Point3dEx>();
            foreach (var pt1 in ptOffsetDic.Keys)
            {
                if (pt1._pt.DistanceTo(new Point3d(1491786.3, 407697, 0)) < 10)
                    ;
                if (pt1._pt.DistanceTo(new Point3d(1491786.3, 907697, 0)) < 10)
                    ;
                if (usedPt.Contains(pt1)) continue;

                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt1)) continue;

                var str1 = fireHydrantSysIn.TermPointDic[pt1].PipeNumber;
                foreach (var pt2 in ptOffsetDic.Keys)
                {
                    if (pt2._pt.DistanceTo(new Point3d(1491786.3, 407697, 0)) < 10)
                        ;
                    if (usedPt.Contains(pt2)) continue;
                    if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt2)) continue;

                    var str2 = fireHydrantSysIn.TermPointDic[pt2].PipeNumber;
                    //两点case1
                    if (pt1._pt.DistanceTo(pt2._pt) > maxDist
                       && ptOffsetDic[pt1].DistanceTo(ptOffsetDic[pt2]) < minDist
                       && str1?.Equals(str2)==true)
                    {
                        DicTools.AddPtDicItem(fireHydrantSysIn, pt1, pt2);

                        fireHydrantSysIn.ThroughPt.AddItem(pt1);
                        fireHydrantSysIn.ThroughPt.AddItem(pt2);
                        using (AcadDatabase currentDb = AcadDatabase.Active())
                        {
                            Draw.ThroughPt(currentDb, pt1);
                            Draw.ThroughPt(currentDb, pt2);
                        }

                        usedPt.Add(pt1);
                        usedPt.Add(pt2);

                        continue;
                    }
                }
            }
            foreach (var pt in fireHydrantSysIn.ThroughPt)
            {
                if (fireHydrantSysIn.TermPointDic.ContainsKey(pt))
                {
                    fireHydrantSysIn.TermPointDic[pt].Type = 5;
                }
            }
        }

    }
}
