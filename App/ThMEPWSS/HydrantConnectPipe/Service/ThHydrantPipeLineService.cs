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
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundFireHydrantSystem.Extract;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS.HydrantConnectPipe.Service
{
    public class ThHydrantPipeLineService
    {
        public void GetHydrantLoopAndBranchLines(ref List<Line> loopLines, ref List<Line> branchLines, Point3d startPt,  Point3dCollection selectArea)
        {
            //获取环管标记点
            var pipeMarkEngine = new ThHydrantPipeMarkRecognitionEngine();
            pipeMarkEngine.Extract(selectArea);
            var pipeMarks = pipeMarkEngine.GetPipeMarks();

            var fireHydrantSysIn = new FireHydrantSystemIn();//输入参数
            List<Line> pipeLines = GetPipeLines(ref fireHydrantSysIn, startPt,selectArea);
            loopLines = GetMainPipeLines(pipeLines,pipeMarks, fireHydrantSysIn);
            branchLines = pipeLines.Except(loopLines).ToList();
            loopLines = ThHydrantConnectPipeUtils.CleanLines(loopLines);
            branchLines = ThHydrantConnectPipeUtils.CleanLines(branchLines);
            loopLines = PipeLineList.CleanLaneLines3(loopLines);
            branchLines = PipeLineList.CleanLaneLines3(branchLines);
//            loopLines.RemoveAll(l => IsMarkLine(l, pipeMarks));
        }
        private bool IsMarkLine(Line l, List<ThHydrantPipeMark> marks)
        {
            foreach (var mark in marks)
            {
                if ((l.PointOnLine(mark.StartPoint, false, 100) && l.PointOnLine(mark.StartPoint, true, 10))
                  || (l.PointOnLine(mark.EndPoint, false, 100) && l.PointOnLine(mark.EndPoint, true, 10)))
                {
                    return true;
                }
            }
            return false;
        }
        private void RemovePipeLines(ref List<Line> pipeLines, ref FireHydrantSystemIn fireHydrantSysIn, List<ThHydrantPipeMark> marks)
        {
            //遍历邻接点为1的线
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
                if (fireHydrantSysIn.PtDic.ContainsKey(startPtEx))
                {
                    var startPtNeighbors = fireHydrantSysIn.PtDic[startPtEx];
                    if (startPtNeighbors.Count == 1)
                    {
                        tmpLine.Add(l);
                        continue;
                    }
                }

                //end
                if (fireHydrantSysIn.PtDic.ContainsKey(endPtEx))
                {
                    var startPtNeighbors = fireHydrantSysIn.PtDic[endPtEx];
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
                    if (fireHydrantSysIn.PtDic.ContainsKey(startPtEx))
                    {
                        var startPtNeighbors = fireHydrantSysIn.PtDic[startPtEx];
                        if (startPtNeighbors.Count == 1)
                        {
                            pipeLines.Remove(l);
                            fireHydrantSysIn.PtDic[endPtEx].Remove(startPtEx);
                            continue;
                        }
                    }

                    //end
                    if (fireHydrantSysIn.PtDic.ContainsKey(endPtEx))
                    {
                        var startPtNeighbors = fireHydrantSysIn.PtDic[endPtEx];
                        if (startPtNeighbors.Count == 1)
                        {
                            pipeLines.Remove(l);
                            fireHydrantSysIn.PtDic[startPtEx].Remove(endPtEx);
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
        private List<Line> GetPipeLines(ref FireHydrantSystemIn fireHydrantSysIn, Point3d startPt, Point3dCollection selectArea) 
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
                var tmpLines = ThHydrantConnectPipeUtils.FindInlineLines(startPt, ref lineList, 10);
                pointList.Clear();
                var starPts = tmpLines.Select(l=> new Point3dEx(l.StartPoint)).ToList();
                var endPts = tmpLines.Select(l => new Point3dEx(l.EndPoint)).ToList();
                pointList.AddRange(starPts);
                pointList.AddRange(endPts);

                PipeLine.PipeLineSplit(ref tmpLines, pointList,1.0,2.0);//管线打断

                fireHydrantSysIn.PtDic = new Dictionary<Point3dEx, List<Point3dEx>>();//清空  当前点和邻接点字典对
                foreach (var L in tmpLines)
                {
                    var pt1 = new Point3dEx(L.StartPoint,12);
                    var pt2 = new Point3dEx(L.EndPoint,12);
                    ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2);
                }

                return tmpLines;
            }
        }
        public void RemoveBranchLines(List<Line> branchLines, List<Line> loopLines, List<BlockReference> valves, List<BlockReference> pipeMarks, Point3dCollection selectArea)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var lineEngine = new ThHydrantLineRecognitionEngine();//提取供水管
                lineEngine.Extract(selectArea);
                var dbObjs = lineEngine.Dbjs;
                foreach (Entity dbj in dbObjs)
                {
                    var startPt = new Point3d();
                    var entPt = new Point3d();
                    if (IsTianZhengElement(dbj))
                    {
                        GetTianZhengLinePt(dbj, out startPt, out entPt);
                    }
                    else
                    {
                        if (dbj is Line)
                        {
                            startPt = (dbj as Line).StartPoint;
                            entPt = (dbj as Line).EndPoint;
                        }
                    }
                    foreach (var l in branchLines)
                    {
                        var box = l.Buffer(10);
                        box = box.Buffer(1.0)[0] as Polyline;
                        var tmpLine = new Line(startPt, entPt);
                        if(!tmpLine.IsParallelToEx(l))
                        {
                            continue;
                        }
                        tmpLine.Dispose();
                        if (box.Contains(startPt) && box.Contains(entPt))
                        {
                            dbj.UpgradeOpen();
                            dbj.Erase();
                            dbj.DowngradeOpen();
                            break;
                        }
                        else if(box.Contains(startPt) && !box.Contains(entPt))
                        {
                            //移动startPt到box边缘
                            var tmpPts = box.IntersectWithEx(dbj);
                            MoveLine(dbj, tmpPts[0], entPt);
                            break;
                        }
                        else if (!box.Contains(startPt) && box.Contains(entPt))
                        {
                            //移动entPt到box边缘
                            var tmpPts = box.IntersectWithEx(dbj);
                            MoveLine(dbj, startPt, tmpPts[0]);
                            break;
                        }
                    }
                }
                foreach (var v in valves)
                {
                    foreach (var l in branchLines)
                    {
                        var box = l.Buffer(10);
                        box = box.Buffer(1.0)[0] as Polyline;
                        if (box.Contains(v.Position))
                        {
                            v.UpgradeOpen();
                            v.Erase();
                            v.DowngradeOpen();
                        }
                    }
                }
                foreach (var m in pipeMarks)
                {
                    foreach (var l in branchLines)
                    {
                        var box = l.Buffer(200);
                        box = box.Buffer(100.0)[0] as Polyline;
                        if (box.Contains(m.Position))
                        {
                            m.UpgradeOpen();
                            m.Erase();
                            m.DowngradeOpen();
                        }
                    }
                }
            }
        }
        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }

        private void GetTianZhengLinePt(Entity ent,out Point3d startPt,out Point3d endPt)
        {
            var pt1 = ent.GetType().GetProperty("StartPoint");
            var pt2 = ent.GetType().GetProperty("EndPoint");
            if(pt1 != null && pt2 != null)
            {
                startPt = (Point3d)pt1.GetValue(ent);
                endPt = (Point3d)pt2.GetValue(ent);
            }
            else
            {
                List<Point3d> pts = new List<Point3d>();
                foreach (Entity l in ent.ExplodeToDBObjectCollection())
                {
                    if(l is Polyline)
                    {
                        pts.Add((l as Polyline).StartPoint);
                        pts.Add((l as Polyline).EndPoint);
                    }
                    else if(l is Line)
                    {
                        pts.Add((l as Line).StartPoint);
                        pts.Add((l as Line).EndPoint);
                    }
                }
                var pairPt = pts.GetCollinearMaxPts();
                startPt = pairPt.Item1;
                endPt = pairPt.Item2;
            }
        }
        private void MoveLine(Entity ent,Point3d startPt,Point3d endPt)
        {
            ent.UpgradeOpen();
            ent.Erase();
            ent.DowngradeOpen();
            using (var database = AcadDatabase.Active())
            {
                var tmpLine = new Line(startPt, endPt);
                if( database.Layers.Contains("W-FRPT-1-HYDT-PIPE"))
                {
                    tmpLine.LayerId = DbHelper.GetLayerId("W-FRPT-1-HYDT-PIPE");
                }
                else if(database.Layers.Contains("W-FRPT-HYDT-PIPE"))
                {
                    tmpLine.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE");
                }
                database.CurrentSpace.Add(tmpLine);
            }
        }
    }
}
