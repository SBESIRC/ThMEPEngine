using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPTCH.CAD;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using ThMEPTCH.TCHArchDataConvert.THArchEntity;

namespace ThMEPTCH.TCHArchDataConvert
{
    class DBToTHEntityCommon
    {
        public static THArchEntityBase DBArchToTHArch(TArchEntity dbArchEntity)
        {
            if (dbArchEntity is TArchWall archWall)
                return TArchWallToEntityWall(archWall, 0, 0, 0, 0, new Vector3d(0, 0, 0));
            else if (dbArchEntity is TArchDoor archDoor)
                return TArchDoorToEntityDoor(archDoor);
            else if (dbArchEntity is TArchWindow archWindow)
                return TArchWindowToEntityWindow(archWindow);
            return null;
        }
        public static WallEntity TArchWallToEntityWall(TArchWall arch, double leftSpOffSet, double leftEpOffSet, double rightSpOffSet, double rightEpOffSet, Vector3d moveOffSet)
        {
            var wallEntity = new WallEntity(arch);
            var z = arch.StartPoint.Z;
            var spX = Math.Floor(arch.StartPoint.X);
            var spY = Math.Floor(arch.StartPoint.Y);
            var epX = Math.Floor(arch.EndPoint.X);
            var epY = Math.Floor(arch.EndPoint.Y);
            if (Math.Abs(z) < 5)
                z = 0;
            wallEntity.StartPoint = new Point3d(spX, spY, z) + moveOffSet;
            wallEntity.EndPoint = new Point3d(epX, epY, z) + moveOffSet;
            wallEntity.LeftWidth = arch.LeftWidth;
            wallEntity.RightWidth = arch.RightWidth;
            wallEntity.Height = arch.Height;
            wallEntity.EnumMaterial = arch.Material;
            if (arch.IsArc)
            {
                var sp = wallEntity.StartPoint;
                var ep = wallEntity.EndPoint;
                var leftWidth = arch.LeftWidth;
                var rightWidth = arch.RightWidth;
                var angle = Math.Atan(arch.Bulge) * 4;
                var xAxis = (ep - sp).GetNormal();
                var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                var length = sp.DistanceTo(ep);
                var centerPt = sp + xAxis.MultiplyBy(length / 2);
                var radius = length / (2 * Math.Sin(angle / 2));
                var moveDis = (length / 2) / Math.Tan(angle / 2);
                var arcCenter = centerPt + yAxis.MultiplyBy(moveDis);
                var sDir = (sp - arcCenter).GetNormal();
                var eDir = (ep - arcCenter).GetNormal();
                var innerArcSp = arcCenter + sDir.MultiplyBy(radius - leftWidth);
                var innerArcEp = arcCenter + eDir.MultiplyBy(radius - leftWidth);
                var outArcSp = arcCenter + sDir.MultiplyBy(radius + rightWidth);
                var outArcEp = arcCenter + eDir.MultiplyBy(radius + rightWidth);
                var sAngle = Vector3d.XAxis.GetAngleTo(sDir, Vector3d.ZAxis);
                var innerRadius = radius - leftWidth;
                var outRadius = radius + rightWidth;
                var innerSpOffSet = leftSpOffSet / innerRadius;
                var innerEpOffSet = leftEpOffSet / innerRadius;
                var outSpOffSet = rightSpOffSet / outRadius;
                var outEpOffSet = rightEpOffSet / outRadius;
                var eAngle = sAngle + angle;
                var innerArc = new Arc(arcCenter, Vector3d.ZAxis, innerRadius, sAngle - innerSpOffSet, eAngle + innerEpOffSet);
                var outArc = new Arc(arcCenter, Vector3d.ZAxis, outRadius, sAngle - outSpOffSet, eAngle + outEpOffSet);
                wallEntity.CenterCurve = new Arc(arcCenter, Vector3d.ZAxis, radius, sAngle, eAngle);
                wallEntity.LeftCurve = innerArc;
                wallEntity.RightCurve = outArc;
                innerArcSp = innerArc.StartPoint;
                innerArcEp = innerArc.EndPoint;
                outArcSp = outArc.StartPoint;
                outArcEp = outArc.EndPoint;
                var segments = new PolylineSegmentCollection();
                segments.Add(new PolylineSegment(outArcSp.ToPoint2D(), innerArcSp.ToPoint2D()));
                if (innerArcSp.DistanceTo(innerArc.StartPoint) < 1)
                {
                    segments.Add(new PolylineSegment(innerArc.StartPoint.ToPoint2D(), innerArc.EndPoint.ToPoint2D(), innerArc.BulgeFromCurve(innerArc.IsClockWise())));
                }
                else
                {
                    segments.Add(new PolylineSegment(innerArc.EndPoint.ToPoint2D(), innerArc.StartPoint.ToPoint2D(), -innerArc.BulgeFromCurve(innerArc.IsClockWise())));
                }
                segments.Add(new PolylineSegment(innerArcEp.ToPoint2D(), outArcEp.ToPoint2D()));
                if (outArcEp.DistanceTo(outArc.EndPoint) < 1)
                {
                    segments.Add(new PolylineSegment(outArc.EndPoint.ToPoint2D(), outArc.StartPoint.ToPoint2D(), -outArc.BulgeFromCurve(outArc.IsClockWise())));
                }
                else
                {
                    segments.Add(new PolylineSegment(outArc.StartPoint.ToPoint2D(), outArc.EndPoint.ToPoint2D(), outArc.BulgeFromCurve(outArc.IsClockWise())));
                }
                var temp = segments.Join(new Tolerance(2, 2));
                var newPLine = temp.First().ToPolyline();
                //newPLine.Closed = false;
                wallEntity.Outline = ThMPolygonTool.CreateMPolygon(newPLine, new List<Curve> { });
                wallEntity.Outline.Elevation = z;
            }
            else
            {
                var sp = wallEntity.StartPoint;
                var ep = wallEntity.EndPoint;
                wallEntity.CenterCurve = new Line(sp, ep);
                var wallDir = (ep - sp).GetNormal();
                var normal = Vector3d.ZAxis;
                var leftDir = normal.CrossProduct(wallDir).GetNormal();
                var spLeft = sp + leftDir.MultiplyBy(arch.LeftWidth) - wallDir.MultiplyBy(leftSpOffSet);
                var spRight = sp - leftDir.MultiplyBy(arch.RightWidth) - wallDir.MultiplyBy(rightSpOffSet);
                var epLeft = ep + leftDir.MultiplyBy(arch.LeftWidth) + wallDir.MultiplyBy(leftEpOffSet);
                var epRight = ep - leftDir.MultiplyBy(arch.RightWidth) + wallDir.MultiplyBy(rightEpOffSet);
                wallEntity.LeftCurve = new Line(spLeft, epLeft);
                wallEntity.RightCurve = new Line(spRight, epRight);
                var segments = new PolylineSegmentCollection();
                segments.Add(new PolylineSegment(spLeft.ToPoint2D(), spRight.ToPoint2D()));
                segments.Add(new PolylineSegment(spRight.ToPoint2D(), epRight.ToPoint2D()));
                segments.Add(new PolylineSegment(epRight.ToPoint2D(), epLeft.ToPoint2D()));
                segments.Add(new PolylineSegment(epLeft.ToPoint2D(), spLeft.ToPoint2D()));
                var temp = segments.Join(new Tolerance(2, 2));
                var newPLine = temp.First().ToPolyline();
                //newPLine.Closed = false;
                wallEntity.Outline = ThMPolygonTool.CreateMPolygon(newPLine, new List<Curve> { });
                wallEntity.Outline.Elevation = z;

            }
            return wallEntity;
        }

