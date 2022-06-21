using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.ArchitecturePlane.Service
{
    /// <summary>
    /// 调整文字边界距离墙线的间隔
    /// </summary>
    internal class ThAdjustTextBoundaryService
    {
        private ThCADCoreNTSSpatialIndex LineSpatialIndex { get; set; }
        private double WallHoleMaxThick { get; set; } = 1000.0; // 墙洞厚度
        private double TextBoundaryDistanceToWall { get; set; }
        private double PointSearchRange { get; set; } = 5.0;
        public ThAdjustTextBoundaryService(DBObjectCollection lines,
            double textBoundaryDistanceToWall)
        {
            TextBoundaryDistanceToWall = textBoundaryDistanceToWall; // 文字边界距离线的间隙
            LineSpatialIndex = new ThCADCoreNTSSpatialIndex(lines);
        }

        /// <summary>
        /// 调整文字的位置
        /// </summary>
        /// <param name="texts"></param>
        public void Adjust(List<MarkInfo> marks)
        {
            marks.ForEach(m => Adjust(m));
        }

        public void SetWallHoleThick(double wallHoleMaxThick)
        {
            if(wallHoleMaxThick > 0.0)
            {
                WallHoleMaxThick = wallHoleMaxThick;
            }
        }

        public void SetPointSearchRange(double pointSearchRange)
        {
            if(pointSearchRange>0.0)
            {
                PointSearchRange = pointSearchRange;
            }
        }

        private void Adjust(MarkInfo mark)
        { 
            var height = GetMoveHeight(mark);
            if(height>0.0)
            {
                var mt = Matrix3d.Displacement(mark.MoveDir.GetNormal().MultiplyBy(height));
                var edgeSp = mark.BelongedLineSp.TransformBy(mt);
                var edgeEp = mark.BelongedLineEp.TransformBy(mt);
                SetTextBoundaryPos(mark.Mark, mark.MoveDir, edgeSp, edgeEp, TextBoundaryDistanceToWall);
            }            
        }
        private double GetMoveHeight(MarkInfo mark)
        {
            var wallThick = GetWallThick1(mark.BelongedLineSp, mark.BelongedLineEp);
            if (wallThick > 0.0)
            {
                return wallThick / 2.0;
            }
            wallThick = GetWallThick2(mark);
            if (wallThick > 0.0)
            {
                return wallThick / 2.0;
            }
            return GetHighestDistanceInRange(mark, WallHoleMaxThick/2.0);
        }
        private double GetWallThick1(Point3d sp, Point3d ep)
        {
            /*
             *      |                          |
             *                 
             *      |--------------------------|  (中心)  
             *                 
             *      |                          |
             */
            var thick = 0.0;
            var dir = sp.GetVectorTo(ep);
            var spEnvelop = ThDrawTool.CreateSquare(sp, PointSearchRange);
            var spLines = Query(spEnvelop).FilterVertical(dir);

            var epEnvelop = ThDrawTool.CreateSquare(ep, PointSearchRange);           
            var epLines = Query(epEnvelop).FilterVertical(dir);

            if (spLines.Count > 0 && epLines.Count > 0)
            {
                foreach (Line first in spLines.OfType<Line>())
                {
                    foreach (Line second in epLines.OfType<Line>())
                    {
                        if (Math.Abs(first.Length - second.Length) <= 5.0)
                        {
                            if (Math.Max(first.Length, second.Length) > thick)
                            {
                                thick = Math.Max(first.Length, second.Length);
                            }
                            break;
                        }
                    }
                }
            }
            spEnvelop.Dispose();
            epEnvelop.Dispose();
            return thick;
        }
        private double GetWallThick2(MarkInfo mark)
        {
            /*
             *      --------------------------
             *                 
             *      --------------------------  (中心)  
             *                 
             *      --------------------------
             */
            double wallThick = 0.0;
            var textWidth = GetTextWidth(mark.Mark);
            var textDir = mark.BelongedLineSp.GetVectorTo(mark.BelongedLineEp);            
            var textCenter = mark.BelongedLineSp.GetMidPt(mark.BelongedLineEp);
            var pt1 = textCenter + textDir.MultiplyBy(textWidth / 2.0);
            var pt2 = textCenter - textDir.MultiplyBy(textWidth / 2.0);
            var outline1 = CreateOutline(pt1, pt2, mark.MoveDir, WallHoleMaxThick / 2.0);
            var outline2 = CreateOutline(pt1, pt2, mark.MoveDir.Negate(), WallHoleMaxThick / 2.0);
            var parallelLines1 = FilterParallel(Query(outline1), textDir);
            var parallelLines2 = FilterParallel(Query(outline2), textDir);
            outline1.Dispose();
            outline2.Dispose();
            parallelLines1 = parallelLines1.Difference(parallelLines2);
            if (parallelLines1.Count>0 && parallelLines2.Count>0)
            {
                foreach(Line first in parallelLines1)
                {
                    var dis1 = first.StartPoint.GetProjectionDis(mark.BelongedLineSp, mark.BelongedLineEp);
                    if(dis1<=1.0)
                    {
                        continue;
                    }
                    foreach (Line second in parallelLines2)
                    {
                        var dis2 = second.StartPoint.GetProjectionDis(mark.BelongedLineSp, mark.BelongedLineEp);
                        if (dis2 <= 1.0)
                        {
                            continue;
                        }
                        if (Math.Abs(dis1- dis2)<=5.0)
                        {
                            if(Math.Max(dis1, dis2)> wallThick)
                            {
                                wallThick = Math.Max(dis1, dis2);
                            }
                            break;
                        }
                    }
                }
            }
            return wallThick;
        }

        private double GetHighestDistanceInRange(MarkInfo mark,double range)
        {
            /*
             *      --------------------------
             *                 |
             *      --------------------------    
             *                 |
             *                 |
             *               point
             */
            double distance = 0.0;
            var textWidth = GetTextWidth(mark.Mark);
            var textDir = mark.BelongedLineSp.GetVectorTo(mark.BelongedLineEp);
            var midPt = mark.BelongedLineSp.GetMidPt(mark.BelongedLineEp);
            var pt1 = midPt + textDir.MultiplyBy(textWidth / 2.0);
            var pt2 = midPt - textDir.MultiplyBy(textWidth / 2.0);
            var outline = CreateOutline(pt1, pt2, mark.MoveDir, WallHoleMaxThick / 2.0);
            var parallelLines = FilterParallel(Query(outline), textDir);
            outline.Dispose();
            if(parallelLines.Count>0)
            {
                parallelLines = Sort(parallelLines, midPt);
                var first = parallelLines.OfType<Line>().First();
                distance = midPt.GetProjectionDis(first.StartPoint, first.EndPoint);
            }
            return distance;
        }

        

        private void SetTextBoundaryPos(DBText text,Vector3d moveDir,Point3d edgeSp, Point3d edgeEp, double interval)
        {
            /* 
             *               M1023a
             * 
             *   ----------------------------------
             *  
             *  
             *   ----------------------------------Center
             */
            // 调整Text边界与edge的间隙
            var btmLinePts = GetBottomLinePts(text); // 底部起点和终点
            var topLinePts = GetTopLinePts(text);
            var midPt = edgeSp.GetMidPt(edgeEp);
            var basePt = midPt + moveDir.GetNormal().MultiplyBy(interval);

            var dis1 = btmLinePts.Item1.GetProjectionDis(edgeSp, edgeEp);
            var dis2 = topLinePts.Item1.GetProjectionDis(edgeSp, edgeEp);
            if (dis1> dis2)
            {
                var projectionPt = basePt.GetProjectPtOnLine(btmLinePts.Item1, btmLinePts.Item2);
                var mt = Matrix3d.Displacement(basePt - projectionPt);
                text.TransformBy(mt);
            }
            else
            {
                var projectionPt = basePt.GetProjectPtOnLine(topLinePts.Item1, topLinePts.Item2);
                var mt = Matrix3d.Displacement(basePt - projectionPt);
                text.TransformBy(mt);
            }
        }

        private DBObjectCollection Sort(DBObjectCollection lines,Point3d pt)
        {
            return lines.OfType<Line>()
                .OrderByDescending(o => o.GetClosestPointTo(pt, true).DistanceTo(pt))
                .ToCollection();
        }

        private DBObjectCollection FilterParallel(DBObjectCollection lines,Vector3d dir)
        {
            return lines.OfType<Line>()
                .Where(o => o.LineDirection().IsParallelToEx(dir))
                .ToCollection();
        }

        private DBObjectCollection Query(Polyline outline)
        {
            return LineSpatialIndex.SelectCrossingPolygon(outline);
        }

        private Polyline CreateOutline(Point3d pt1,Point3d pt2,Vector3d dir,double length)
        {
            var pt1U = pt1+dir.GetNormal().MultiplyBy(length);  
            var pt2U = pt2+dir.GetNormal().MultiplyBy(length);
            var pts = new Point3dCollection() { pt1, pt1U , pt2U, pt2 };
            return pts.CreatePolyline();
        }

        private double GetTextWidth(DBText text)
        {
            var clone = text.Clone() as DBText;   
            var mt = Matrix3d.Rotation(text.Rotation * -1.0, text.Normal, text.Position);
            clone.TransformBy(mt);
            var width = clone.GeometricExtents.MaxPoint.X - clone.GeometricExtents.MinPoint.X;
            clone.Dispose();
            return width;
        }
        private Tuple<Point3d,Point3d> GetBottomLinePts(DBText text)
        {
            var clone = text.Clone() as DBText;
            var mt1 = Matrix3d.Rotation(text.Rotation * -1.0, text.Normal, text.Position);
            clone.TransformBy(mt1);
            var pt1 = clone.GeometricExtents.MinPoint;
            var pt2 = new Point3d(clone.GeometricExtents.MaxPoint.X, pt1.Y,0);

            var mt2 = Matrix3d.Rotation(text.Rotation, text.Normal, text.Position);
            pt1 = pt1.TransformBy(mt2);
            pt2 = pt2.TransformBy(mt2);

            clone.Dispose();
            return Tuple.Create(pt1, pt2);
        }
        private Tuple<Point3d, Point3d> GetTopLinePts(DBText text)
        {
            var clone = text.Clone() as DBText;
            var mt1 = Matrix3d.Rotation(text.Rotation * -1.0, text.Normal, text.Position);
            clone.TransformBy(mt1);
            var pt2 = clone.GeometricExtents.MaxPoint;
            var pt1 = new Point3d(clone.GeometricExtents.MinPoint.X, pt2.Y, 0);

            var mt2 = Matrix3d.Rotation(text.Rotation, text.Normal, text.Position);
            pt1 = pt1.TransformBy(mt2);
            pt2 = pt2.TransformBy(mt2);

            clone.Dispose();
            return Tuple.Create(pt1, pt2);
        }
    }
}
