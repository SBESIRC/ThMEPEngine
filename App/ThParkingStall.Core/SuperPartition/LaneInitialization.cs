using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.SuperPartition
{
    public class LaneInitialization
    {
        public LaneInitialization(List<LineSegment> lanes, Polygon boundary)
        {
            Lanes = lanes;
            Boundary = boundary;
        }
        private List<LineSegment> Lanes { get; set; }
        private Polygon Boundary { get; set; }
        public List<SLane> InitLanes()
        {
            JoinFragmentaryLanes();
            var sLanes = ConstructLanes();
            return sLanes;
        }
        /// <summary>
        /// 输入的车道线有可能有碎线，合并能合并的
        /// </summary>
        void JoinFragmentaryLanes()
        {
            int count = 0;
            while (true)
            {
                count++;
                if (count > 10) break;
                if (Lanes.Count < 2) break;
                for (int i = 0; i < Lanes.Count - 1; i++)
                {
                    var joined = false;
                    for (int j = i + 1; j < Lanes.Count; j++)
                    {
                        if (IsParallelLine(Lanes[i], Lanes[j]) && IsConnectedLine(Lanes[i], Lanes[j]))
                        {
                            var pl = JoinCurves(new List<LineString>(), new List<LineSegment>() { Lanes[i], Lanes[j] }).First();
                            var line = new LineSegment(pl.StartPoint.Coordinate, pl.EndPoint.Coordinate);
                            Lanes.RemoveAt(j);
                            Lanes.RemoveAt(i);
                            Lanes.Add(line);
                            joined = true;
                            break;
                        }
                    }
                    if (joined) break;
                }
            }
        }
        List<SLane> ConstructLanes()
        {
            var sLanes = new List<SLane>();
            foreach (var line in Lanes)
            {
                var vec = Vector(line).GetPerpendicularVector().Normalize();
                var pt_a = line.MidPoint.Translation(vec);
                var pt_b = line.MidPoint.Translation(-vec);
                if (Boundary.Contains(pt_a))
                    sLanes.Add(new SLane(line, vec));
                if (Boundary.Contains(pt_b))
                    sLanes.Add(new SLane(line, -vec));
            }
            return sLanes;
        }
    }
}
