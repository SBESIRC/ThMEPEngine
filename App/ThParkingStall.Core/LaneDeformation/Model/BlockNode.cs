using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

namespace ThParkingStall.Core.LaneDeformation
{

    public class BlockNode
    {

        public BlockNode()  //父类无参构造
        {

        }
        public BlockNode(BlockType type, Vector2D dir, Polygon obb, double freeLength = 0)  //此处freeLength设置了默认值
        {
            Type = type;
            Dir = dir;
            Obb = obb;
            SelfTolerance = freeLength;
            NeighborNodes = new List<List<BlockNode>>(2);
            MoveTolerances = new List<double> { 0, 0 };
            InDegrees = new List<int> { 0, 0 };
        }
        public List<BlockNode> NextNodes(PassDirection dir)
        {
            return NeighborNodes[(int)dir];
        }
        public List<BlockNode> LastNodes(PassDirection dir)
        {
            return NeighborNodes[1 - (int)dir];
        }
        public double Tolerance(PassDirection dir)
        {
            return MoveTolerances[(int)dir] + SelfTolerance;
        }
        public double MoveTolerance(PassDirection dir)
        {
            return MoveTolerances[(int)dir];
        }
        public void SetMoveTolerance(PassDirection dir, double tolerance)
        {
            MoveTolerances[(int)dir] = tolerance;
        }
        public int InDegree(PassDirection dir)
        {
            return InDegrees[(int)dir];
        }
        public void SetInDegree(PassDirection dir, int degree)
        {
            InDegrees[(int)dir] = degree;
        }
        public BlockType Type;
        public Vector2D Dir;
        public Polygon Obb;
        public double SelfTolerance;
        private List<List<BlockNode>> NeighborNodes;
        private List<double> MoveTolerances;
        private List<int> InDegrees;
    }
/*
    class FreeBlock : BlockNode
    {
    }
    class SpotBlock : BlockNode
    {
    }
    class LaneBlock : BlockNode
    {
    }
*/
    public enum BlockType : int
    {
        FREE = 0,
        LANE = 1,
        SPOT = 2,
    }
    public enum PassDirection : int
    {
        FORWARD = 0,
        BACKWARD = 1,
    }
}
