using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace ThParkingStall.Core.LaneDeformation
{

    class CarsPorter
    {
        // input
        public List<LaneBlock> RearrangedLanes;
        public List<List<SpotBlock>> RearrangedSpots;
        public List<NewCarDataPass> NewCarDataPasses;
        // tmp
        private List<BlockNode> Nodes;
        private Queue<BlockNode> WaitQueue = new Queue<BlockNode>();
        private PassDirection Dir;
        public CarsPorter(List<BlockNode> nodes, PassDirection dir)
        {
            Dir = dir;
            Nodes = nodes;
            RearrangedLanes = ProcessedData.RearrangedLanes;
            RearrangedSpots = ProcessedData.RearrangedSpots;
            NewCarDataPasses = ProcessedData.NewCarDataPasses;
            Preprocess();
        }
        void Preprocess()
        {
            var usefulLanes = new List<LaneBlock>();
            var usefulData = new List<NewCarDataPass>();
            for (int i = 0; i < RearrangedLanes.Count; i++)
            {
                //if (NewCarDataPasses[i].NewCars.Count > RearrangedSpots[i].Count)
                if (i == 0 || i == 4)
                {
                    usefulLanes.Add(RearrangedLanes[i]);
                    usefulData.Add(NewCarDataPasses[i]);
                    RearrangedSpots[i].ForEach(s => s.IsDeleted = true);
                }
            }
            RearrangedLanes = usefulLanes;
            NewCarDataPasses = usefulData;

            WaitQueue.Clear();
            foreach (BlockNode n in Nodes)
            {
                int degree = n.LastNodes(Dir).Count;
                n.SetInDegree(Dir, degree);
                if (degree is 0)
                    WaitQueue.Enqueue(n);
            }
        }
        public void Pipeline()
        {
            for (int i = 0; i < RearrangedLanes.Count; i++)
            {
                List<BlockNode> newSpots = new List<BlockNode>();
                for (int j = 0; j < NewCarDataPasses[i].NewCars.Count; j++)
                {
                    var p = NewCarDataPasses[i].NewCars[j];
                    var sp = new SingleParkingPlace(p, 0, new PureVector(0, 0), p.Coordinate);
                    var newSpot = new SpotBlock(sp, Parameter.TestDirection);
                    newSpot.SetMovement(Dir, NewCarDataPasses[i].CarUpLineOccupy[j]);
                    newSpots.Add(newSpot);
                    LDOutput.DrawTmpOutPut0.ResultSpotsNew.Add(p);
                }
                RearrangedLanes[i].InitMovements(Dir, true, newSpots);
            }

            UpdateMovements();

            // Draw
            var pointsToDraw = new List<Point>();
            var valuesToDraw = new List<double>();
            foreach (var node in Nodes)
            {
                if (node is BreakableBlock bb && bb.MovementTable != null)
                {
                    for (int i = 0; i < bb.MovementTable.Count - 1; i++)
                    {
                        Point p = new Point(bb.MovementTable[i].Coord, node.Obb.Centroid.Y);
                        pointsToDraw.Add(p);
                        valuesToDraw.Add(bb.MovementTable[i].ValueRight);
                    }
                }
                else
                {
                    Point p = new Point(node.Obb.Centroid.X - 800, node.Obb.Centroid.Y);
                    pointsToDraw.Add(p);
                    valuesToDraw.Add(node.Movement(Dir));
                }
            }
            LDOutput.DrawTmpOutPut0.TolerancePositions = pointsToDraw;
            LDOutput.DrawTmpOutPut0.ToleranceResults = valuesToDraw;

            // Draw 
            foreach (var node in Nodes)
            {
                if (node is SpotBlock spot)
                {
                    if (spot.IsDeleted)
                        continue;
                    var movement = spot.Movement(Dir);
                    if (movement < 0.01)
                        LDOutput.DrawTmpOutPut0.ResultSpotsNew.Add(spot.Obb);
                    else
                    {
                        LDOutput.DrawTmpOutPut0.ResultSpotsNew.Add(
                            PolygonUtils.CreatePolygonRec(
                                spot.LeftDownPoint.X, spot.RightUpPoint.X,
                                spot.LeftDownPoint.Y + movement, spot.RightUpPoint.Y + movement));
                    }
                }
            }
        }
        public void UpdateMovements()
        {
            while (WaitQueue.Count > 0)
            {
                BlockNode node = WaitQueue.Dequeue();
                if (node is FreeBlock free)
                {
                    free.InitMovements(Dir);
                }
                else if (node is SpotBlock spot)
                {
                    GetMaxMovement(spot);
                }
                else if (node is ParkBlock park)
                {
                    if (park.IsAnchor)
                        park.SetMovement(Dir, 0);
                    else
                        GetMaxMovement(park);
                }
                else if (node is LaneBlock lane)
                {
                    // 不可动车道
                    if (lane.IsAnchor)
                    {
                        lane.SetMovement(Dir, 0);
                    }
                    // 横向车道
                    else if (lane.IsHorizontal)
                    {
                        lane.InitMovements(Dir, lane.IsFtherLane[(int)Dir]);
                    }
                    // 其余车道
                    else
                    {
                        GetMaxMovement(lane);
                    }
                }

                foreach (BlockNode n in node.NextNodes(Dir))
                {
                    n.InDegreeDecre(Dir);
                    if (n.InDegree(Dir) is 0)
                        WaitQueue.Enqueue(n);
                }
            }
        }
        private bool GetMaxMovement(BlockNode node)
        {
            if (node.LastNodes(Dir).Count is 0)
            {
                node.SetMovement(Dir, 0);
                return false;
            }
            else
            {
                double max = node.Movement(Dir);
                foreach (BlockNode n in node.LastNodes(Dir))
                {
                    max = BlockNode.MaxMove(n.MovementForChild(Dir, node.LeftDownPoint.X, node.RightUpPoint.X), max);
                }
                node.SetMovement(Dir, max);
                return max > 0.01;
            }
        }

    }
}
