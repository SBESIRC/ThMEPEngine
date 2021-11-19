﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Command;
using ThMEPHVAC.FanConnect.Model;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService;
using NFox.Cad;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThCreatePathServiece
    {
        public List<Line> TrunkLines { set; get; }//干路线
        public List<Line> BranchLines { set; get; }//支干路
        public List<Polyline> EquipmentObbs { set; get; }//可穿越区域，但是必须垂直连接且代价大(设备框)
        public List<Polyline> ObstacleRooms { set; get; }//可穿越区域，但是必须垂直穿越且代价大(房间框线)
        public List<Polyline> ObstacleHoles { set; get; }//不可穿越区域
        private ThCADCoreNTSSpatialIndex HoleIndex;
        private ThCADCoreNTSSpatialIndex RoomIndex;
        public void InitData()
        {
            HoleIndex = new ThCADCoreNTSSpatialIndex(ObstacleHoles.ToCollection());
            RoomIndex = new ThCADCoreNTSSpatialIndex(ObstacleRooms.ToCollection());
        }
        public Polyline CreatePath(ThFanCUModel model)
        {
            var retLine = new Polyline();
            //选择两条距离设备最近的线
            var nearLines = ThFanConnectUtils.GetNearbyLine(model.FanPoint, TrunkLines, 2);

            var pathList = new List<Polyline>();
            foreach (var l in nearLines)
            {
                var tmpPath = CreatePath(model, l);
                if (tmpPath != null)
                {
                    pathList.Add(tmpPath);
                }
            }

            //从pathList里面，挑选一条
            return retLine;
        }

        public Polyline CreatePath(ThFanCUModel model, Line line)
        {
            //根据model的类型，先走一步
            var stepPt = TakeStep(model.FanObb, model.FanPoint,500);
            //根据model位置和line，构建一个框frame
            var frame = ThFanConnectUtils.CreateMapFrame(line, stepPt,10000);
            //提取frame里面的hole和room
            var dbHoles = HoleIndex.SelectCrossingPolygon(frame);
            var holes = new List<Polyline>();
            foreach (var dbHole in dbHoles)
            {
                if (dbHole is Polyline)
                {
                    holes.Add(dbHole as Polyline);
                }
            }
            var dbRooms = RoomIndex.SelectCrossingPolygon(frame);
            var rooms = new List<Line>();
            foreach (var room in dbRooms)
            {
                if (room is Line)
                {
                    rooms.Add(room as Line);
                }
            }
            //----简单的一条延伸线且不穿洞
            var retLine = CreateSymbolLine(frame, line, stepPt, holes, rooms);
            if (retLine != null)
            {
                return retLine;
            }
            else
            {
                //使用A*算法，跑出路径
                retLine = GetPathByAStar(frame, line, stepPt, holes, rooms);
            }
            return retLine;
        }

        /// <summary>
        /// 往前走一步
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="pt"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public Point3d TakeStep(Polyline pl, Point3d pt, double step)
        {
            var retPt = new Point3d();
            var lines = (pl.Buffer(step)[0] as Polyline).ToLines();
            double minDist = double.MaxValue;
            foreach(var l in lines)
            {
                double tmpDist = ThFanConnectUtils.DistanceToPoint(l, pt);
                if(tmpDist < minDist)
                {
                    minDist = tmpDist;
                    retPt = l.GetClosestPointTo(pt, false);
                }
            }
            return retPt;
        }

        private Polyline CreateSymbolLine(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Line> rooms)
        {
            var closetPt = closetLane.GetClosestPointTo(startPt, false);
            Vector3d dir = Vector3d.ZAxis.CrossProduct((closetPt - startPt).GetNormal());
            if ((closetLane.EndPoint - closetLane.StartPoint).GetNormal().IsParallelTo(dir, new Tolerance(0.001, 0.001)))
            {
                Polyline line = new Polyline();
                line.AddVertexAt(0, startPt.ToPoint2D(), 0, 0, 0);
                line.AddVertexAt(1, closetPt.ToPoint2D(), 0, 0, 0);
                if (!ThFanConnectUtils.LineIntersctBySelect(holes, line, 50)
                    && !ThFanConnectUtils.LineIntersctBySelect(rooms, line)
                    && !ThFanConnectUtils.CheckIntersectWithFrame(line, frame))
                {
                    return line;
                }
            }

            return null;
        }

        public Polyline GetPathByAStar(Polyline frame, Line closetLane, Point3d startPt, List<Polyline> holes, List<Line> rooms)
        {
            //----初始化寻路类
            var dir = (closetLane.EndPoint - closetLane.StartPoint).GetNormal();
            AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(frame, dir, closetLane, 400, 300, 50);
            var costGetter = new ToLineCostGetterEx();
            aStarRoute.costGetter = costGetter;
            //----设置障碍物
            aStarRoute.SetObstacle(holes);
            //----设置房间
            aStarRoute.SetRoom(rooms);
            //----计算路径
            var path = aStarRoute.Plan(startPt);

            return path;
        }
    }
}
