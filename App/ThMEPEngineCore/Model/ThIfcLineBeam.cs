﻿using System;
using System.Linq;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.BeamInfo.Model;

namespace ThMEPEngineCore.Model
{
    public sealed class ThIfcLineBeam : ThIfcBeam
    {
        public Vector3d Direction
        {
            get
            {
                return this.StartPoint.GetVectorTo(this.EndPoint);
            }
        }
        public double Length
        {
            get
            {
                return StartPoint.DistanceTo(EndPoint);
            }
        }
        public override void TransformBy(Matrix3d transform)
        {
            EndPoint = EndPoint.TransformBy(transform);
            StartPoint = StartPoint.TransformBy(transform);
            Outline = ThLineBeamOutliner.CreatOutline(StartPoint, EndPoint, Width);
        }
        public static ThIfcLineBeam Create(Polyline polyline, double height = 0.0, double distanceToFloor = 0.0)
        {
            var segments = new PolylineSegmentCollection(polyline);
            var enumerable = segments.Where(o => o.IsLinear).OrderByDescending(o => o.ToLineSegment().Length);
            var beam = new LineBeam(
                new Line(enumerable.ElementAt(0).StartPoint.ToPoint3d(), enumerable.ElementAt(0).EndPoint.ToPoint3d()),
                new Line(enumerable.ElementAt(1).StartPoint.ToPoint3d(), enumerable.ElementAt(1).EndPoint.ToPoint3d()));
            return new ThIfcLineBeam()
            {
                Height = height,
                Width = beam.Width,
                EndPoint = beam.EndPoint,
                Outline = beam.BeamBoundary,
                StartPoint = beam.StartPoint,
                Uuid = Guid.NewGuid().ToString(),
                DistanceToFloor = distanceToFloor
            };
        }
        public static ThIfcLineBeam Create(ThIfcBeamAnnotation annotation)
        {
            var outline = ThLineBeamOutliner.CreatOutline(annotation.StartPoint, annotation.EndPoint, annotation.Size.X);
            outline.TransformBy(annotation.Matrix);
            return new ThIfcLineBeam()
            {
                Outline = outline,
                Width = annotation.Size.X,
                Height = annotation.Size.Y,
                StartPoint = annotation.StartPoint.TransformBy(annotation.Matrix),
                EndPoint = annotation.EndPoint.TransformBy(annotation.Matrix),
                Uuid = Guid.NewGuid().ToString(),
                DistanceToFloor = double.Parse(annotation.Attributes[ThMEPEngineCoreCommon.BEAM_GEOMETRY_DISTANCETOFLOOR])
            };
        }
        public static ThIfcLineBeam Create(ThIfcLineBeam olderLineBeam, double startExtend, double endExtend)
        {
            return Create(ThLineBeamOutliner.ExtendBoth(olderLineBeam, startExtend, endExtend), olderLineBeam.Height, olderLineBeam.DistanceToFloor);
        }
        public static ThIfcLineBeam Create(ThIfcLineBeam olderLineBeam, Point3d startPt, Point3d endPt)
        {
            return Create(ThLineBeamOutliner.CreatOutline(startPt, endPt, olderLineBeam.Width), olderLineBeam.Height, olderLineBeam.DistanceToFloor);
        }
    }
}
