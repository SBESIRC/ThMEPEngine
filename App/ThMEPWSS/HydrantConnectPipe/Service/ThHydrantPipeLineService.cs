using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.HydrantConnectPipe.Engine;
using ThMEPWSS.HydrantConnectPipe.Model;
using Linq2Acad;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.CADExtensionsNs;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using NFox.Cad;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantPipeLineService
    {
        public void GetHydrantLoopAndBranchLines(ref List<Line> loopLines, ref List<Line> branchLines,  Point3dCollection selectArea)
        {
            //获取环管标记点
            var pipeMarkEngine = new ThHydrantPipeMarkRecognitionEngine();
            pipeMarkEngine.Extract(selectArea);
            var pipeMarks = pipeMarkEngine.GetPipeMarks();

            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
            List<Line> pipeLines = GetPipeLines(ref fireHydrantSysIn,selectArea);
            loopLines = GetMainPipeLines(pipeLines,pipeMarks, fireHydrantSysIn);
            branchLines = pipeLines.Except(loopLines).ToList();
            loopLines = PipeLineList.CleanLaneLines3(loopLines);
            branchLines = PipeLineList.CleanLaneLines3(branchLines);
        }
        private void RemovePipeLines(ref List<Line> pipeLines,ref FireHydrantSystemIn fireHydrantSysIn,List<ThHydrantPipeMark> marks)
        {
            //便历邻接点为1的线
            List<Line> tmpLine = new List<Line>();
            foreach (var l in pipeLines)
            {
                bool isStartLoopLine = false;
                foreach (var mark in marks)
                {
                    if ((l.PointOnLine(mark.StartPoint, false, 100) && l.PointOnLine(mark.StartPoint, true, 10)) 
                      ||(l.PointOnLine(mark.EndPoint, false, 100) && l.PointOnLine(mark.EndPoint, true, 10)))
                    {
                        isStartLoopLine = true;
                        break;
                    }
                }
                if (isStartLoopLine)
                {
                    continue;
                }

                var startPtEx = new Point3dEx(l.StartPoint);

                var endPtEx = new Point3dEx(l.EndPoint);

                //start
                if (fireHydrantSysIn.ptDic.ContainsKey(startPtEx))
                {
                    var startPtNeighbors = fireHydrantSysIn.ptDic[startPtEx];
                    if (startPtNeighbors.Count == 1)
                    {
                        tmpLine.Add(l);
                        continue;
                    }
                }

                //end
                if (fireHydrantSysIn.ptDic.ContainsKey(endPtEx))
                {
                    var startPtNeighbors = fireHydrantSysIn.ptDic[endPtEx];
                    if (startPtNeighbors.Count == 1)
                    {
                        tmpLine.Add(l);
                        continue;
                    }
                }
            }

            if (tmpLine.Count == 0)
            {
                return;
            }
            else
            {
                foreach (var l in tmpLine)
                {
                    var startPtEx = new Point3dEx(l.StartPoint);

                    var endPtEx = new Point3dEx(l.EndPoint);

                    //start
                    if (fireHydrantSysIn.ptDic.ContainsKey(startPtEx))
                    {
                        var startPtNeighbors = fireHydrantSysIn.ptDic[startPtEx];
                        if (startPtNeighbors.Count == 1)
                        {
                            pipeLines.Remove(l);
                            fireHydrantSysIn.ptDic[endPtEx].Remove(startPtEx);
                            continue;
                        }
                    }

                    //end
                    if (fireHydrantSysIn.ptDic.ContainsKey(endPtEx))
                    {
                        var startPtNeighbors = fireHydrantSysIn.ptDic[endPtEx];
                        if (startPtNeighbors.Count == 1)
                        {
                            pipeLines.Remove(l);
                            fireHydrantSysIn.ptDic[startPtEx].Remove(endPtEx);
                            continue;
                        }
                    }
                }
                RemovePipeLines(ref pipeLines,ref fireHydrantSysIn,marks);
            }
        }
        private List<Line> GetMainPipeLines(List<Line> pipeLines, List<ThHydrantPipeMark> marks, FireHydrantSystemIn fireHydrantSysIn)
        {
            var tmpPipeLines = new List<Line>(pipeLines);
            RemovePipeLines(ref tmpPipeLines, ref fireHydrantSysIn, marks);

            return tmpPipeLines;
        }
        private List<Point3dEx> GetPipePoints(List<Line> pipeLines)
        {
            List<Point3dEx> pipePoints = new List<Point3dEx>();
            foreach(var line in pipeLines)
            {
                Point3dEx point1 = new Point3dEx(line.StartPoint);
                Point3dEx point2 = new Point3dEx(line.EndPoint);
                pipePoints.Add(point1);
                pipePoints.Add(point2);
            }
            return pipePoints.Distinct().ToList();
        }

        private List<Line> GetPipeLines(ref FireHydrantSystemIn fireHydrantSysIn,Point3dCollection selectArea) 
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var lineList = new List<Line>();//管段列表
                var pointList = new List<Point3dEx>();//点集

                var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

                var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
                var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);

                PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

                PipeLineList.PipeLineAutoConnect(ref lineList);

                //var valveEngine = new ThExtractValveService();//提取蝶阀
                //var valveDB = valveEngine.Extract(acadDatabase.Database, selectArea);
                //fireHydrantSysIn.ValveIsBkReference = valveEngine.IsBkReference;
                //var valveList = new List<Line>();
                //PipeLine.AddValveLine(valveDB, ref fireHydrantSysIn, ref pointList, ref lineList, ref valveList);

                PipeLine.PipeLineSplit(ref lineList, pointList);//管线打断

                fireHydrantSysIn.ptDic = new Dictionary<Point3dEx, List<Point3dEx>>();//清空  当前点和邻接点字典对
                foreach (var L in lineList)
                {
                    var pt1 = new Point3dEx(L.StartPoint);
                    var pt2 = new Point3dEx(L.EndPoint);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2);
                }

                return lineList;
            }
        }

  

       
    }
}
