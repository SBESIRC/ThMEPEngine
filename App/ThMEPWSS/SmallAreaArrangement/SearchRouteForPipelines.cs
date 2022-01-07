using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Engine;
using System;

namespace THMEPWSS.RoomOrderingService
{
    public class OrderingService
    {
        public List<List<Polyline>> roomOrder { get; set; } = new List<List<Polyline>>();
        public List<Polyline> roomSet { get; set; } = new List<Polyline>();
        private List<Polyline> smallArea { get; set; } = new List<Polyline>();
        public List<Polyline> targetRoom { get; set; } = new List<Polyline>();
        public List<Polyline> shearWalls { get; set; } = new List<Polyline>();
        private HashSet<Polyline> bigArea { get; set; } = new HashSet<Polyline>();
        public List<Point3d> sprinkler { get; set; } = new List<Point3d>();
        //private DBObjectCollection shearWalls { get; set; } = new DBObjectCollection();
        private Dictionary<Polyline, List<Polyline>> nextInlayerPoly { get; set; } = new Dictionary<Polyline, List<Polyline>>();
        private Dictionary<Polyline, List<Polyline>> nextOuterLayerPoly { get; set; } = new Dictionary<Polyline, List<Polyline>>();
        private Dictionary<KeyValuePair<Polyline, Polyline>, List<Polyline>> penetrableWalls { get; set; } = new Dictionary<KeyValuePair<Polyline, Polyline>, List<Polyline>>();
        public Dictionary<Polyline, List<Point3d>> sprinklerInRoom { get; set; } = new Dictionary<Polyline, List<Point3d>>();
        public List<List<Line>> gridLines { get; set; } = new List<List<Line>>();
        public List<List<Line>> orthogonalGrid { get; set; } = new List<List<Line>>();

        //key为外层房间，value为里层房间
        public Dictionary<KeyValuePair<Polyline, Polyline>, KeyValuePair<Point3d,double> > penetrablePoint { get; set; } = new Dictionary<KeyValuePair<Polyline, Polyline>, KeyValuePair<Point3d, double>>();
        public Dictionary<Polyline, List<Line>> piplineToRoom { get; set; } = new Dictionary<Polyline, List<Line>>();
        public Dictionary<Polyline, List<ThSprinklerNetGroup>> sprinklerParameterList { get; set; } = new Dictionary<Polyline, List<ThSprinklerNetGroup>>();
        public List<Line> subPipelineList { get; set; } = new List<Line>();
        private double boundary { get; set; }
        public void PipelineArrange(List<Polyline> rooms, List<Polyline> geometry, double bound)
        {
            boundary = bound;
            //shearwall.ForEach(o=>shearWalls.Add(o));
            smallArea = rooms.Where(r => r.Area < boundary).ToList();
            bigArea = rooms.Where(r => r.Area > boundary).ToHashSet();
            //var spatialShearwallIndex = new ThCADCoreNTSSpatialIndex(shearWalls);
            //记录房间相应喷淋点位,将有喷淋的放入targetRoom
            smallArea.ForEach(r =>
            {
                sprinkler.ForEach(pt =>
                {
                    if (r.Contains(pt))
                    {
                        if (sprinklerInRoom.ContainsKey(r))
                        {
                            sprinklerInRoom[r].Add(pt);
                        }
                        else
                        {
                            sprinklerInRoom.Add(r, new List<Point3d> { pt });
                        }
                    }
                });
            });

           

            targetRoom = sprinklerInRoom.Keys.ToList();
            FindOrder(targetRoom);

            foreach(var room in targetRoom)
            {
                var sprinklerParameter = new ThSprinklerParameter();
                sprinklerParameter.SprinklerPt = sprinklerInRoom[room];
                var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(sprinklerParameter, geometry, out double dtTol);
                sprinklerParameterList.Add(room, netList);
                //List<Line> lines = new List<Line>();
                //netList.ForEach(o => lines.AddRange(o.lines));
                //var tree = kruskal(lines, sprinklerInRoom[room]);
                //List<List<Line>> forest = new List<List<Line>>();
                //netList.ForEach(o => forest.Add(kruskal(o.lines, sprinklerInRoom[room])));


            }

        }

        private void FindSubPipelineInRoom(Polyline room)
        {
            var outerRoom = nextOuterLayerPoly[room];
            var innerRoom = nextInlayerPoly[room];
            outerRoom.OrderBy(o => sprinklerInRoom[o].Count());
            //double denominator = 5;
            var roomBound = ThCADCoreNTSDbExtension.ToNTSPolygon(room).EnvelopeInternal;
            var lenth = roomBound.Width / 2;
            bool flag = false;
            if (innerRoom.Count == 0)
            {
                //if(sprinklerInRoom[room].Count <= 8)
                //{
                    foreach(Polyline o in outerRoom)
                    {
                        var walls = penetrableWalls[new KeyValuePair<Polyline, Polyline>(o, room)];     
                        foreach(Polyline wall in walls)
                        {
                            var angles = GetAngle(wall);
                            foreach(var angle in angles)
                            {
                                var start = getPoint(wall, room, angle, lenth);
                                if (start != wall.GetPoint3dAt(0))
                                {
                                    flag = true;
                                    break;
                                }
                                if (flag) break;
                            }
                            if (flag) break;
                        }
                    }
                //}

            }
            else
            {
                var innerRoomPoints = new List<KeyValuePair<Point3d, double>>();
                innerRoom.ForEach(x => innerRoomPoints.Add(penetrablePoint[new KeyValuePair<Polyline, Polyline> (room, x)]));
                var outerRoomAngles = new List<double>();
                foreach(Polyline o in outerRoom)
                {
                    var walls = penetrableWalls[new KeyValuePair<Polyline, Polyline>(o, room)];
                    foreach(Polyline wall in walls)
                    {
                        var outerAngles = GetAngle(wall).ToHashSet();
                        var innerRoomAngels = new HashSet<double>();
                        innerRoomPoints.ForEach(x => innerRoomAngels.Add(x.Value));
                        var intersection = new HashSet<double>(innerRoomAngels);
                        intersection.IntersectWith(outerAngles);
                        if (intersection.Count > 0)
                        {

                        }
                    }

                }
            }


        }

