using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Diagnostics;
using ThCADExtension;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimConflictService
    {

        public static bool NeedToCutOff(Line line, ThCADCoreNTSSpatialIndex wallLinesSI, double tolerance = 200.0)
        {
            //穿图的墙线crossGraphLines
            List<Line> crossGraphLines = ThDataTransformService.Change(ThGeometryOperationService.SelectFence(wallLinesSI, line));
            if (crossGraphLines.Count > 0)
            {
                //把crossGraphLines转换为同方向
                Vector3d dir = crossGraphLines[0].EndPoint - crossGraphLines[0].StartPoint;
                for (int i = 1; i < crossGraphLines.Count; i++)
                {
                    Vector3d tDir = crossGraphLines[i].EndPoint - crossGraphLines[i].StartPoint;
                    if (!ThCoordinateService.IsTheSameDirrection(tDir, dir))
                    {
                        crossGraphLines[i] = new Line(crossGraphLines[i].EndPoint, crossGraphLines[i].StartPoint);
                    }
                }

                //图线两边的距离最大值，两边最大值中取最小值
                double distance1 = 0;
                double distance2 = 0;
                foreach (Line l in crossGraphLines)
                {
                    Point3d pt1 = line.GetClosestPointTo(l.StartPoint, true);
                    Point3d pt2 = line.GetClosestPointTo(l.EndPoint, true);

                    double td1 = pt1.DistanceTo(l.StartPoint);
                    double td2 = pt2.DistanceTo(l.EndPoint);

                    if (distance1 < td1)
                        distance1 = td1;
                    if (distance2 < td2)
                        distance2 = td2;
                }

                if (Math.Min(distance1, distance2) > tolerance)
                    return true;
            }

            return false;
        }


        public static bool IsDimCrossBarrier(Line dim, ThCADCoreNTSSpatialIndex barrier, double tolerance=10)
        {
            if(dim.Length > tolerance)
            {
                //获取穿标注的参考线
                List<Line> crossDimLines = ThDataTransformService.Change(ThGeometryOperationService.SelectFence(barrier, dim));

                if (crossDimLines.Count > 0)
                {
                    foreach (Line l in crossDimLines)
                    {
                        // 过滤重合参考线
                        if (l.Length < tolerance || ThCoordinateService.IsParalleled(dim, l))
                            continue;

                        if (dim.LineIsIntersection(l))
                        {
                            Point3d p = dim.Intersection(l);
                            if (p.DistanceTo(dim.StartPoint) < tolerance || p.DistanceTo(dim.EndPoint) < tolerance)
                                continue;
                            else
                                return true;
                        }

                    }

                }
            }

            return false;
        }


        public static long GetOverlap(List<Polyline> dimensions, ThCADCoreNTSSpatialIndex texts, ThCADCoreNTSSpatialIndex mixColumnWall, DBObjectCollection dimedArea, ThCADCoreNTSSpatialIndex pipes, MPolygon room)
        {
            int w1 = 1, w2 = 1, w3 = 1, w4 = 1, w5 = 100;

            long area1 = w1 * GetOverlapArea(dimensions, texts);
            long area3 = w3 * GetOverlapArea(dimensions, dimedArea);

            long area2 = w2 * GetOverlapArea(dimensions, mixColumnWall);

            long area4 = w4 * GetDiffenceArea(dimensions, room);

            long len = w5 * GetOverlapLength(dimensions, pipes);

            return area1 + area2 + area3 + len + area4;
        }


        private static long GetOverlapArea(List<Polyline> dimensions, ThCADCoreNTSSpatialIndex texts)
        {
            if (texts.IsNull())
                return 0;

            List<Polyline> overlapTexts = new List<Polyline>();
            foreach (Polyline dimText in dimensions)
            {
                overlapTexts.AddRange(ThGeometryOperationService.Intersection(ThGeometryOperationService.SelectCrossingPolygon(texts, dimText), dimText));
            }

            long area = 0;
            foreach(Polyline overlap in overlapTexts)
            {
                if (overlap.Closed)
                {
                    area += (long)overlap.Area;
                }
          
            }

            return area;
        }

        private static long GetOverlapArea(List<Polyline> dimensions, DBObjectCollection dimedArea)
        {
            if (dimedArea.Count == 0)
                return 0;

            List<Polyline> overlapDims = new List<Polyline>();
            foreach (Polyline dimText in dimensions)
            {
                overlapDims.AddRange(ThDataTransformService.GetPolylines(dimText.Intersection(dimedArea)));
            }

            long area = 0;
            foreach (Polyline overlap in overlapDims)
            {
                if (overlap.Closed)
                {
                    area += (long)overlap.Area;
                }

            }

            return area;
        }

        private static long GetOverlapLength(List<Polyline> dimensions, ThCADCoreNTSSpatialIndex pipes)
        {
            if (pipes.IsNull())
                return 0;

            List<Polyline> overlapPipes = new List<Polyline>();
            foreach (Polyline dimText in dimensions)
            {
                overlapPipes.AddRange(ThGeometryOperationService.Intersection(ThGeometryOperationService.SelectCrossingPolygon(pipes, dimText), dimText));
            }

            long length = 0;
            foreach(Polyline overlap in overlapPipes)
            {
                length += (long)overlap.Length;
            }

            return length;
        }

        private static long GetDiffenceArea(List<Polyline> dimensions, MPolygon room)
        {
            long area = 0;
            List<Polyline> overlapDims = ThGeometryOperationService.Intersection(dimensions, room);

            foreach(Polyline d in dimensions)
            {
                area = area + (long)d.Area;
            }

            foreach (Polyline o in overlapDims)
            {
                if (o.Closed)
                    area = area - (long)o.Area;
            }

            return area;
        }



    }
}
