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
        public BlockType Type;
        public Vector2D Dir;
        public Polygon Obb;
        public double SelfTolerance;
        public Coordinate LeftDownPoint;
        public Coordinate RightUpPoint;
        public List<List<BlockNode>> NeighborNodes;
        protected List<double> MoveTolerances;
        protected List<int> InDegrees;
        public BlockNode()  //父类无参构造
        {

        }
        public BlockNode(BlockType type, Vector2D dir, Polygon obb, Coordinate leftDown, Coordinate rightUp, double selfTolerance = 0)  //此处freeLength设置了默认值
        {
            Type = type;
            Dir = dir;
            Obb = obb;
            LeftDownPoint = leftDown;
            RightUpPoint = rightUp;
            SelfTolerance = selfTolerance;
            NeighborNodes = new List<List<BlockNode>>();
            NeighborNodes.Add(new List<BlockNode>());
            NeighborNodes.Add(new List<BlockNode>());
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
        public void InDegreeIncre(PassDirection dir)
        {
            InDegrees[(int)dir]++;
        }
        public void InDegreeDecre(PassDirection dir)
        {
            InDegrees[(int)dir]--;
        }
        public void SetInDegree(PassDirection dir, int degree)
        {
            InDegrees[(int)dir] = degree;
        }
    }
    public class SpotBlock : BlockNode
    {
        public SingleParkingPlace Spot;
        public SpotBlock(SingleParkingPlace spot, Vector2D dir)
        {
            Spot = spot;
            this.Dir = dir;
            this.Obb = spot.ParkingPlaceObb;
            UpdateBase();
        }
        private void UpdateBase()
        {
            InitCoordinates();
            this.Type = BlockType.SPOT;
            this.SelfTolerance = 0;
            this.NeighborNodes = new List<List<BlockNode>>();
            NeighborNodes.Add(new List<BlockNode>());
            NeighborNodes.Add(new List<BlockNode>());
            this.MoveTolerances = new List<double> { 0, 0 };
            this.InDegrees = new List<int> { 0, 0 };
        }
        private void InitCoordinates()
        {
            // TODO：根据方向进行旋转
            this.LeftDownPoint = this.RightUpPoint = Spot.ParkingPlaceObb.Coordinates[0];
            for (int i = 1; i < Spot.ParkingPlaceObb.Coordinates.Count(); i++)
            {
                Coordinate coord = Spot.ParkingPlaceObb.Coordinates[i];
                if (coord.CompareTo(LeftDownPoint) is -1)
                    LeftDownPoint = coord;
                if (coord.CompareTo(RightUpPoint) is 1)
                    RightUpPoint = coord;
            }
        }
    }
    public class LaneBlock : BlockNode
    {
        public VehicleLane Lane;
        public bool IsVerticle;
        public LaneBlock(VehicleLane lane, Vector2D dir)
        {
            Lane = lane;
            this.Dir = dir;
            this.Obb = lane.LaneObb;
            IsVerticle = Dir.Dot(new Vector2D(Lane.CenterLine.P0, Lane.CenterLine.P1)).Equals(0);
            UpdateBase();
        }
        private void UpdateBase()
        {
            InitCoordinates();
            this.Type = BlockType.LANE;
            this.SelfTolerance = 0;
            this.NeighborNodes = new List<List<BlockNode>>();
            NeighborNodes.Add(new List<BlockNode>());
            NeighborNodes.Add(new List<BlockNode>());
            this.MoveTolerances = new List<double> { 0, 0 };
            this.InDegrees = new List<int> { 0, 0 };
        }
        private void InitCoordinates()
        {
            // TODO：根据方向进行旋转
            this.LeftDownPoint = this.RightUpPoint = Lane.LaneObb.Coordinates[0];
            for (int i = 1; i < Lane.LaneObb.Coordinates.Count(); i++)
            {
                Coordinate coord = Lane.LaneObb.Coordinates[i];
                if (coord.CompareTo(LeftDownPoint) is -1)
                    LeftDownPoint = coord;
                if (coord.CompareTo(RightUpPoint) is 1)
                    RightUpPoint = coord;
            }
        }
    }
    public class FreeBlock : BlockNode
    {
        public FreeAreaRec Area;
        public bool IsVerticle;
        public FreeBlock(FreeAreaRec area, Vector2D dir)
        {
            Area = area;
            this.Dir = dir;
            this.Obb = area.Obb;
            UpdateBase();
        }
        private void UpdateBase()
        {
            InitCoordinates();
            this.Type = BlockType.FREE;
            this.SelfTolerance = Area.FreeLength;
            this.NeighborNodes = new List<List<BlockNode>>();
            NeighborNodes.Add(new List<BlockNode>());
            NeighborNodes.Add(new List<BlockNode>());
            this.MoveTolerances = new List<double> { 0, 0 };
            this.InDegrees = new List<int> { 0, 0 };
        }
        private void InitCoordinates()
        {
            // TODO：根据方向进行旋转
            this.LeftDownPoint = Area.LeftDownPoint;
            this.RightUpPoint = Area.RightUpPoint;
        }
    }
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
