using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using NFox.Cad;
using ThMEPWSS.SprinklerConnect.Service;

namespace THMEPWSS.RoomOrderingService
{
    public class OrderingService
    {
        public List<List<Polyline>> roomOrder { get; set; } = new List<List<Polyline>>();
        public List<Polyline> roomSet { get; set; } = new List<Polyline>();
        private List<Polyline> smallArea { get; set; } = new List<Polyline>();
 
        private HashSet<Polyline> bigArea { get; set; } = new HashSet<Polyline>();
        public List<Point3d> sprinkler { get; set; } = new List<Point3d>();
        private DBObjectCollection shearWalls { get; set; } = new DBObjectCollection();
        private Dictionary<Polyline, List<Polyline>> nextInlayerPoly { get; } = new Dictionary<Polyline, List<Polyline>>();
        private Dictionary<Polyline, List<Polyline>> nextOuterLayerPoly { get; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Point3d>> sprinklerInRoom { get; set; } = new Dictionary<Polyline, List<Point3d>>();
        public List<List<Line>> gridLines { get; set; } = new List<List<Line>>();
        public List<List<Line>> orthogonalGrid { get; set; } = new List<List<Line>>();

        private double boundary { get; set; }
        public void PipelineArrange(List<Polyline> rooms, List<Polyline> shearwall, double bound)
        {
            boundary = bound;
            shearwall.ForEach(o=>shearWalls.Add(o));
            smallArea = rooms.Where(r => r.Area < boundary).ToList();
            bigArea = rooms.Where(r => r.Area > boundary).ToHashSet();
            var spatialShearwallIndex = new ThCADCoreNTSSpatialIndex(shearWalls);
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
            foreach (var room in sprinklerInRoom)
            {
                var dtOrthogonalSeg = ThSprinklerNetworkService.FindOrthogonalAngleFromDT(room.Value, out var dtSeg);
                var roomBoundary = room.Key;
                for (int i = 0; i < dtOrthogonalSeg.Count; ++i)
                {
                    var l = dtOrthogonalSeg[i];
                    //spatialShearwallIndex.Intersects(l.Buffer(1)) ||
                    if ( roomBoundary.Intersects(l))
                    {
                        dtOrthogonalSeg.RemoveAt(i);
                        --i;
                    }
                }
                if (dtOrthogonalSeg.Count > 0)
                {
                    orthogonalGrid.Add(dtOrthogonalSeg);
                }
                if (dtSeg.Count > 0)
                {
                    gridLines.Add(dtSeg);
                }
            }

        }

        private List<Line> kruskal(List<Line> group, List<Point3d> sprinkler)
        {
            HashSet<Point3d> ptSet = new HashSet<Point3d>();
            List<Line> result = new List<Line>();
            while (sprinkler.Count != ptSet.Count)
            {
                group.Sort();
                var pt1 = group[0].StartPoint;
                var pt2 = group[0].EndPoint;
                if(ptSet.Contains(pt1) && !ptSet.Contains(pt2))
                {
                    ptSet.Add(pt2);
                    result.Add(group[0]);
                }else if(!ptSet.Contains(pt1) && ptSet.Contains(pt2))
                {
                    ptSet.Add(pt1);
                    result.Add(group[0]);
                }else if(!ptSet.Contains(pt1) && !ptSet.Contains(pt2))
                {
                    ptSet.Add(pt1);
                    ptSet.Add(pt2);
                    result.Add(group[0]);
                }
                group.RemoveAt(0);

            }
            return result;
        }

        private void FindCorrespondingRoom(Polyline roomA , List<Polyline> roomSetB)
        {
            ThCADCoreNTSSpatialIndex spatialRoomIndex = new ThCADCoreNTSSpatialIndex(roomSetB.ToCollection());
            DBObjectCollection crossingRoom = spatialRoomIndex.SelectCrossingPolygon(roomA);
            foreach(Polyline room in crossingRoom)
            {
                var intersect1 = ThCADCoreNTSEntityExtension.Intersection(room, roomA);
                foreach(Entity interPart in intersect1)
                {
                    var difference = ThCADCoreNTSEntityExtension.Difference(interPart, shearWalls);
                }
            }
        }


    }



}