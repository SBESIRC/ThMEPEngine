using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.SprinklerConnect.Engine;
using ThMEPWSS.SprinklerConnect.Model;

using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerPiping.Service;

namespace ThMEPWSS.SprinklerPiping.Engine
{
    class SprinklerPipingEngine
    {
        public static void GetStartPointChoices(SprinklerPipingParameter parameter, SprinklerTreeNode parent)
        {
            parent.dir = parameter.startDirection.GetNormal();
            parent.endPos = parameter.startPoint + parent.dir * parameter.startLen;
            //foreach(var pipe in parent.state.pipes)
            //{
            //    //TODO: 可能需要从起点分出多条路
            //}
            GetConnectingChoices(parameter, parent);

        }

        public static void GetConnectingChoices(SprinklerPipingParameter parameter, SprinklerTreeNode parent) //TODO: intersected
        {
            Vector3d initDir = parent.dir;
            //bool intersected = false;
            for (int i = 0; i < 3; i++)
            {
                bool intersected = false;
                bool breakFlag = false;
                bool extendFlag = false;
                SprinklerTreeState childState = new SprinklerTreeState(parent.state);
                childState.choices = new List<SprinklerTreeNode>();
                SprinklerTreeNode child = new SprinklerTreeNode(childState, parent, SprinklerTreeNode.nodeType.connecting);
                child.dir = initDir.RotateBy(Math.PI / 2 * (Math.Pow(2, i) - 1), Vector3d.ZAxis);
                
                child.endPos = parent.endPos + parameter.dttol * child.dir;
                Line newPipe = new Line(parent.endPos, child.endPos);
                //bool iii = false;
                //int extendFlag = 0;
                //child.state.pipes.Add(new SprinklerPipe(newPipe, true));//TODO:pipe assigned & point assigned

                //是否和前面相交
                foreach (var pipe in child.state.pipes)
                {
                    List<Point3d> listInter = SprinklerPipingAssist.ExtendLineSingleDir(newPipe, 1).Intersect(pipe.pipe, 0);
                    //if (newPipe.LineIsIntersection(pipe.pipe))
                    if (pipe.pipe.GetClosestPointTo(newPipe.EndPoint, false).DistanceTo(newPipe.EndPoint) == 0 && pipe.pipe.Angle == newPipe.Angle && pipe.assigned)
                    {
                        breakFlag = true;
                        break;
                    }
                    if (listInter.Count > 0)
                    {
                        //if (listInter[0] == newPipe.StartPoint)
                        if (listInter[0].IsEqualTo(newPipe.StartPoint, new Tolerance(1, 1)))//TODO
                        {
                            break;
                        }
                        //if (pipe.pipe.EndPoint.IsEqualTo(newPipe.StartPoint, new Tolerance(1, 1)))
                        //{
                        //    break;
                        //}
                        if (pipe.assigned)
                        {
                            //成环 TODO:剪枝
                            //bug：从原有车位线出发时被剪掉了 永远无法terminal
                            breakFlag = true;
                            break;
                        }
                        else
                        {
                            //如果有多段相交 取最远交点
                            //List<Point3d> l = newPipe.Intersect(pipe.pipe, 0);
                            //Point3d newIntersect = newPipe.Intersect(pipe.pipe, 0)[0];
                            if (intersected)
                            {
                                child.endPos = (parent.endPos.DistanceTo(child.endPos) > parent.endPos.DistanceTo(listInter[0])) ? child.endPos : listInter[0];
                            }
                            else
                            {
                                child.endPos = listInter[0];
                                intersected = true;
                            }
                            pipe.assigned = true;
                        }
                        //if(child.state.pipes.ToList()[0].assigned && child.state.pipes.ToList()[1].assigned && child.state.pipes.ToList()[2].assigned)
                        //{
                        //    iii = true;
                        //    break;
                        //}
                    }
                }

                if (breakFlag) continue;

                child.len += 1;

                if (i == 0)
                {
                    //合并line
                    //check: 为什么过线的时候不生效
                    foreach(var pipe in child.state.pipes)
                    {
                        if (pipe.pipe.EndPoint.IsEqualTo(parent.endPos, new Tolerance(1, 1)) && pipe.pipe.Angle == newPipe.Angle)
                        {
                            child.state.pipes.Remove(pipe);
                            child.state.pipes.Add(new SprinklerPipe(new Line(pipe.pipe.StartPoint, child.endPos), pipe.assigned));
                            extendFlag = true;
                            break;
                        }
                    }
                    if (!intersected)
                    {
                        child.initWeight = 10; //如果是从管子上找点引线 不加不弯的权重 TODO:权重
                    }
                }
                else
                {
                    child.turnCnt++;
                }

                if (intersected) child.initWeight = 20;

                if(!extendFlag)
                {

                    child.state.pipes.Add(new SprinklerPipe(new Line(parent.endPos, child.endPos), true));
                }
                if (parameter.frame.Contains(child.endPos))
                {
                    //TODO:中间障碍物（墙 房间等，穿墙要计数）
                    parent.state.choices.Add(child);
                }
                //if (iii) break;
            }
            parent.isFullyExpanded = true;
            if(parent.state.choices.Count == 0)
            {
                parent.isTerminal = true;
            }
        }

