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
                    if (l.PointOnLine(mark.StartPoint, false, 10) || l.PointOnLine(mark.EndPoint, false, 10))
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

        private List<Line> GetHydrantMainLine(Point3dCollection selectArea)
        {
            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数

            GetFireHydrantSysInput(selectArea, ref fireHydrantSysIn);//提取输入参数

            var mainPathList = new List<List<Point3dEx>>();//主环路最终路径
            var extraNodes = new List<Point3dEx>();//主环路连通阀点集
            var visited = new HashSet<Point3dEx>();//访问标志
            foreach (var markLine in fireHydrantSysIn.markLineList)
            {
                var startPt = new Point3dEx(markLine[0].StartPoint);//主环起始点
                var targetPt = new Point3dEx(markLine[1].EndPoint);//主环终止点
                var tempPath = new List<Point3dEx>();//主环路临时路径
                visited.Add(startPt);
                tempPath.Add(startPt);

                //主环路深度搜索
                DepthFirstSearch.dfsMainLoop(startPt, tempPath, visited, ref mainPathList, targetPt, fireHydrantSysIn, ref extraNodes);
                ThPointCountService.SetPointType(ref fireHydrantSysIn, mainPathList);
            }

            var subPathList = new List<List<Point3dEx>>();//次环路最终路径 List

            foreach (var nd in fireHydrantSysIn.nodeList)
            {
                var subStartPt = nd[0];//次环起始点
                var subTargetPt = nd[1];//次环终止点
                var subTempPath = new List<Point3dEx>();//次环路临时路径
                var subRstPath = new List<Point3dEx>();//次环路临时路径

                visited.Add(subStartPt);
                subTempPath.Add(subStartPt);

                //次环路深度搜索
                DepthFirstSearch.dfsSubLoop(subStartPt, subTempPath, visited, ref subPathList, subTargetPt, fireHydrantSysIn);
                visited.Remove(visited.Last());//删除占用的点，避免干扰其他次环的遍历
            }
            ThPointCountService.SetPointType(ref fireHydrantSysIn, subPathList);

            var branchDic = new Dictionary<Point3dEx, List<List<Point3dEx>>>();//支点 + 支点的支路列表
            foreach (var rstPath in mainPathList)
            {
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, rstPath, fireHydrantSysIn, extraNodes);//支路遍历
                        branchDic.Add(pt, branchPath);
                    }
                }
            }

            foreach (var ptls in subPathList)
            {
                foreach (var pt in ptls)
                {
                    if (fireHydrantSysIn.ptTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var branchPath = new List<List<Point3dEx>>();
                        DepthFirstSearch.BranchSearch(pt, visited, ref branchPath, ptls, fireHydrantSysIn, extraNodes);//支路遍历
                        branchDic.Add(pt, branchPath);
                    }
                }
            }

            var fireHydrantSysOut = new FireHydrantSystemOut();
            GetFireHydrantPipe.GetMainLoop(ref fireHydrantSysOut, mainPathList, fireHydrantSysIn);//主环路获取
            GetFireHydrantPipe.GetSubLoop(ref fireHydrantSysOut, subPathList, fireHydrantSysIn);//次环路获取


            List<Line> lineList = new List<Line>();
            for(int i = 0;i  < mainPathList.Count;i++)
            {
                for(int j = 0;j < mainPathList[i].Count -1;j++)
                {
                    Line tmpLine = new Line(mainPathList[i][j]._pt, mainPathList[i][j+1]._pt);
                    lineList.Add(tmpLine);
                }

            }
            for (int i = 0; i < subPathList.Count; i++)
            {
                for(int j = 0;j < subPathList[i].Count - 1;j ++)
                {
                    Line tmpLine = new Line(subPathList[i][j]._pt, subPathList[i][j+1]._pt);
                    lineList.Add(tmpLine);
                }

            }
            return lineList;
        }

        private void GetFireHydrantSysInput(Point3dCollection selectArea,ref FireHydrantSystemIn fireHydrantSysIn)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var lineList = new List<Line>();//管段列表
                var pointList = new List<Point3dEx>();//点集

                var ptVisit = new Dictionary<Point3dEx, bool>();//访问标志

                var pipeEngine = new ThExtractHYDTPipeService();//提取供水管
                var dbObjs = pipeEngine.Extract(acadDatabase.Database, selectArea);
                PipeLine.AddPipeLine(dbObjs, ref fireHydrantSysIn, ref pointList, ref lineList);

                PipeLineList.PipeLineAutoConnect(ref lineList);//管线自动连接

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

                var hydrantEngine = new ThExtractHydrant();//提取消火栓管段末端
                var hydrantDB = hydrantEngine.Extract(acadDatabase.Database, selectArea);
                fireHydrantSysIn.hydrantPosition = hydrantEngine.CreatePointList();


                var markEngine = new ThExtractPipeMark();//提取消火栓环管标记
                var mark = markEngine.Extract(acadDatabase.Database, selectArea);
                var pipeMarkSite = markEngine.GetPipeMarkPoisition();


                foreach (var pms in pipeMarkSite)
                {
                    var markL = new List<Line>();
                    foreach (var v in pms)
                    {
                        markL.Add(PointCompute.PointOnLine(v, lineList));
                    }
                    fireHydrantSysIn.markLineList.Add(markL);
                }


                var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
                var labelDB = labelEngine.Extract(acadDatabase.Database, selectArea);
                var labelLine = labelEngine.CreateLabelLineList();

                var textEngine = new ThExtractLabelText();//提取文字
                textEngine.Extract(acadDatabase.Database);
                var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textEngine.Results.ToCollection());

                var fireHydrantEngine = new ThExtractFireHydrant();
                fireHydrantEngine.Extract(acadDatabase.Database);
                var fhSpatialIndex = new ThCADCoreNTSSpatialIndex(fireHydrantEngine.Results.ToCollection());


                foreach (var pt in fireHydrantSysIn.hydrantPosition)
                {
                    var flag = false;
                    var tpt = new Point3dEx(new Point3d());
                    foreach (var p in pointList)
                    {
                        if (p._pt.DistanceTo(pt._pt) < 200)
                        {
                            tpt = p;
                            flag = true;
                        }

                    }
                    if (!flag)
                    {
                        ;
                    }
                    var termPoint = new TermPoint(pt);
                    termPoint.SetLines(labelLine);
                    termPoint.SetPipeNumber(textSpatialIndex);
                    termPoint.SetType(fhSpatialIndex);
                    fireHydrantSysIn.termPointDic.Add(tpt, termPoint);
                }

                foreach (var pt in pointList)
                {
                    if (fireHydrantSysIn.ptDic[pt].Count == 1)
                    {
                        if (!fireHydrantSysIn.termPointDic.ContainsKey(pt))
                        {
                            var termPoint = new TermPoint(pt);
                            termPoint.Type = 2;
                            termPoint.PipeNumber = " ";
                            fireHydrantSysIn.termPointDic.Add(pt, termPoint);

                        }
                    }
                }
            }
        }
    }
}
