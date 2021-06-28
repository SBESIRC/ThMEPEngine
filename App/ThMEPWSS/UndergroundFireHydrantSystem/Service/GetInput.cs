using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class GetInput
    {
        public static void GetFireHydrantSysInput(ref FireHydrantSystemIn fireHydrantSysIn)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var tuplePoint = Common.Utils.SelectPoints();//范围框定
                var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域

                var lineList = new List<Line>();//管段列表
                var pointList = new List<Point3dEx>();//点集
                
                var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

                var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
                var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
                PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

                var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
                var mark = markEngine.Extract(acadDatabase.Database, selectArea);
                var pipeMarkSite = markEngine.GetPipeMarkPoisition();

                var hydrantEngine = new ThExtractHydrant();//提取消火栓管段末端
                var hydrantDB = hydrantEngine.Extract(acadDatabase.Database, selectArea);
                fireHydrantSysIn.hydrantPosition = hydrantEngine.CreatePointList();

                foreach (var pms in pipeMarkSite)
                {
                    fireHydrantSysIn.markLine.Add(PointCompute.PointInLine(pms, lineList));
                }

                var valveEngine = new ThExtractValveService();//提取蝶阀
                var valveDB = valveEngine.Extract(acadDatabase.Database, selectArea);
                fireHydrantSysIn.ValveIsBkReference = valveEngine.IsBkReference;
                var valveList = new List<Line>();
                PipeLine.AddValveLine(valveDB, ref fireHydrantSysIn, ref pointList, ref lineList, ref valveList);

                PipeLine.PipeLineSplit(ref lineList, pointList);//管线打断

                var nodeEngine = new ThExtractNodeTag();//提取消火栓环管节点标记
                var nodeDB = nodeEngine.Extract(acadDatabase.Database, selectArea);
                fireHydrantSysIn.nodeList = nodeEngine.GetPointList();
                fireHydrantSysIn.angleList = nodeEngine.GetAngle();
                fireHydrantSysIn.markList = nodeEngine.GetMark();

                //管线添加
                fireHydrantSysIn.ptDic = new Dictionary<Point3dEx, List<Point3dEx>>();//清空  当前点和邻接点字典对
                foreach (var L in lineList)
                {
                    var pt1 = new Point3dEx(L.StartPoint);
                    var pt2 = new Point3dEx(L.EndPoint);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2);
                }

                


                var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
                var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
                var labelLine = labelEngine.CreateLabelLineList();


                var textEngine = new ThExtractLabelText();//提取文字
                textEngine.Extract(acadDatabase.Database);

                var fireHydrantEngine = new ThExtractFireHydrant();
                fireHydrantEngine.Extract(acadDatabase.Database);
                
                foreach (var pt in fireHydrantSysIn.hydrantPosition)
                {
                    var tpt = new Point3dEx(new Point3d());
                    foreach(var p in pointList)
                    {
                        if(p._pt.DistanceTo(pt._pt) < 150)
                        {
                            tpt = p;
                        }
                    }
                    var termPoint = new TermPoint(pt);
                    termPoint.SetLines(labelLine);
                    termPoint.SetPipeNumber(textEngine.Results);
                    termPoint.SetType(fireHydrantEngine.Results);
                    fireHydrantSysIn.termPointDic.Add(tpt, termPoint);
                }
            }
        }
    }
}
