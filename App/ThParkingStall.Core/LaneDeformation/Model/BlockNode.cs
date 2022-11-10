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
        public Vector2D Dir;
        public Polygon Obb;
        public double SelfTolerance;
        public bool IsAnchor;
        public Coordinate LeftDownPoint;
        public Coordinate RightUpPoint;
        public List<List<BlockNode>> NeighborNodes;
        protected List<double> MoveTolerances;
        protected List<int> InDegrees;
        public BlockNode()  //父类无参构造
        {

        }
        public BlockNode(Vector2D dir, Polygon obb, Coordinate leftDown, Coordinate rightUp, double selfTolerance = 0)  //此处freeLength设置了默认值
        {
            Dir = dir;
            Obb = obb;
            LeftDownPoint = leftDown;
            RightUpPoint = rightUp;
            SelfTolerance = selfTolerance;
            IsAnchor = false;
            InitBase();
            InitCoordinates();
        }
        protected void InitBase()
        {
            NeighborNodes = new List<List<BlockNode>>();
            NeighborNodes.Add(new List<BlockNode>());
            NeighborNodes.Add(new List<BlockNode>());
            MoveTolerances = new List<double> { 0, 0 };
            InDegrees = new List<int> { 0, 0 };
        }
        protected virtual void InitCoordinates()
        {
            // TODO：根据方向进行旋转
            this.LeftDownPoint = Obb.Coordinates[0].Copy();
            this.RightUpPoint = Obb.Coordinates[0].Copy();
            for (int i = 1; i < Obb.Coordinates.Count(); i++)
            {
                Coordinate coord = Obb.Coordinates[i];
                if (coord.X < LeftDownPoint.X)
                    LeftDownPoint.X = coord.X;
                if (coord.Y < LeftDownPoint.Y)
                    LeftDownPoint.Y = coord.Y;
                if (coord.X > RightUpPoint.X)
                    RightUpPoint.X = coord.X;
                if (coord.Y > RightUpPoint.Y)
                    RightUpPoint.Y = coord.Y;
            }
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
        public virtual double ToleranceForChild(PassDirection dir, double left, double right)
        {
            if (left > RightUpPoint.X || right < LeftDownPoint.X)
                return -1;
            return Tolerance(dir);
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
        public static double MinToler(double t1, double t2)
        {
            if (t1 < 0)
                return t2;
            if (t2 < 0)
                return t1;
            return Math.Min(t1, t2);
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
            this.SelfTolerance = 0;
            this.IsAnchor = false;
            InitCoordinates();
            InitBase();
        }
    }
    public class ParkBlock : BlockNode
    {
        public ParkingPlaceBlock Park;
        public ParkBlock(ParkingPlaceBlock park, Vector2D dir, bool isAnchor)
        {
            Park = park;
            this.Dir = dir;
            this.IsAnchor = isAnchor;
            this.Obb = park.ParkingPlaceBlockObb;
            UpdateBase();
        }
        private void UpdateBase()
        {
            this.SelfTolerance = 0;
            InitCoordinates();
            InitBase();
        }
    }
    public class BreakableBlock : BlockNode
    {
        public BreakableBlock() 
        {

        }
        public class MarkPoint
        {
            public double ValueRight;
            public double Coord;
            public MarkPoint(double coord, double tolerance)
            {
                Coord = coord;
                ValueRight = tolerance;
            }
        }
        public List<MarkPoint> ToleranceTable = null;
        // 接口
        public void InitTolerances(PassDirection passDir, bool narrow = false)
        {
            InitTable(passDir, narrow);
            MergeTable();
        }
        public override double ToleranceForChild(PassDirection passDir, double left, double right)
        {
            left = Math.Max(left, LeftDownPoint.X);
            right = Math.Min(right, RightUpPoint.X);
            if (left >= right)
                return -1;

            if (ToleranceTable is null)
                return Tolerance(passDir);

            int cur = 0;
            while (cur < ToleranceTable.Count && ToleranceTable[cur].Coord <= left)
                cur++;
            double res = ToleranceTable[cur - 1].ValueRight;
            while (cur < ToleranceTable.Count && ToleranceTable[cur].Coord < right)
            {
                res = MinToler(res, ToleranceTable[cur].ValueRight);
                cur++;
            }

            return res;
        }
        private void InitTable(PassDirection passDir, bool narrow = false)
        {
            ToleranceTable = new List<MarkPoint>
            {
                new MarkPoint(LeftDownPoint.X, -1),
                new MarkPoint(RightUpPoint.X, 0)
            };
            if (LastNodes(passDir).Count == 0)
            {
                ToleranceTable[0].ValueRight = SelfTolerance;
                return;
            }
            var segList = new List<ValueTuple<double, double, double>>();
            foreach (var node in this.LastNodes(passDir))
            {
                if (node is BreakableBlock bb && bb.ToleranceTable != null)
                {
                    for (int i = 0; i < bb.ToleranceTable.Count - 1; i++)
                    {
                        segList.Add((bb.ToleranceTable[i].Coord, bb.ToleranceTable[i + 1].Coord, bb.ToleranceTable[i].ValueRight));
                    }
                }
                else
                {
                    segList.Add((node.LeftDownPoint.X, node.RightUpPoint.X, node.Tolerance(passDir)));
                }
            }
            foreach (var seg in segList)
            {
                var left = Math.Max(seg.Item1 - (narrow ? VehicleLane.VehicleLaneWidth : 0), LeftDownPoint.X);
                var right = Math.Min(seg.Item2 + (narrow ? VehicleLane.VehicleLaneWidth : 0), RightUpPoint.X);
                var value = seg.Item3 + SelfTolerance;
                if (left >= right) continue;

                int cur = 0;
                while (cur < ToleranceTable.Count && ToleranceTable[cur].Coord <= left)
                    cur++;
                int lm = cur - 1;
                while (cur < ToleranceTable.Count && ToleranceTable[cur].Coord < right)
                    cur++;
                int rm = cur;

                var leftMark = new MarkPoint(left, MinToler(value, ToleranceTable[lm].ValueRight));
                var rightMark = new MarkPoint(right, ToleranceTable[rm - 1].ValueRight);

                for (int i = lm + 1; i < rm; i++)
                    ToleranceTable[i].ValueRight = MinToler(value, ToleranceTable[i].ValueRight);

                if (right < ToleranceTable[rm].Coord)
                    ToleranceTable.Insert(rm, rightMark);
                if (left == ToleranceTable[lm].Coord)
                    ToleranceTable[lm] = leftMark;
                else
                    ToleranceTable.Insert(lm + 1, leftMark);
            }
        }
        private void MergeTable()
        {
            var deleteList = new List<int>();
            // 去掉值为-1的区域
            // ** 连续-1情况未考虑，若之前的逻辑正确应该不会出现
            if (ToleranceTable[0].ValueRight < 0)
                ToleranceTable[0].ValueRight = ToleranceTable[1].ValueRight;
            for (int i = 1; i < ToleranceTable.Count - 1; i++)
            {
                if (ToleranceTable[i].ValueRight < 0)
                    ToleranceTable[i].ValueRight = Math.Max(ToleranceTable[i - 1].ValueRight, ToleranceTable[i + 1].ValueRight);
            }
            // 合并值相同的区域
            for (int i = 1; i < ToleranceTable.Count - 1; i++)
            {
                if (ToleranceTable[i].ValueRight == ToleranceTable[i - 1].ValueRight)
                    deleteList.Add(i);
            }
            for (int i = deleteList.Count - 1; i >= 0; i--)
                ToleranceTable.RemoveAt(deleteList[i]);
        }
    }
    public class LaneBlock : BreakableBlock
    {
        public VehicleLane Lane;

        public bool IsVerticle;
        public bool IsHorizontal;
        public List<bool> IsFtherLane = new List<bool> { false, false };

        public LaneBlock(VehicleLane lane, Vector2D dir)
        {
            Lane = lane;
            this.Dir = dir;
            this.Obb = lane.LaneObb;
            var laneVec = new Vector2D(Lane.CenterLine.P0, Lane.CenterLine.P1);
            var cos = Math.Abs(Dir.Normalize().Dot(laneVec.Normalize()));
            double eps = 1e-4;
            IsVerticle = cos > (1 - eps);
            IsHorizontal = cos < eps;
            UpdateBase();
        }
        public bool isOblique()
        {
            return !IsVerticle && !IsHorizontal; 
        }
        private void UpdateBase()
        {
            this.SelfTolerance = 0;
            this.IsAnchor = Lane.IsAnchorLane || this.isOblique();
            InitCoordinates();
            InitBase();
        }
    }
    public class FreeBlock : BreakableBlock
    {
        public FreeAreaRec Area;
        public FreeBlock(FreeAreaRec area, Vector2D dir)
        {
            Area = area;
            this.Dir = dir;
            this.Obb = area.Obb;
            UpdateBase();
        }
        private void UpdateBase()
        {
            this.SelfTolerance = Area.FreeLength;
            InitCoordinates();
            InitBase();
        }
        protected override void InitCoordinates()
        {
            // TODO：根据方向进行旋转
            this.LeftDownPoint = Area.LeftDownPoint;
            this.RightUpPoint = Area.RightUpPoint;
        }
    }
    public enum PassDirection : int
    {
        FORWARD = 0,
        BACKWARD = 1,
    }
}