        public static DoorEntity TArchDoorToEntityDoor(TArchDoor arch)
        {
            var entity = new DoorEntity(arch)
            {
                Width = arch.Width,
                Height = arch.Height,
                Rotation = arch.Rotation,
                Thickness = arch.Thickness,
                BasePoint = arch.BasePoint,
                Outline = GetDoorOutline(arch),
                Swing = arch.Swing,
                OperationType = arch.OperationType,
            };
            return entity;
        }
        private static MPolygon GetDoorOutline(TArchDoor arch)
        {
            // 用MPolygon处理带洞的场景
            return ThMPolygonTool.CreateMPolygon(arch.Profile());
        }

        public static WindowEntity TArchWindowToEntityWindow(TArchWindow arch)
        {
            var entity = new WindowEntity(arch)
            {
                Width = arch.Width,
                Height = arch.Height,
                Rotation = arch.Rotation,
                Thickness = arch.Thickness,
                BasePoint = arch.BasePoint,
                Outline = GetWindowOutline(arch),
                WindowType = arch.WindowType,
            };
            return entity;
        }
        private static MPolygon GetWindowOutline(TArchWindow arch)
        {
            // 用MPolygon处理带洞的场景
            return ThMPolygonTool.CreateMPolygon(arch.Profile());
        }
    }
}
