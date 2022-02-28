using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThShapeAnalysisService
    {
        public ShapeCode Analysis(Polyline poly)
        {
            // 不支持弧
            if(!ThMEPFrameService.IsClosed(poly,1.0) || HasArc(poly))
            {
                return ShapeCode.Unknown;
            }
            if(IsRectType(poly))
            {
                return ShapeCode.Rect;
            }
            else if(IsLType(poly))
            {
                return ShapeCode.L;
            }
            return ShapeCode.Unknown;
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
            var lines = ToLines(poly);
            if(lines.Count!=6 || !IsAllAnglesVertical(lines))
            {
                return false;
            }
            //TODO
            return false;
        }

        private List<Tuple<int,List<int>>> FindLTypeEdge(List<Tuple<Point3d, Point3d>> lines)
        {
            /*         -----
             *         |   | 
             *         |   |
             *         |   |
             *  (L2)   |   |
             *         |   ------------
             *         |              |
             *         ----------------
             *             （L1）
             */
            var results = new List<Tuple<int, List<int>>>();
            for (int i =0;i<lines.Count;i++)
            {
                var currentDir = GetLineDirection(lines[i]);
                var parallels = new List<int>();
                for (int j = 0; j < lines.Count; j++)
                {
                    if(i==j)
                    {
                        continue;
                    }
                    var nextDir = GetLineDirection(lines[j]);
                    if (currentDir.IsParallelToEx(nextDir) && 
                        !ThGeometryTool.IsCollinearEx(lines[i].Item1,lines[i].Item2, lines[j].Item1, lines[j].Item2))
                    {
                        parallels.Add(j);
                    }
                }
                if (parallels.Count != 2)
                {
                    continue;
                }
                var length = parallels.Sum(index => GetLineDistance(lines[index]));
                if (!IsEqual(GetLineDistance(lines[i]), length))
                {
                    continue;
                }
                //
                throw new NotImplementedException();
            }
            return results;
        }

        private bool IsEqual(double first,double second,double tolerance=1e-6)
        {
            return Math.Abs(first - second) <= tolerance;
        }


        private bool IsRectType(Polyline poly)
        {
            // 特点：四条边、对边平行、相邻边垂直
            var lines = ToLines(poly);
            if(lines.Count!=4)
            {
                return false;
            }
            var line1Dir = GetLineDirection(lines[0]);
            var line2Dir = GetLineDirection(lines[1]);
            var line3Dir = GetLineDirection(lines[2]);
            var line4Dir = GetLineDirection(lines[3]);
            if (line1Dir.IsParallelToEx(line3Dir) && line2Dir.IsParallelToEx(line4Dir))
            {
                if (line1Dir.IsVertical(line2Dir))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsAllAnglesVertical(List<Tuple<Point3d, Point3d>> lines)
        {
            for(int i=0;i<lines.Count;i++)
            {
                var currentDir = GetLineDirection(lines[i% lines.Count]);
                var nextDir = GetLineDirection(lines[(i+1)%lines.Count]);
                if (!currentDir.IsVertical(nextDir))
                {
                    return false;
                }
            }
            return true;
        }

        private Vector3d GetLineDirection(Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.GetVectorTo(linePtPair.Item2).GetNormal();
        }

        private double GetLineDistance(Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.DistanceTo(linePtPair.Item2);
        }

        private List<Tuple<Point3d, Point3d>> ToLines(Polyline poly)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            for(int i =0;i<poly.NumberOfVertices-1;i++)
            {
                var st = poly.GetSegmentType(i);
                if(st == SegmentType.Line)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    results.Add(Tuple.Create(lineSeg.StartPoint,lineSeg.EndPoint));
                }
            }
            return results;
        }
    }
    internal enum ShapeCode
    {
        L,
        T,
        Rect,
        Unknown
    }
}
