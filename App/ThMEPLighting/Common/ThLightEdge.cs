using System;
using System.Linq;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage;

namespace ThMEPLighting.Common
{
    public class ThLightEdge
    {
        public string Id { get; set; }
        public Line Edge { get; set; }
        public List<ThLightNode> LightNodes { get; set; }
        public List<Tuple<Point3d, ThLightEdge>> MultiBranch { get; set; }
        public bool IsTraversed { get; set; } 
        /// <summary>
        /// 遍历的前进方向
        /// </summary>
        public Vector3d Direction { get; set; }
        public bool IsDX { get; set; }
        public EdgePattern EdgePattern { get; set; }
        public ThLightEdge()
        {
            IsDX = true;
            EdgePattern = EdgePattern.Unknown;
            LightNodes = new List<ThLightNode>();
            MultiBranch = new List<Tuple<Point3d, ThLightEdge>>();
        }
        public ThLightEdge(Line line):this()
        {
            Edge = line;
            Id = Guid.NewGuid().ToString();
            Direction = line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }
        public void Update(Point3d portPt)
        {
            SetDirection(portPt);
            Sort();
        }
        private void SetDirection(Point3d portPt)
        {
            if(portPt.DistanceTo(Edge.StartPoint)< portPt.DistanceTo(Edge.EndPoint))
            {
                Direction = Edge.StartPoint.GetVectorTo(Edge.EndPoint);
            }
            else
            {
                Direction = Edge.EndPoint.GetVectorTo(Edge.StartPoint);
            }
        }
        private void Sort()
        {
            //灯上的点要根据遍历的方向保持一致
            LightNodes = LightNodes.OrderBy(o => Edge.StartPoint.DistanceTo(o.Position)).ToList();
            //当前边的向量与直线向量相反
            if(Direction.Negate().IsCodirectionalTo(Edge.StartPoint.GetVectorTo(Edge.EndPoint)))
            {
                LightNodes.Reverse();
            }
        }
        public ThLightNode FindPreLightNode(Point3d branchPt)
        {
            Point3d start = Edge.StartPoint;
            if (!Direction.IsCodirectionalTo(Edge.StartPoint.GetVectorTo(Edge.EndPoint)))
            {
                start = Edge.EndPoint;
            }
            Vector3d branchVec = start.GetVectorTo(branchPt);
            double branchDis=ThGeometryTool.ProjectionDis(Direction,branchVec);
            for (int i= LightNodes.Count-1; i>=0;i--)
            {
                Vector3d vec = start.GetVectorTo(LightNodes[i].Position);
                double length = ThGeometryTool.ProjectionDis(Direction, vec);
                if(branchDis> length)
                {
                    return LightNodes[i];
                }
            }
            return new ThLightNode();
        }
        public ThLightNode FindNextLightNode(Point3d branchPt)
        {
            Point3d start = Edge.StartPoint;
            if (!Direction.IsCodirectionalTo(Edge.StartPoint.GetVectorTo(Edge.EndPoint)))
            {
                start = Edge.EndPoint;
            }
            double dis = start.DistanceTo(branchPt);
            for (int i=0;i< LightNodes.Count;i++)
            {
                if(dis<start.DistanceTo(LightNodes[i].Position))
                {
                    return LightNodes[i];
                }
            }
            return new ThLightNode();
        }
        /// <summary>
        /// 获取当前边的起点和终点
        /// </summary>
        /// <returns></returns>
        public Tuple<Point3d,Point3d> GetDirectionPts()
        {
            var vec = Edge.StartPoint.GetVectorTo(Edge.EndPoint);
            Point3d startPt = Edge.StartPoint;
            Point3d endPt = Edge.EndPoint;
            if (vec.Negate().IsCodirectionalTo(Direction, new Tolerance(1.0, 1.0)))
            {
                startPt = Edge.EndPoint;
                endPt = Edge.StartPoint;
            }
            return Tuple.Create(startPt, endPt);
        }
        public Point3d StartPoint
        {
            get
            {
                return IsSameDirection ? Edge.StartPoint : Edge.EndPoint;
            }
        }

        public Point3d EndPoint
        {
            get
            {
                return IsSameDirection ? Edge.EndPoint : Edge.StartPoint;
            }
        }
        public ThLightNode GetRecentNode(Point3d pt)
        {
            var sorts = LightNodes.OrderBy(o => o.Position.DistanceTo(pt));
            return sorts.Count() > 0 ? sorts.First() : new ThLightNode();
        }
        /// <summary>
        /// 线的方向和边的遍历方向是否一致
        /// </summary>
        public bool IsSameDirection
        {
            get
            {
                var vec = Edge.StartPoint.GetVectorTo(Edge.EndPoint);
                return vec.IsSameDirection(Direction);
            }
        }
    }
    public enum EdgePattern
    {
        Unknown,
        First,
        Second,
        Center
    }
}