        //public static void GetDirChoice(SprinklerPipingParameter parameter, SprinklerTreeNode parent, Vector3d initDir, bool intersected)
        //{
            
        //    for(int i=0; i<3; i++)
        //    {
        //        SprinklerTreeState childState = new SprinklerTreeState(parent.state);
        //        childState.choices = new List<SprinklerTreeNode>();
        //        SprinklerTreeNode child = new SprinklerTreeNode(childState, parent, SprinklerTreeNode.nodeType.direction);
        //        child.dir = initDir.RotateBy(Math.PI / 2 * (Math.Pow(2, i) - 1), Vector3d.ZAxis);
        //        //if (i == 0 && !intersected)
        //        //{
        //        //    child.initWeight = 10; //如果是从管子上找点引线 不加不弯的权重
        //        //}
        //        if(i != 0)
        //        {
        //            child.endPos = parent.endPos + parent.len * parameter.dttol * parent.dir;

        //        }
        //        parent.state.choices.Add(child);
        //    }
        //}

        //public static void GetLenChoice(SprinklerTreeNode parent)
        //{
        //    Vector3d dir = parent.dir;
        //    Point3d endPos = parent.endPos;
        //    for (int len = 0; ; len++)
        //    {
        //        //TODO:中间障碍物（墙 房间等，穿墙要计数）
        //    }
        //}

        public static void ParkingPiping(SprinklerPipingParameter parameter, SprinklerTreeNode parent)
        {
            //List<SprinklerTreeNode> children = new List<SprinklerTreeNode>();
            //SprinklerTreeState initState = new SprinklerTreeState(parent.state);

            List<Polyline> parkingRows = parameter.parkingRows;
            List<SprinklerParkingRow> sprinklerParkingRows = parameter.sprinklerParkingRows;
            List<Point3d> pts = parameter.pts;
            List<SprinklerPoint> sprinklerPoints = parameter.sprinklerPoints;

            //TODO: 多ucs
            int cntXParking = 0;
            int cntYParking = 0;

            Dictionary<string, int> xvote = new Dictionary<string, int> { { "up", 0 }, { "down", 0 } };
            Dictionary<string, int> yvote = new Dictionary<string, int> { { "up", 0 }, { "down", 0 } };

            //构建parameter.sprinklerParkingRows
            foreach (var parkingRow in parkingRows)
            {
                SprinklerParkingRow curRow = new SprinklerParkingRow(parkingRow, parameter);
                sprinklerParkingRows.Add(curRow);
                if (curRow.isX)
                {
                    cntXParking++;
                    xvote["up"] += curRow.vote[0];
                    xvote["down"] += curRow.vote[1];
                    //if (curRow.againstWall)
                    //{
                    //    if (curRow.parkingRow.GetPoint3dAt(3).Y >= curRow.parkingRow.GetPoint3dAt(0).Y ^ curRow.wallDir == 0)
                    //        xvote["down"]++;
                    //    else
                    //        xvote["up"]++;
                    //}
                }
                else
                {
                    cntYParking++;
                    yvote["up"] += curRow.vote[0];
                    yvote["down"] += curRow.vote[1];
                    //if (curRow.againstWall)
                    //{
                    //    if (curRow.parkingRow.GetPoint3dAt(3).Y >= curRow.parkingRow.GetPoint3dAt(0).Y ^ curRow.wallDir == 0)
                    //        yvote["down"]++;
                    //    else
                    //        yvote["up"]++;
                    //}
                }
            }

            //画管道
            List<Line> newPipes = new List<Line>();
            HashSet<Point3d> ptIdxList = new HashSet<Point3d>();
            if (cntXParking >= cntYParking)
            {
                //up:1 down:-1 arbitrary:0
                if (xvote["up"] > 0 && xvote["down"] == 0)
                {
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 1 : 0, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);
                }
                else if (xvote["up"] == 0 && xvote["down"] > 0)
                {
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? -1 : 0, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);
                }
                else
                {
                    //Random rand = new Random();
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        //int randPos = (int)((rand.Next(2) - 0.5) * 2);
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? -1 : 0, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);

