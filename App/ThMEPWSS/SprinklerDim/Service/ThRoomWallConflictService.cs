using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Diagnostics;
using ThCADExtension;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThRoomWallConflictService
    {
        /// <summary>
        /// 房间重新分隔出net group
        /// </summary>
        /// <param name="netList"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        public static List<ThSprinklerNetGroup> ReGroupByRoom(List<ThSprinklerNetGroup> netList, List<Polyline> rooms, string printTag)
        {
            List<MPolygon> roomList = PreprocessRooms(rooms);

            List<ThSprinklerNetGroup> newNetList = new List<ThSprinklerNetGroup>();
            if (roomList.Count > 0)
            {
                foreach (ThSprinklerNetGroup net in netList)
                {
                    // 获取所有线
                    DBObjectCollection lines = new DBObjectCollection();
                    foreach (ThSprinklerGraph graph in net.PtsGraph)
                    {
                        foreach(Line l in graph.Print(net.Pts))
                            lines.Add(l);
                    }
                    ThCADCoreNTSSpatialIndex linesSI = new ThCADCoreNTSSpatialIndex(lines);

                    // 房间框线框住的线重新生成net group
                    for(int i = 0; i < roomList.Count; i++)
                    {
                        MPolygon room = roomList[i];
                        if (room == null)
                            continue;

                        // 获取window line
                        List<Line> selectWindowLines = new List<Line>();
                        DBObjectCollection dbSelect = linesSI.SelectWindowPolygon(room);
                        foreach(DBObject dbo in dbSelect)
                        {
                            selectWindowLines.Add((Line)dbo);
                        }

                        if (selectWindowLines.Count > 0)
                        {
                            // 获取fence line
                            List<Line> selectFenceLines = new List<Line>();
                            DBObjectCollection tdbSelect = linesSI.SelectFence(room);
                            foreach (DBObject dbo in tdbSelect)
                            {
                                selectFenceLines.Add((Line)dbo);
                            }

                            // 加入满足首尾两点被框进房间的fence line到window line
                            if(selectFenceLines.Count > 0)
                            {
                                foreach(Line l in selectFenceLines)
                                {
                                    if(IsContained(room, l.StartPoint) && IsContained(room, l.EndPoint))
                                    {
                                        selectWindowLines.Add(l);
                                    }
                                }

                            }

                            newNetList.Add(ThSprinklerNetGraphService.CreateNetwork(net.Angle, selectWindowLines));
                        }

                    }

                }

            }
            else
                newNetList = netList;


            //test
            for (int i = 0; i < newNetList.Count; i++)
            {
                var net = newNetList[i];
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(pts);
                    DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-1Room-{0}-{1}", i, j, printTag), i % 7);
                }
            }


            return newNetList;
        }


        private static List<MPolygon> PreprocessRooms(List<Polyline> rooms)
        {
            // 按面积从大到小排房间
            List<MPolygon> roomList = new List<MPolygon>();
            foreach(Polyline room in rooms)
            {
                MPolygon mPolygon = ThMPolygonTool.CreateMPolygon(room);
                roomList.Add(mPolygon);
            }
            roomList.Sort((x, y) => 0 - x.Area.CompareTo(y.Area));

            for (int i = 0; i < roomList.Count; i++)
            {
                for(int j = i+1; j<roomList.Count; j++)
                {
                    DBObjectCollection dboc = new DBObjectCollection() { roomList[j] };
                    DBObjectCollection dbocd = roomList[i].DifferenceMP(dboc);
                    
                    if (dbocd.Count > 0)
                    {
                        roomList[i] = dbocd.OfType<MPolygon>().OrderByDescending(x => x.Area).FirstOrDefault();
                    }
                    else
                    {
                        roomList[i] = null;
                        break;
                    }

                    // dbocd.OfType<MPolygon>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "AAA"));

                }
            }

            return roomList;
        }

        private static bool IsContained(MPolygon room, Point3d pt)
        {
            if(!ThCADCoreNTSPolygonExtension.Contains(room.Shell(), pt))
            {
                return false;
            }

            List<Polyline> holes = room.Holes();
            foreach(Polyline hole in holes)
            {
                if (ThCADCoreNTSPolygonExtension.Contains(hole, pt))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 根据墙断开不能连接的线，并记录下来
        /// </summary>
        /// <param name="transNetList"></param>
        /// <param name="walls"></param>
        /// <param name="printTag"></param>
        public static void CutOffLinesByWall(List<ThSprinklerNetGroup> transNetList, List<Polyline> walls, string printTag)
        {
            for(int idx = 0; idx < transNetList.Count; idx++)
            {
                ThSprinklerNetGroup net = transNetList[idx];
                net.LinesCuttedOffByWall.Clear();
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());

                //生成所有线（图）
                DBObjectCollection graphLines = new DBObjectCollection();
                foreach (ThSprinklerGraph graph in net.PtsGraph)
                {
                    foreach (Line l in graph.Print(pts))
                        graphLines.Add(l);
                }
                ThCADCoreNTSSpatialIndex graphLinesSI = new ThCADCoreNTSSpatialIndex(graphLines);

                //找出图与墙相交线
                List<Line> crossWallLines = new List<Line>();
                foreach (Polyline wall in walls)
                {
                    DBObjectCollection dbSelect = graphLinesSI.SelectFence(wall);
                    foreach (DBObject dbo in dbSelect)
                    {
                        crossWallLines.Add((Line)dbo);
                    }

                }

                //判断相交线是否需要断开
                foreach (Line line in crossWallLines)
                {
                    if(IsConflicted(line, walls))
                    {
                        int i = SearchIndex(pts, line.StartPoint);
                        int j = SearchIndex(pts, line.EndPoint);
                        net.LinesCuttedOffByWall.Add(new Tuple<int, int>(i, j));
                        net.LinesCuttedOffByWall.Add(new Tuple<int, int>(j, i));

                        foreach(ThSprinklerGraph graph in net.PtsGraph)
                        {
                            graph.DeleteEdge(i, j);
                            graph.DeleteEdge(j, i);
                        }
                    }

                }

            }

            //test
            for (int i = 0; i < transNetList.Count; i++)
            {
                var net = transNetList[i];
                List<Point3d> pts = ThChangeCoordinateService.MakeTransformation(net.Pts, net.Transformer.Inverse());
                for (int j = 0; j < net.PtsGraph.Count; j++)
                {
                    var lines = net.PtsGraph[j].Print(pts);
                    DrawUtils.ShowGeometry(lines, string.Format("SSS-{2}-4Wall-{0}-{1}", i, j, printTag), i % 7);
                }
            }
        }

        private static int SearchIndex(List<Point3d> pts, Point3d pt)
        {
            return pts.IndexOf(pt);
        }


        public static bool IsConflicted(Line line, List<Polyline> walls, double tolerance=200.0)
        {
            //生成所有线（墙）
            DBObjectCollection wallLines = new DBObjectCollection();
            foreach (Polyline wall in walls)
            {
                for (int i = 0; i < wall.NumberOfVertices; i++)
                {
                    wallLines.Add(new Line(wall.GetPoint3dAt(i), wall.GetPoint3dAt((i + 1) % wall.NumberOfVertices)));
                }

            }
            ThCADCoreNTSSpatialIndex wallLinesSI = new ThCADCoreNTSSpatialIndex(wallLines);

            //穿图的墙线
            List<Line> crossGraphLines = new List<Line>();
            DBObjectCollection dbSelect = wallLinesSI.SelectFence(line);
            foreach (DBObject dbo in dbSelect)
            {
                crossGraphLines.Add((Line)dbo);
            }

            if (crossGraphLines.Count > 0)
            {
                //把crossGraphLines转换为同方向
                Vector3d dir = (crossGraphLines[0].StartPoint - crossGraphLines[0].EndPoint).GetNormal();
                for (int i = 1; i < crossGraphLines.Count; i++)
                {
                    Vector3d tDir = (crossGraphLines[i].StartPoint - crossGraphLines[i].EndPoint).GetNormal();
                    if (dir.DotProduct(tDir) < 0)
                    {
                        crossGraphLines[i] = new Line(crossGraphLines[i].EndPoint, crossGraphLines[i].StartPoint);
                    }

                }

                //图线两边的距离最大值，两边最大值中取最小值
                double distance1 = 0;
                double distance2 = 0;
                foreach (Line l in crossGraphLines)
                {
                    Point3d pt1 = line.GetClosestPointTo(l.StartPoint, true);
                    Point3d pt2 = line.GetClosestPointTo(l.EndPoint, true);

                    double td1 = pt1.DistanceTo(l.StartPoint);
                    double td2 = pt2.DistanceTo(l.EndPoint);

                    if (distance1 < td1)
                        distance1 = td1;
                    if (distance2 < td2)
                        distance2 = td2;
                }

                if (Math.Min(distance1, distance2) > tolerance)
                    return true;
            }

            return false;
        }

    }
}
