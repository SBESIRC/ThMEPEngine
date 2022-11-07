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
                if (node.Type is BlockType.FREE || node.Type is BlockType.SPOT)
                {
                    // 更新容差值
                    GetMinTolerance(node);

                    // 更新后继的入度,为0则入队
                    UpdateNextNodes(node);
                }
                else if (node.Type is BlockType.LANE)
                {
                    LaneBlock laneBlock = (LaneBlock)node;

                    // 不可动车道
                    if (laneBlock.Lane.IsAnchorLane)
                    {
                        // 更新容差值
                        node.SetMoveTolerance(Dir, 0);

                        // 更新后继的入度,为0则入队
                        UpdateNextNodes(node);
                    }
                    // 垂直车道
                    else if (laneBlock.IsVerticle)
                    {
                        GetMinTolerance(node);
                        UpdateNextNodes(node);
                        /*if (node.LastNodes(Dir).Count is 0)
                            node.SetMoveTolerance(Dir, 0);
                        else
                        {
                            List<Coordinate> coords = new List<Coordinate>();
                            List<double> values;
                            foreach (BlockNode n in node.LastNodes(Dir))
                            {
                                coords.Add(n.LeftDownPoint);
                            }
                        }*/
                    }
                    // 其余车道
                    else
                    {
                        GetMinTolerance(node);
                        UpdateNextNodes(node);
                    }
                }
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
                    min = Math.Min(n.Tolerance(Dir), min);
                node.SetMoveTolerance(Dir, min);
            }
        }
        public void Pipeline()
        {
            Run(PassDirection.FORWARD);
            Run(PassDirection.BACKWARD);
        }

    }
}