                    newPipes = new List<Line>();
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        //int randPos = (int)((rand.Next(2) - 0.5) * 2);
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 1 : 0, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);

                }
            }
            else
            {
                //up:1 down:-1 arbitrary:0
                if (yvote["up"] > 0 && yvote["down"] == 0)
                {
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        if(sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 0 : 1, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                        
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);
                }
                else if (yvote["up"] == 0 && yvote["down"] > 0)
                {
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 0 : -1, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);
                }
                else
                {
                    //Random rand = new Random();
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        //int randPos = (int)((rand.Next(2) - 0.5) * 2);
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 0 : -1, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);

                    newPipes = new List<Line>();
                    foreach (var sprinklerParkingRow in sprinklerParkingRows)
                    {
                        //int randPos = (int)((rand.Next(2) - 0.5) * 2);
                        if (sprinklerParkingRow.choices.Count != 0)
                        {
                            newPipes.Add(sprinklerParkingRow.Select(sprinklerParkingRow.isX ? 0 : 1, out HashSet<Point3d> ptList));
                            ptIdxList.UnionWith(ptList);
                        }
                    }
                    AddAndAssign(parent, newPipes, ptIdxList, SprinklerTreeNode.nodeType.parking);
                }
            }
            parent.isFullyExpanded = true;

        }

        public static void CorridorPiping(SprinklerPipingParameter parameter, SprinklerTreeNode parent)
        {
            List<Point3d> pts = parameter.pts;
            List<SprinklerPoint> sprinklerPoints = parameter.sprinklerPoints;
        }

        public static void AddChoice(SprinklerTreeNode parent, List<Line> newPipes, SprinklerTreeNode.nodeType type)
        {
            SprinklerTreeState initState = new SprinklerTreeState(parent.state);
            SprinklerTreeState curState = new SprinklerTreeState(initState, newPipes);
            SprinklerTreeNode newChild = new SprinklerTreeNode(curState, parent, type);
            parent.state.choices.Add(newChild);
        }

        public static void AddAndAssign(SprinklerTreeNode parent, List<Line> newPipes, HashSet<Point3d> ptList, SprinklerTreeNode.nodeType type)
        {
            SprinklerTreeState initState = new SprinklerTreeState(parent.state);
            SprinklerTreeState curState = new SprinklerTreeState(initState, newPipes);
            curState.AssignPts(ptList);
            //foreach(var idx in ptIdxList)
            //{
            //    curState.sprinklerPoints[idx].assigned = true;
            //}
            SprinklerTreeNode newChild = new SprinklerTreeNode(curState, parent, type);
            parent.state.choices.Add(newChild);
        }
    }
}
