using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;
using ThCADCore.NTS;

namespace ThMEPTCH.TCHXmlDataConvert.TCHEntityToThIfc
{
    [TCHConvertAttribute("天正墙转中间数据")]
    class TCHXmlWallToThIfcWall : TCHConvertBase
    {
        private double ArcChord = 500;
        public TCHXmlWallToThIfcWall()
        {
            AcceptTCHEntityTypes.Add(typeof(TCH_WALL));
        }

        public override List<object> ConvertToBuidingElement()
        {
            var thIfcWalls = new List<object>();
            if (null == TCHXmlEntities || TCHXmlEntities.Count < 1)
                return thIfcWalls;
            foreach (var item in TCHXmlEntities)
            {
                if (item is TCH_WALL tch_Wall)
                {
                    ThTCHWall newWall = null;
                    if (IsShapeWall(tch_Wall))
                    {
                    }
                    else if (IsArcWall(tch_Wall))
                    {
                        newWall = GetArcWall(tch_Wall);
                    }
                    else 
                    {
                        newWall = GetRectangleWall(tch_Wall);
                    }
                    if (newWall != null)
                    {
                        newWall.Uuid = item.Object_ID.value;
                        thIfcWalls.Add(newWall); 
                    }
                    
                }
            }
            return thIfcWalls;
        }
        bool IsArcWall(TCH_WALL xmlEntity) 
        {
            if (null == xmlEntity)
                return false;
            return Math.Abs(xmlEntity.Baseline.Central_ang.GetDoubleValue() - 0.0) > 0.001;
        }
        bool IsShapeWall(TCH_WALL xmlEntity) 
        {
            if (null == xmlEntity)
                return false;
            return xmlEntity.ShapeWall.GetIntValue()>0;
        }
        bool IsOffCentre(TCH_WALL xmlEntity) 
        {
            if (null == xmlEntity)
                return false;
            var leftWidth = xmlEntity.Width.Left_width.GetDoubleValue();
            var rightWidth = xmlEntity.Width.Right_width.GetDoubleValue();
            return Math.Abs(leftWidth - rightWidth) > 1;
        }
        ThTCHWall GetRectangleWall(TCH_WALL xmlEntity) 
        {
            var sp = xmlEntity.Baseline.Start_point.GetCADPoint().Value;
            var ep = xmlEntity.Baseline.End_point.GetCADPoint().Value;
            var leftWidth = xmlEntity.Width.Left_width.GetDoubleValue();
            var rightWidth = xmlEntity.Width.Right_width.GetDoubleValue();
            if (IsOffCentre(xmlEntity))
            {
                var xAxis = (ep - sp).GetNormal();
                var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                var moveDis = (leftWidth - rightWidth) / 2;
                sp = sp + yAxis.MultiplyBy(moveDis);
                ep = ep + yAxis.MultiplyBy(moveDis);
            }
            var wallWidth = leftWidth + rightWidth;
            var wallHeight = xmlEntity.Height.WallHeight();
            var newWall = new ThTCHWall(sp, ep, wallWidth, wallHeight);
            return newWall;
        }
        ThTCHWall GetArcWall(TCH_WALL xmlEntity)
        {
            var sp = xmlEntity.Baseline.Start_point.GetCADPoint().Value;
            var ep = xmlEntity.Baseline.End_point.GetCADPoint().Value;
            
            var leftWidth = xmlEntity.Width.Left_width.GetDoubleValue();
            var rightWidth = xmlEntity.Width.Right_width.GetDoubleValue();
            var angle = xmlEntity.Baseline.Central_ang.GetDoubleValue();
            var xAxis = (ep - sp).GetNormal();
            var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
            var length = sp.DistanceTo(ep);
            var centerPt = sp + xAxis.MultiplyBy(length / 2);
            var radius = length / (2 * Math.Sin(angle / 2));
            var moveDis = (length / 2) / Math.Tan(angle / 2);
            var arcCenter = centerPt + yAxis.MultiplyBy(moveDis);
            var sDir = (sp - arcCenter).GetNormal();
            var eDir = (ep - arcCenter).GetNormal();
            var innerArcSp =arcCenter + sDir.MultiplyBy(radius - leftWidth);
            var innerArcEp = arcCenter + eDir.MultiplyBy(radius - leftWidth);
            var outArcSp = arcCenter + sDir.MultiplyBy(radius + rightWidth);
            var outArcEp = arcCenter + eDir.MultiplyBy(radius + rightWidth);
            var sAngle = Vector3d.XAxis.GetAngleTo(sDir, Vector3d.ZAxis);
            var eAngle = sAngle + angle;
            var innerArc = new Arc(arcCenter,Vector3d.ZAxis, radius - leftWidth, sAngle, eAngle);
            var outArc = new Arc(arcCenter, Vector3d.ZAxis, radius + rightWidth, sAngle, eAngle);

           
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

            /*var curves = new DBObjectCollection();
           var tempLines = new List<Line>();
           var entitySet = new DBObjectCollection();
           var innerArcLines = innerArc.TessellateArcWithChord(ArcChord);
           innerArcLines.Explode(entitySet);
           foreach (var obj in entitySet)
           {
               tempLines.Add(obj as Line);
           }
           entitySet.Clear();
           var outArcLines = outArc.TessellateArcWithChord(ArcChord);
           outArcLines.Explode(entitySet);
           foreach (var obj in entitySet)
           {
               tempLines.Add(obj as Line);
           }
           tempLines.Add(new Line(innerArcSp, outArcSp));
           tempLines.Add(new Line(innerArcEp, outArcEp));
           foreach (var line in tempLines) 
           {
                var eLine = line.ExtendLine(1);
               curves.Add(eLine);
           }
           var pLines = curves.PolygonsEx().OfType<Polyline>().ToList();
           if (pLines == null || pLines.Count < 1)
           {
               return null;
           }
           */
            var wallHeight = xmlEntity.Height.WallHeight();
            var newWall = new ThTCHWall(newPLine, wallHeight);
            return newWall;
        }
    }
}
