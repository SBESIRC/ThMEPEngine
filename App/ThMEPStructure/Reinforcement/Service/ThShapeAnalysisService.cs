using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThShapeAnalysisService: ThAnalysisService
    {
        private ShapeCode shapeCode = ShapeCode.Unknown;
        public ShapeCode ShapeCode
        { 
            get => shapeCode; 
        }

        public override void Analysis(Polyline poly)
        {
            shapeCode = ShapeCode.Unknown;
            // 不支持弧
            if (!ThMEPFrameService.IsClosed(poly,1.0) || HasArc(poly))
            {
                shapeCode = ShapeCode.Unknown;
            }
            if(IsRectType(poly))
            {
                shapeCode = ShapeCode.Rect;
            }
            else if(IsLType(poly))
            {
                shapeCode = ShapeCode.L;
            }
            else if (IsTType(poly))
            {
                shapeCode = ShapeCode.T;
            }
        }

        private bool HasArc(Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var st = poly.GetSegmentType(i);
                if (st == SegmentType.Arc)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsLType(Polyline poly)
        {
            /*          L3
             *         -----
             *         |   | 
             *         |   |
             *         |   | L4
             *  (L2)   |   |     L5
             *         |   ------------
             *         |              | L6
             *         ----------------
             *             （L1）
             */
            // 识别条件：
            // 有6条边
            // 相邻边是互相垂直的
            // 有一个凹点
            // 有两组边满足：
            // L1//L3,L3//L5,L1=L3+L5, L3和L5不共线
            // L2//L4,L2//L6,L2=L4+L6, L4和L6不共线
            var lines = poly.ToLines();
            if(lines.Count != 6 || !IsAllAnglesVertical(lines))
            {
                return false;
            }
            var concavePoints = GetConcavePoints(poly);
            if(concavePoints.Count!=1)
            {
                return false;
            }
            var l1l2Edges = lines.FindLTypeEdge();
            return l1l2Edges.Count == 2;
        }

        private bool IsTType(Polyline poly)
        {
            /*                  (L5)
             *                 -----
             *                 |   | 
             *                 |   |
             *            (L4) |   | (L6)
             *          (L3)   |   |    (L7)
             *       |----------   ------------|
             *   (L2)|                         | (L8)
             *       |-------------------------|
             *                 （L1）
             */
            // 识别条件
            // 8条边
            // 相邻边是互相垂直的
            // 两个凹点
            // 其中有一组平行边满足 L1//L3,L1//L5,L1//L7 L1=L3+l5+l7, L3与L7共线，L5与L3,L7不共线
            var lines = poly.ToLines();
            if (lines.Count != 8 || !IsAllAnglesVertical(lines))
            {
                return false;
            }
            var concavePoints = GetConcavePoints(poly);
            if (concavePoints.Count != 2)
            {
                return false;
            }
            var l1l2Edges = lines.FindTTypeMainEdge(); //<L1,(L3,L5,L7)>
            return l1l2Edges.Count == 1;
        }

        private List<Point3d> GetConcavePoints(Polyline polyline)
        {
            var result = polyline.PointClassify();
            return result.Where(o => o.Value == 2).Select(o => o.Key).ToList();
        }
        private List<Point3d> GetConvexPoints(Polyline polyline)
        {
            var result = polyline.PointClassify();
            return result.Where(o => o.Value == 1).Select(o => o.Key).ToList();
        }       

        private bool IsRectType(Polyline poly)
        {
            // 特点：四条边、对边平行、相邻边垂直
            // 识别条件
            // 多段线是闭合的
            // 有4条边
            // 相邻边是互相垂直的
            var lines = poly.ToLines();
            return lines.Count == 4 && IsAllAnglesVertical(lines);
        }

        private bool IsAllAnglesVertical(List<Tuple<Point3d, Point3d>> lines)
        {
            for(int i=0;i<lines.Count;i++)
            {
                var currentDir = lines[i % lines.Count].GetLineDirection();
                var nextDir = lines[(i + 1) % lines.Count].GetLineDirection();
                if (!currentDir.IsVertical(nextDir))
                {
                    return false;
                }
            }
            return true;
        }            
    }
}
