using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Engine;

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
        public Dictionary<Polyline, List<ThSprinklerNetGroup>> sprinklerParameterList { get; set; } = new Dictionary<Polyline, List<ThSprinklerNetGroup>>();

        private double boundary { get; set; }
        public void PipelineArrange(List<Polyline> rooms, List<Polyline> geometry, double bound)
        {
            boundary = bound;
            //shearwall.ForEach(o=>shearWalls.Add(o));
            smallArea = rooms.Where(r => r.Area < boundary).ToList();
            bigArea = rooms.Where(r => r.Area > boundary).ToHashSet();
            //var spatialShearwallIndex = new ThCADCoreNTSSpatialIndex(shearWalls);
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
            //foreach (var room in sprinklerInRoom)
            //{
            //    var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(room.Value, out var dtSeg);
            //    var roomBoundary = room.Key;
            //    for (int i = 0; i < dtOrthogonalSeg.Count; ++i)
            //    {
            //        var l = dtOrthogonalSeg[i];
            //        //spatialShearwallIndex.Intersects(l.Buffer(1)) ||
            //        if ( roomBoundary.Intersects(l))
            //        {
            //            dtOrthogonalSeg.RemoveAt(i);
            //            --i;
            //        }
            //    }
            //    if (dtOrthogonalSeg.Count > 0)
            //    {
            //        orthogonalGrid.Add(dtOrthogonalSeg);
            //    }
            //    if (dtSeg.Count > 0)
            //    {
            //        gridLines.Add(dtSeg);
            //    }
            //}

            targetRoom = sprinklerInRoom.Keys.ToList();
            foreach(var room in targetRoom)
            {
                var sprinklerParameter = new ThSprinklerParameter();
                sprinklerParameter.SprinklerPt = sprinklerInRoom[room];
                var netList = ThSprinklerPtNetworkEngine.GetSprinklerPtNetwork(sprinklerParameter, geometry, out double dtTol);
                sprinklerParameterList.Add(room, netList);
                //List<Line> lines = new List<Line>();
                //netList.ForEach(o => lines.AddRange(o.lines));
                //var tree = kruskal(lines, sprinklerInRoom[room]);
                List<List<Line>> forest = new List<List<Line>>();
                netList.ForEach(o => forest.Add(kruskal(o.lines, sprinklerInRoom[room])));


            }

        }

        private List<Line> kruskal(List<Line> group, List<Point3d> sprinkler)
        {
            List<Line> result = new List<Line>();
            HashSet<Point3d> ptSet = new HashSet<Point3d>();
            Dictionary<Point3d, Point3d> father = new Dictionary<Point3d, Point3d>();
            group.Sort((p1, p2) =>  p1.Length.CompareTo(p2.Length));
            foreach(var pt in sprinkler)
            {
                father[pt] = pt;
            }
            foreach(var l in group)
            {
                var pt1 = l.StartPoint;
                var pt2 = l.EndPoint;
                var fatherPt1 = findFather(father,pt1);
                var fatherPt2 = findFather(father,pt2);
                if(fatherPt1 != fatherPt2)
                {
                    result.Add(l);
                    father[pt1] = fatherPt2;
                }
            }
           
            return result;
        }

        private Point3d findFather(Dictionary<Point3d, Point3d> father, Point3d pt)
        {
            while(father[pt]!= pt)
            {
                pt = father[pt];
            }
            return pt;
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
                roomOrder.Add(temp);
                presentLayer.ForEach(o => presentLayer.Remove(o));
            }
        }

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