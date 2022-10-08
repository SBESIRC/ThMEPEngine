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
        public static bool GetFireHydrantSysInput(AcadDatabase acadDatabase, FireHydrantSystemIn fireHydrantSysIn, 
            Point3dCollection selectArea, Point3d startPt)
        {
            var lineList = new List<Line>();//管段列表
            var pointList = new List<Point3dEx>();//点集

            var verticalEngine = new Vertical();//提取立管
            verticalEngine.Extract(acadDatabase, selectArea);
            fireHydrantSysIn.VerticalPosition = verticalEngine.CreatePointList();

            var fireHydrantEngine = new ThExtractFireHydrant();//提取室内消火栓平面
            fireHydrantSysIn.HydrantWithReel = fireHydrantEngine.Extract(acadDatabase.Database, selectArea);
            var fhSpatialIndex = new ThCADCoreNTSSpatialIndex(fireHydrantEngine.DBobjs);
            fireHydrantEngine.CreateVerticalHydrantDic(fireHydrantSysIn.VerticalPosition, fireHydrantSysIn);

            var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
            var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
            PipeLine.AddPipeLine(dbObjs, fireHydrantSysIn, pointList, lineList);
         
            //if (PipeLine.HasSitong(fireHydrantSysIn))
            //{
            //    return false;
            //}

            var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
            markEngine.Extract(acadDatabase.Database, selectArea);
            var pipeMarkSite = markEngine.GetPipeMarkPoisition(out Dictionary<Point3dEx, double> markAngleDic);
            MarkLine.GetPipeMark(fireHydrantSysIn, pipeMarkSite, startPt);
            var markBool = fireHydrantSysIn.GetMarkLineList(lineList, markAngleDic);
            foreach(var item in markAngleDic)
            {
                fireHydrantSysIn.AngleList.Add(item.Key,item.Value);
            }
            if (!markBool)
            {
                MessageBox.Show("找不到环管标记所在直线");
                return false;
            }

            var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
            var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
            var labelLine = labelEngine.CreateLabelLineList(labelDB);

            var stopEngine = new ThExtractStopLine();
            var stopPts = stopEngine.Extract(acadDatabase.Database, selectArea);
            PipeLineList.ConnectWithVertical(lineList, fireHydrantSysIn, labelLine);
            PipeLineList.ConnectClosedPt(lineList, fireHydrantSysIn);
            PipeLineList.PipeLineAutoConnect(ref lineList, fireHydrantSysIn);//管线自动连接
            PipeLineList.RemoveFalsePipe(lineList, fireHydrantSysIn.VerticalPosition);//删除两个点都是端点的线段
            PipeLineList.ConnectBreakLineWithoutPtdic(lineList, fireHydrantSysIn, pointList, stopPts);//连接没画好的线段
            PipeLine.PipeLineSplit(lineList, pointList);//管线打断                                                                           
            PtDic.CreatePtDic(fireHydrantSysIn, lineList);//字典对更新

            var valveEngine = new ThExtractValveService();//提取阀
            var valveDB = valveEngine.Extract(acadDatabase.Database, selectArea);
            var valveList = new List<Line>();

            var gateValveEngine = new ThExtractGateValveService();//提取闸阀
            var gateValveDB = gateValveEngine.Extract(acadDatabase.Database, selectArea);
            fireHydrantSysIn.GateValves = gateValveEngine.GetGateValveSite(gateValveDB);

            var casingEngine = new ThExtractCasing();//提取套管
            var casingPts = casingEngine.Extract(acadDatabase.Database, selectArea);

            PipeLine.AddValveLine(valveDB, fireHydrantSysIn, lineList, valveList, casingPts);
            
            var nodeEngine = new ThExtractNodeTag();//提取消火栓环管节点标记
            nodeEngine.Extract(acadDatabase.Database, selectArea);
            nodeEngine.GetPointList(fireHydrantSysIn);
            
            PtDic.CreatePtDic(fireHydrantSysIn, lineList);//字典对更新
            
            //double textWidth = 1300;
            string textModel = "";
            var textEngine = new ThExtractLabelText();//提取文字
            var textCollection = textEngine.Extract(acadDatabase.Database, selectArea);
            
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);
            var dbText = ThTextSet.ThText(new Point3d(), textModel);
            //if(dbText.TextString.Trim().Count()!=0)
            //{
            //    textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
            //}
            
            var DNLineEngine = new ThExtractPipeDNLine();//提取管径标注线
            DNLineEngine.Extract(acadDatabase.Database, selectArea);
            var SlashPts = DNLineEngine.ExtractSlash();//斜线点集合
            var leadLineDic = DNLineEngine.ExtractleadLine(SlashPts);
            var segLineDic = DNLineEngine.ExtractSegLine(leadLineDic);
                
            var DNPipeEngine = new ThExtractPipeDN();//提取管径标注
            var PipeDN = DNPipeEngine.Extract(acadDatabase.Database, selectArea);
            var pipeDNSpatialIndex = new ThCADCoreNTSSpatialIndex(PipeDN);
            PtDic.CreateBranchDNDic(fireHydrantSysIn, pipeDNSpatialIndex);

            fireHydrantSysIn.SlashDic = DNPipeEngine.GetSlashDic(leadLineDic, segLineDic);//斜点标注对
            PtDic.CreateDNDic(fireHydrantSysIn, PipeDN, lineList);//创建DN字典对

            var labelPtDic = PtDic.CreateLabelPtDic(fireHydrantSysIn.VerticalPosition, labelLine);//把在同一条标记线上的点聚集
            var labelLineDic = PtDic.CreateLabelLineDic(labelPtDic, labelLine);//找到标注线
            PtDic.CreateLeadPtDic(fireHydrantSysIn, labelLine);//引线添加----20s----

            var ptTextDic = PtDic.CreatePtTextDic(labelPtDic, labelLineDic, textSpatialIndex);//直接生成点和text对应
            double textWidth = 1300;
            PtDic.CreateTermPtDic(fireHydrantSysIn, pointList, labelLine, textSpatialIndex, ptTextDic, fhSpatialIndex);
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
                if (usedPt.Contains(pt1)) continue;

                if (!fireHydrantSysIn.TermPointDic.ContainsKey(pt1)) continue;

                var str1 = fireHydrantSysIn.TermPointDic[pt1].PipeNumber;
                foreach (var pt2 in ptOffsetDic.Keys)
                {
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
