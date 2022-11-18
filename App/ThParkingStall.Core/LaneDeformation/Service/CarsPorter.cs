using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace ThParkingStall.Core.LaneDeformation
{

    class CarsPorter
    {
        private List<BlockNode> Nodes;
        private Queue<BlockNode> WaitQueue = new Queue<BlockNode>();
        private PassDirection Dir;
        public CarsPorter(List<BlockNode> nodes)
        {
            Nodes = nodes;
        }
        void Preprocess()
        {
            WaitQueue.Clear();
            // 从移动车位开始寻找所有孩子
            // 被找到的孩子入度加1
            // 入度为0则入队
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
                    free.InitMovements(Dir);
                }
                else if (node is SpotBlock spot)
                {
                    GetMaxMovement(spot);
                    if (spot.Movement(dir) > 0)
                    {
                        // OutPut.MoveSpots.Add(spot);
                    }
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
        private void GetMaxMovement(BlockNode node)
        {
            if (node.LastNodes(Dir).Count is 0)
                node.SetMovement(Dir, 0);
            else
            {
                double max = 0;
                foreach (BlockNode n in node.LastNodes(Dir))
                {
                    /*                    if (n.Type is BlockType.LANE &&
                                            ((LaneBlock) n).)*/
                    max = BlockNode.MaxMove(n.MovementForChild(Dir, node.LeftDownPoint.X, node.RightUpPoint.X), max);
                }
                node.SetMovement(Dir, max);
            }
        }
        public void Pipeline()
        {
            Run(PassDirection.BACKWARD);
            //Run(PassDirection.BACKWARD);

            // Draw
/*            var pointsToDraw = new List<Point>();
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
            LDOutput.DrawTmpOutPut0.ToleranceResults = valuesToDraw;*/
        }

    }
}
