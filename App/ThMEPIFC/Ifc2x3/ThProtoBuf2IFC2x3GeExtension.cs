using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.ProfileResource;
using ThMEPIFC.Geometry;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThProtoBuf2IFC2x3GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, ThTCHPoint3d point)
        {
            return model.Instances.New<IfcCartesianPoint>(c =>
            {
                c.SetXYZ(point.X, point.Y, point.Z);
            });
        }

        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, XbimPoint3D point)
        {
            return model.Instances.New<IfcCartesianPoint>(c =>
            {
                c.SetXYZ(point.X, point.Y, point.Z);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            IfcObjectPlacement relative_to = null)
        {
            var cs = ThXbimCoordinateSystem3D.Identity;
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(cs);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            ThTCHMatrix3d matrix, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(matrix);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model,
            ThTCHPoint3d origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(origin.ToXbimPoint3D());
            });
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, XbimVector3D vector)
        {
            return model.Instances.New<IfcDirection>(d =>
            {
                d.SetXYZ(vector.X, vector.Y, vector.Z);
            });
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this IfcStore model,
            IfcProfileDef profile, XbimVector3D direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = model.ToIfcDirection(direction);
                s.Position = model.ToIfcAxis2Placement3D(XbimPoint3D.Zero);
            });
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model,
            XbimPoint3D point)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Location = model.ToIfcCartesianPoint(point);
            });
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this IfcStore model,
            XbimPoint3D point, XbimVector3D direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = model.ToIfcCartesianPoint(point);
                p.RefDirection = model.ToIfcDirection(direction);
            });
        }

        private static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThTCHMatrix3d m)
        {
            return model.ToIfcAxis2Placement3D(new ThXbimCoordinateSystem3D(m));
        }

        private static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, ThXbimCoordinateSystem3D cs)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Axis = model.ToIfcDirection(cs.CS.ZAxis.ToXbimVector3D());
                p.RefDirection = model.ToIfcDirection(cs.CS.XAxis.ToXbimVector3D());
                p.Location = model.ToIfcCartesianPoint(cs.CS.Origin.ToXbimPoint3D());
            });
        }

        public static IfcArbitraryProfileDefWithVoids ToIfcArbitraryProfileDefWithVoids(this IfcStore model, ThTCHMPolygon e)
        {
            return model.Instances.New<IfcArbitraryProfileDefWithVoids>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = model.ToIfcCompositeCurve(e.Shell);
                foreach (var hole in e.Holes)
                {
                    var innerCurve = model.ToIfcCompositeCurve(hole);
                    d.InnerCurves.Add(innerCurve);
                }
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this IfcStore model, ThTCHMPolygon e)
        {
            return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = model.ToIfcCompositeCurve(e.Shell);
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this IfcStore model, ThTCHPolyline e)
        {
            return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = model.ToIfcCompositeCurve(e);
            });
        }

        private static IfcPolyline ToIfcPolyline(this IfcStore model, ThTCHPoint3d startPt, ThTCHPoint3d endPt)
        {
            var poly = model.Instances.New<IfcPolyline>();
            poly.Points.Add(model.ToIfcCartesianPoint(startPt));
            poly.Points.Add(model.ToIfcCartesianPoint(endPt));
            return poly;
        }

        private static IfcCircle ToIfcCircle(this IfcStore model, ThXbimCircle3D circle)
        {
            return model.Instances.New<IfcCircle>(c =>
            {
                c.Radius = circle.Geometry.Radius;
                c.Position = model.ToIfcAxis2Placement2D(circle.Geometry.CenterPoint.ToXbimPoint3D(), new XbimVector3D(0, 0, 1));
            });
        }

        private static IfcTrimmedCurve ToIfcTrimmedCurve(this IfcStore model, ThTCHPoint3d startPt, ThTCHPoint3d pt, ThTCHPoint3d endPt)
        {
            var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
            var circle = new ThXbimCircle3D(startPt.ToXbimPoint3D(), pt.ToXbimPoint3D(), endPt.ToXbimPoint3D());
            trimmedCurve.BasisCurve = model.ToIfcCircle(circle);
            trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
            trimmedCurve.SenseAgreement = !circle.IsClockWise();
            trimmedCurve.Trim1.Add(model.ToIfcCartesianPoint(startPt));
            trimmedCurve.Trim2.Add(model.ToIfcCartesianPoint(endPt));
            return trimmedCurve;
        }

        public static IfcCompositeCurve ToIfcCompositeCurve(this IfcStore model, ThTCHPolyline polyline)
        {
            var compositeCurve = ThIFC2x3Factory.CreateIfcCompositeCurve(model);
            var pts = polyline.Points;
            foreach (var segment in polyline.Segments)
            {
                var curveSegement = ThIFC2x3Factory.CreateIfcCompositeCurveSegment(model);
                if (segment.Index.Count == 2)
                {
                    //直线段
                    var startPt = pts[(int)segment.Index[0]];
                    var endPt = pts[(int)segment.Index[1]];
                    curveSegement.ParentCurve = model.ToIfcPolyline(startPt, endPt);
                    compositeCurve.Segments.Add(curveSegement);
                }
                else
                {
                    //圆弧段
                    var startPt = pts[(int)segment.Index[0]];
                    var midPt = pts[(int)segment.Index[1]];
                    var endPt = pts[(int)segment.Index[2]];
                    curveSegement.ParentCurve = model.ToIfcTrimmedCurve(startPt, midPt, endPt);
                    compositeCurve.Segments.Add(curveSegement);
                }
            }
            return compositeCurve;
        }
    }
}
