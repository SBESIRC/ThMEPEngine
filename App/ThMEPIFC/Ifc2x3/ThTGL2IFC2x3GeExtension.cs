using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, Point3d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }
        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, Point2d point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXY(point.X, point.Y);
            return pt;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, Point3d point)
        {
            var placement = model.Instances.New<IfcAxis2Placement3D>();
            placement.Location = model.ToIfcCartesianPoint(point);
            return placement;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this IfcStore model, CoordinateSystem3d cs)
        {
            return model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Axis = model.ToIfcDirection(cs.Zaxis);
                p.RefDirection = model.ToIfcDirection(cs.Xaxis);
                p.Location = model.ToIfcCartesianPoint(cs.Origin);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model, CoordinateSystem3d cs, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(cs);
            });
        }

        public static IfcLocalPlacement ToIfcLocalPlacement(this IfcStore model, Point3d origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = model.ToIfcAxis2Placement3D(origin);
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this IfcStore model, Entity e)
        {
            if (e is Polyline p)
            {
                return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
                {
                    d.ProfileType = IfcProfileTypeEnum.AREA;
                    d.OuterCurve = ThTGL2IFC2x3DbExtension.ToIfcCompositeCurve(model, p);
                });
            }
            else if (e is MPolygon mp)
            {
                return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
                {
                    d.ProfileType = IfcProfileTypeEnum.AREA;
                    d.OuterCurve = ThTGL2IFC2x3DbExtension.ToIfcCompositeCurve(model, mp);
                });
            }
            throw new NotSupportedException();
        }

        public static IfcRectangleProfileDef ToIfcRectangleProfileDef(this IfcStore model, double xDim, double yDim)
        {
            return model.Instances.New<IfcRectangleProfileDef>(d =>
            {
                d.XDim = xDim;
                d.YDim = yDim;
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.Position = model.ToIfcAxis2Placement2D(Point2d.Origin);
            });
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this IfcStore model, IfcProfileDef profile, Vector3d direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = model.ToIfcDirection(direction);
                s.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);
            });
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this IfcStore model, Point2d point)
        {
            var placement = model.Instances.New<IfcAxis2Placement2D>();
            placement.Location = model.ToIfcCartesianPoint(point);
            return placement;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this IfcStore model, Point2d point, Vector2d direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = model.ToIfcCartesianPoint(point);
                p.RefDirection = model.ToIfcDirection(direction);
            });
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, Vector3d vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcDirection ToIfcDirection(this IfcStore model, Vector2d vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXY(vector.X, vector.Y);
            return direction;
        }

        public static Point3d ToAcGePoint3d(this IfcStore model, IfcCartesianPoint point)
        {
            return new Point3d(point.X, point.Y, point.Z);
        }
    }
}