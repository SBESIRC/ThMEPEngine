using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace ThParkingStall.Core.LaneDeformation
{

    class ToleranceCalculator
    {
        private List<BlockNode> Nodes;
        private Queue<BlockNode> WaitQueue = new Queue<BlockNode>();
        private PassDirection Dir;
        public ToleranceCalculator(List<BlockNode> nodes)
        {
            Nodes = nodes;
        }
        void Preprocess()
        {
            WaitQueue.Clear();
            // 入度为0则入队
            foreach (BlockNode n in Nodes)
            {
                int degree = n.LastNodes(Dir).Count;
                n.SetInDegree(Dir, degree);
                if (degree is 0)
                    WaitQueue.Enqueue(n);
            }
        }
        public void Run(PassDirection dir)
        {
            Dir = dir;
            Preprocess();
            while (WaitQueue.Count > 0)
            {
                BlockNode node = WaitQueue.Dequeue();
                if (node is FreeBlock free)
                {
                    free.InitTolerances(Dir);
                }
                else if (node is SpotBlock spot)
                {
                    GetMinTolerance(spot);
                }
                else if (node is ParkBlock park)
                {
                    if (park.IsAnchor)
                        park.SetMoveTolerance(Dir, 0);
                    else
                        GetMinTolerance(park);
                }
                else if (node is LaneBlock lane)
                {
                    // 不可动车道
                    if (lane.IsAnchor)
                    {
                        lane.SetMoveTolerance(Dir, 0);
                    }
                    // 横向车道
                    else if (lane.IsHorizontal)
                    {
                        lane.InitTolerances(Dir, lane.IsFtherLane[(int)Dir]);
                    }
                    // 其余车道
                    else
                    {
                        GetMinTolerance(lane);
                    }
                }

                // 更新后继的入度,为0则入队
                UpdateNextNodes(node);
            }
        }
        private void UpdateNextNodes(BlockNode node)
        {
            foreach (BlockNode n in node.NextNodes(Dir))
            {
                n.InDegreeDecre(Dir);
                if (n.InDegree(Dir) is 0)
                    WaitQueue.Enqueue(n);
            }
        }
        private void GetMinTolerance(BlockNode node)
        {
            if (node.LastNodes(Dir).Count is 0)
                node.SetMoveTolerance(Dir, 0);
            else
            {
                double min = double.PositiveInfinity;
                foreach (BlockNode n in node.LastNodes(Dir))
                {
/*                    if (n.Type is BlockType.LANE &&
                        ((LaneBlock) n).)*/
                    min = BlockNode.MinToler(n.ToleranceForChild(Dir, node.LeftDownPoint.X, node.RightUpPoint.X), min);
                }
                node.SetMoveTolerance(Dir, min);
            }
        }
        public void Pipeline()
        {
            Run(PassDirection.FORWARD);
            //Run(PassDirection.BACKWARD);

            // Draw
            var pointsToDraw = new List<Point>();
            var valuesToDraw = new List<double>();
            foreach (var node in Nodes)
            {
                if (node is BreakableBlock bb && bb.ToleranceTable != null)
                {
                    for (int i = 0; i < bb.ToleranceTable.Count - 1; i++)
                    {
                        Point p = new Point(bb.ToleranceTable[i].Coord, node.Obb.Centroid.Y);
                        pointsToDraw.Add(p);
                        valuesToDraw.Add(bb.ToleranceTable[i].ValueRight);
                    }
                }
                else
                {
                    Point p = new Point(node.Obb.Centroid.X - 800, node.Obb.Centroid.Y);
                    pointsToDraw.Add(p);
                    valuesToDraw.Add(node.Tolerance(PassDirection.FORWARD));
                }
            }
            LDOutput.DrawTmpOutPut0.TolerancePositions = pointsToDraw;
            LDOutput.DrawTmpOutPut0.ToleranceResults = valuesToDraw;
        }

    }
}
