using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;

namespace ThParkingStall.Core.LaneDeformation
{

    class ToleranceCalculator
    {
        public ToleranceCalculator(List<BlockNode> nodes)
        {
            Nodes = nodes;
        }

        void Preprocess(PassDirection dir)
        {
            WaitQueue.Clear();
            // 入度为0则入队
            foreach (BlockNode n in Nodes)
            {
                int degree = n.LastNodes(dir).Count;
                n.SetInDegree(dir, degree);
                if (degree is 0)
                    WaitQueue.Enqueue(n);
            }
        }

        public void Run(PassDirection dir)
        {
            Preprocess(dir);
            while (WaitQueue.Count > 0)
            {
                BlockNode node = WaitQueue.Dequeue();
                if (node.Type is BlockType.FREE || node.Type is BlockType.SPOT)
                {
                    // 更新容差值
                    if (node.LastNodes(dir).Count is 0)
                        node.SetMoveTolerance(dir, 0);
                    else
                    {
                        double min = double.PositiveInfinity;
                        foreach (BlockNode n in node.LastNodes(dir))
                            min = Math.Min(n.Tolerance(dir), min);
                        node.SetMoveTolerance(dir, min);
                    }

                    // 更新后继的入度,为0则入队
                    foreach (BlockNode n in node.NextNodes(dir))
                    {
                        n.SetInDegree(dir, n.InDegree(dir) - 1);
                        if (n.InDegree(dir) is 0)
                            WaitQueue.Enqueue(n);
                    }
                }
                else if (node.Type is BlockType.LANE)
                {
                    LaneBlock laneBlock = (LaneBlock)node;

                    // 不可动车道
                    if (laneBlock.Lane.IsAnchorLane)
                    {
                        // 更新容差值
                        node.SetMoveTolerance(dir, 0);

                        // 更新后继的入度,为0则入队
                        foreach (BlockNode n in node.NextNodes(dir))
                        {
                            n.SetInDegree(dir, n.InDegree(dir) - 1);
                            if (n.InDegree(dir) is 0)
                                WaitQueue.Enqueue(n);
                        }
                    }
                    // 垂直车道
                    else if (laneBlock.IsVerticle)
                    {
                        if (node.LastNodes(dir).Count is 0)
                            node.SetMoveTolerance(dir, 0);
                        else
                        {
                            List<Coordinate> coords = new List<Coordinate>();
                            List<double> values;
                            foreach (BlockNode n in node.LastNodes(dir))
                            {
                                coords.Add(n.LeftDownPoint);
                            }
                        }
                    }
                    // 其余车道
                    else
                    {

                    }
                }
            }
        }

        public void Pipeline()
        {
            Run(PassDirection.FORWARD);
            Run(PassDirection.BACKWARD);
        }

        private List<BlockNode> Nodes;
        private Queue<BlockNode> WaitQueue;
    }
}