        private bool collision(Line l, Polyline room)
        {
            var sprinklers = sprinklerInRoom[room];
            var tol = 250;
            foreach(var o in sprinklers)
            {
                if(l.GetDistAtPoint(o) < tol)
                {
                    return false;
                }
            }
            return true;
        }
        //返回支干管交房间的点
        private Point3d getPoint(Polyline p, Polyline room, double angle, double lenth)
        {
            double tol = 1;
            var start = new Point3d();
            var denominator = 6;
            bool flag = false;
            for(int i = 0; i<p.NumberOfVertices; ++i)
            {
                var l = new Line(p.GetPoint3dAt(i), p.GetPoint3dAt(i + 1));
                if (Math.Abs(l.Angle - angle) % (Math.PI/2) < tol)
                {

                    while (!flag && denominator>0)
                    {
                        start = (l.StartPoint + l.Delta) * (1 / denominator);
                        --denominator;
                        var end = (start + new Vector3d(1, 0, 0) * lenth).RotateBy(angle, Vector3d.ZAxis, start);
                        var pipeline = new Line(start, end);
                        if (!collision(pipeline, room))
                        {
                            subPipelineList.Add(pipeline);
                            piplineToRoom[room].Add(pipeline);
                            flag = true;
                        }
                    }
                    
                }
                if (flag)
                    break;
            }
            if (!flag)
                start = p.GetPoint3dAt(0);

            return start;
        }



        private List<double> GetAngle(Polyline bound)
        {
            var result = new List<double>();
            double min = 250;
            for(int i = 0; i< bound.NumberOfVertices; ++i)
            {
                var l = new Line(bound.GetPoint3dAt(i), bound.GetPoint3dAt(i + 1));
                if (l.Length <= min) continue;
                if (!result.Contains(l.Angle))
                    result.Add(l.Angle);
            }
            return result;
        }

  
        private void FindOrder(List<Polyline> targetRoom)
        {
            var firstLayer = new List<Polyline>();
            bigArea.ToList().ForEach(o => firstLayer.AddRange(FindCorrespondingRoom(o, targetRoom)));
            roomOrder.Add(firstLayer);
            firstLayer.ForEach(o => targetRoom.Remove(o));
            while (targetRoom.Count > 0)
            {
                var temp = new List<Polyline>();
                var presentLayer = roomOrder[roomOrder.Count - 1];
                presentLayer.ForEach( o => temp.AddRange(FindCorrespondingRoom(o, targetRoom)));
                temp.ToHashSet().ToList();
                roomOrder.Add(temp);
                temp.ForEach(o => targetRoom.Remove(o));
            }
        }
        //记录相邻房间并记录可穿墙
        private List<Polyline> FindCorrespondingRoom(Polyline roomA, List<Polyline> roomSetB)
        {
            ThCADCoreNTSSpatialIndex spatialRoomIndex = new ThCADCoreNTSSpatialIndex(roomSetB.ToCollection());
            var crossingRoom = spatialRoomIndex.SelectCrossingPolygon(roomA).Cast<Polyline>().ToList();
            foreach (Polyline room in crossingRoom)
            {
                var intersect1 = ThCADCoreNTSEntityExtension.Intersection(room, roomA);
                var order = new KeyValuePair<Polyline, Polyline> (roomA, room);
                foreach (Entity interPart in intersect1)
                {
                    var difference = ThCADCoreNTSEntityExtension.Difference(interPart, shearWalls.ToCollection()).Cast<Polyline>().ToList();
                    if(difference.Count >0)
                    {
                        if (penetrableWalls.ContainsKey(order))
                        {
                            penetrableWalls[order].AddRange(difference);
                        }
                        else
                        {
                            penetrableWalls.Add(order, new List<Polyline>(difference));
                        }
                    }
                    
                }

                if (nextInlayerPoly.ContainsKey(roomA))
                {
                    nextInlayerPoly[roomA].Add(room);
                }
                else
                {
                    nextInlayerPoly.Add(roomA, new List<Polyline>{room});
                    
                }

                if (nextOuterLayerPoly.ContainsKey(room))
                {
                    nextOuterLayerPoly[room].Add(roomA);
                }
                else
                {
                    nextOuterLayerPoly.Add(room, new List<Polyline> { roomA });

                }
            }

            return crossingRoom;
        }


    }



}