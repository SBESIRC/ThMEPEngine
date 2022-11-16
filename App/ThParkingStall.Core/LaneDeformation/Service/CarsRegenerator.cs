using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using ThParkingStall.Core.MPartitionLayout;

namespace ThParkingStall.Core.LaneDeformation
{
    class CarsRegenerator
    {
        private readonly List<BlockNode> Nodes;
        private readonly PassDirection PassDir;
        public CarsRegenerator(List<BlockNode> nodes, PassDirection passDir)
        {
            Nodes = nodes;
            PassDir = passDir;
        }
        public void Pipeline()
        {
            List<LaneBlock> potentials = GetPotentialLanes();
            List<List<Polygon>> regions = new List<List<Polygon>>();

            ProcessedData.output1.Clear();
            ProcessedData.output2.Clear();
            ProcessedData.output3.Clear();

            foreach (var lane in potentials)
            {
                List<Polygon> polygons = new List<Polygon>();
                LineSegment line = null;
                List<LineSegment> upLines = new List<LineSegment>();
                GetParkingPlace(lane, ref polygons, ref line, ref upLines);
                regions.Add(polygons);

                ProcessedData.output1.Add(polygons);
                ProcessedData.output2.Add(line);
                ProcessedData.output3.Add(upLines);
            }

            // Draw
            LDOutput.DrawTmpOutPut0.RearrangeRegions.Clear();
            foreach (var list in regions)
                LDOutput.DrawTmpOutPut0.RearrangeRegions.AddRange(list);
        }
        private List<LaneBlock> GetPotentialLanes()
        {
            var res = new List<LaneBlock>();
            foreach (var node in Nodes)
            {
                if (node is LaneBlock laneBlock && laneBlock.IsHorizontal && !laneBlock.IsFtherLane[(int)PassDir])
                {
                    if (laneBlock.Lane.ParkingPlaceBlockList.Count == 1)
                    {
                        var parkObb = laneBlock.Lane.ParkingPlaceBlockList[0].ParkingPlaceBlockObb;
                        double left = parkObb.Coordinates[0].X;
                        double right = parkObb.Coordinates[0].X;
                        for (int i = 1; i < parkObb.Coordinates.Count(); i++)
                        {
                            left = Math.Min(left, parkObb.Coordinates[i].X);
                            right = Math.Max(right, parkObb.Coordinates[i].X);
                        }
                        continue;
                    }
                    bool isPotential = false;
                    foreach (var park in laneBlock.Lane.ParkingPlaceBlockList)
                    {
                        foreach (var spot in park.Cars)
                        {
                            if (spot.Type != 2)
                            {
                                isPotential = true;
                                break;
                            }
                        }
                        if (isPotential)
                            break;
                    }
                    if (isPotential)
                        res.Add(laneBlock);
                }
            }
            return res;
        }
        private void GetParkingPlace(LaneBlock laneBlock, ref List<Polygon> polygons, ref LineSegment line, ref List<LineSegment> upLines)
        {
            var children = new List<Geometry>();
            var q = new Queue<BlockNode>();
            foreach (var ch in laneBlock.NextNodes(PassDir))
            {
                q.Enqueue(ch);
            }
            while (q.Count > 0)
            {
                BlockNode node = q.Dequeue();
                if (node is LaneBlock || node is ParkBlock)
                    continue;
                if (node is SpotBlock spotBlock)
                {
                    if (spotBlock.Spot.FatherVehicleLane != laneBlock.Lane)
                        continue;
                    if (spotBlock.Spot.Type == 2)
                        continue;
                }

                children.AddRange(PolygonUtils.GetBufferedPolygons(node.Obb, 50));

                foreach (var ch in node.NextNodes(PassDir))
                    q.Enqueue(ch);
            }
            var unionResult = OverlayNGRobust.Union(children);

            if (unionResult is Polygon a)
            {
                var slim = PolygonUtils.GetBufferedPolygons(a, -50);
                foreach (var p in slim)
                {
                    if (IsValidRegion(p))
                    {
                        polygons.Add(p);
                    }
                }
            }
            else if (unionResult is GeometryCollection collection)
            {
                foreach (var geo in collection.Geometries)
                {
                    if (geo is Polygon pl)
                    {
                        var slim = PolygonUtils.GetBufferedPolygons(pl, -50);
                        foreach (var p in slim)
                        {
                            if (IsValidRegion(p))
                            {
                                polygons.Add(p);
                            }
                        }
                    }
                }
            }

            line = new LineSegment(laneBlock.LeftDownPoint, new Coordinate(laneBlock.RightUpPoint.X, laneBlock.LeftDownPoint.Y));

            for (int i = 0; i < laneBlock.ToleranceTable.Count - 1; i++)
            {
                var left = laneBlock.ToleranceTable[i].Coord;
                var right = laneBlock.ToleranceTable[i + 1].Coord;
                var y = laneBlock.LeftDownPoint.Y + laneBlock.ToleranceTable[i].ValueRight;
                upLines.Add(new LineSegment(new Coordinate(left, y), new Coordinate(right, y)));
            }
        }
        private bool IsValidRegion(Polygon p)
        {
            var LeftDownPoint = p.Coordinates[0].Copy();
            var RightUpPoint = p.Coordinates[0].Copy();
            for (int i = 1; i < p.Coordinates.Count(); i++)
            {
                Coordinate coord = p.Coordinates[i];
                if (coord.X < LeftDownPoint.X)
                    LeftDownPoint.X = coord.X;
                if (coord.Y < LeftDownPoint.Y)
                    LeftDownPoint.Y = coord.Y;
                if (coord.X > RightUpPoint.X)
                    RightUpPoint.X = coord.X;
                if (coord.Y > RightUpPoint.Y)
                    RightUpPoint.Y = coord.Y;
            }
            if (RightUpPoint.X - LeftDownPoint.X >= Parameter.SingleParkingPlaceWidth &&
                RightUpPoint.Y - LeftDownPoint.Y >= Parameter.SingleParkingPlaceWidth)
            {
                return true;
            }
            return false;
        }
    }
}
