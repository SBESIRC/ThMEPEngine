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
                    // 取前继中的最小值
                    double min = double.PositiveInfinity;
                    foreach (BlockNode n in node.NextNodes(dir))
                    {
                        min = Math.Min(n.Tolerance(dir), min);
                    }

                    // 更新后继的入度
                    foreach (BlockNode n in node.LastNodes(dir))
                    {
                        n.SetInDegree(dir, n.InDegree(dir) - 1);
                    }

                    // 更新容差值
                    node.SetMoveTolerance(dir, min);
                }

                 else if (node.Type is BlockType.LANE)
                {

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
