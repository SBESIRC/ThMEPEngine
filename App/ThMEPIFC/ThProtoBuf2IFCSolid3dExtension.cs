using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPTCH.CAD;
using ThMEPTCH.Model;
using Xbim.Common.Geometry;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.IO.Memory;
using Xbim.Ifc4.Interfaces;
using Xbim.Common;
using ThMEPIFC.Ifc2x3;

namespace ThMEPIFC
{
    public static class ThProtoBuf2IFCSolid3dExtension
    {
        public static readonly XbimVector3D XAxis = new XbimVector3D(1, 0, 0);
        public static readonly XbimVector3D YAxis = new XbimVector3D(0, 1, 0);
        public static readonly XbimVector3D ZAxis = new XbimVector3D(0, 0, 1);
        public static readonly XbimMatrix3D WordMatrix = new XbimMatrix3D(XbimVector3D.Zero);

        public static Solid3d CreateSlabSolid(this ThTCHSlabData slab, Point3d floorOrigin)
        {
            var moveVector = floorOrigin.GetAsVector();
            if (slab.BuildElement.Outline.Shell.Points.Count > 0)
            {
                var pline = slab.BuildElement.Outline.ToPolyline();
                moveVector += Vector3d.ZAxis.MultiplyBy(pline.Elevation);
                var outPolyline = pline.Clone() as Polyline;
                outPolyline.Elevation = 0;
                outPolyline = outPolyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
                var slabSolid = CreateExtrudedSolid(outPolyline, -slab.BuildElement.Height, 0.0);
                if (slabSolid != null)
                {
                    foreach (var data in slab.Descendings)
                    {
                        if (data.IsDescending)
                        {
                            var outlinebuffer = data.OutlineBuffer.ToPolyline();
                            outlinebuffer.Elevation = 0;
                            outlinebuffer = outlinebuffer.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
                            var wrapSolid = CreateExtrudedSolid(outlinebuffer, -(data.DescendingHeight + data.DescendingThickness), 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolUnite, wrapSolid);

                            var outLine = data.Outline.ToPolyline();
                            outLine.Elevation = 0;
                            outLine = outLine.GetTransformedCopy(Matrix3d.Displacement(moveVector + Vector3d.ZAxis.MultiplyBy(1))) as Polyline;
                            var descendingSolid = CreateExtrudedSolid(outLine, -data.DescendingHeight-1, 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolSubtract, descendingSolid);
                        }
                        else
                        {
                            var outLine = data.Outline.ToPolyline();
                            outLine.Elevation = 0;
                            outLine = outLine.GetTransformedCopy(Matrix3d.Displacement(moveVector + Vector3d.ZAxis.MultiplyBy(1))) as Polyline;
                            var holeSolid = CreateExtrudedSolid(outLine, -(slab.BuildElement.Height + 2), 0.0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolSubtract, holeSolid);
                        }
                    }
                }
                return slabSolid;
            }
            return null;
        }

        private static Solid3d CreateExtrudedSolid(Polyline pline, double height, double taperAngle)
        {
            try
            {
                var curves = new DBObjectCollection() { pline };
                var region = Region.CreateFromCurves(curves)[0] as Region;
                Solid3d ent = new Solid3d();
                ent.Extrude(region, height, taperAngle);
                return ent;
            }
            catch
            {
                return null;
            }
        }

        public static List<IXbimSolid> GetSlabSolid(this ThTCHSlabData slab, ThXbimSlabEngine slabxbimEngine)
        {
            var geometryParam = slab.BuildElement;
            var slabDes = slab.Descendings;
            XbimVector3D moveVector = XbimVector3D.Zero;
            IXbimSolidSet solidSet = slabxbimEngine.Engine.CreateSolidSet();
            var slabSolid = GetXBimSolid(geometryParam, moveVector, slabxbimEngine);
            foreach (var item in slabSolid)
                solidSet.Add(item);
            var openings = new List<IXbimSolid>();
            using (var txn = slabxbimEngine.Model.BeginTransaction("Create solid"))
            {
                var thisMove = moveVector;
                foreach (var item in slabDes)
                {
                    if (item.IsDescending)
                    {
                        var outLine = item.OutlineBuffer;
                        IXbimSolid opening = null;
                        IXbimSolid geoSolid = null;
                        geoSolid = GetXBimSolid2x3(outLine.Shell, thisMove, ZAxis.Negated(), item.DescendingThickness + item.DescendingHeight, slabxbimEngine);
                        opening = GetXBimSolid2x3(item.Outline.Shell, thisMove, ZAxis.Negated(), item.DescendingHeight, slabxbimEngine);
                        if (null == geoSolid || geoSolid.SurfaceArea < 10)
                            continue;
                        solidSet = solidSet.Union(geoSolid, 1);
                        openings.Add(opening);
                    }
                    else
                    {
                        geometryParam.Outline.Holes.Add(item.Outline.Shell);
                    }
                }
                foreach (var item in geometryParam.Outline.Holes)
                {
                    IXbimSolid opening = null;
                    if (item.Points == null || item.Points.Count < 1)
                        continue;
                    opening = GetXBimSolid2x3(item, thisMove, ZAxis.Negated(), geometryParam.Height + 0, slabxbimEngine);//geometryStretch.Outline.HolesMaxHeight
                    if (null == opening || opening.SurfaceArea < 10)
                        continue;
                    openings.Add(opening);
                }
                foreach (var item in openings)
                {
                    solidSet = solidSet.Cut(item, 1);
                }
                txn.Commit();
            }
            List<IXbimSolid> solids = new List<IXbimSolid>();
            foreach (var item in solidSet)
                solids.Add(item);
            return solids;
        }

        public static List<IXbimSolid> GetXBimSolid(ThTCHBuiltElementData geometryParam, XbimVector3D moveVector, ThXbimSlabEngine slabxbimEngine)
        {
            var resList = new List<IXbimSolid>();
            IXbimSolid geoSolid = null;
            using (var txn = slabxbimEngine.Model.BeginTransaction("Create solid"))
            {
                geoSolid = GetXBimSolid2x3(geometryParam, moveVector, slabxbimEngine.Model, slabxbimEngine);
                txn.Commit();
            }
            if (null != geoSolid)
                resList.Add(geoSolid);
            return resList;
        }

        private static IXbimSolid GetXBimSolid2x3(ThTCHBuiltElementData geometryStretch, XbimVector3D moveVector, MemoryModel memoryModel, ThXbimSlabEngine slabxbimEngine)
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = null;
            XbimPoint3D planeOrigin = XbimPoint3D.Zero + moveVector;// + ZAxis.Negated() * geometryStretch.Height;
            if (geometryStretch.Outline != null && geometryStretch.Outline.Shell != null && geometryStretch.Outline.Shell.Points.Count > 0)
            {
                profile = ToIfcArbitraryClosedProfileDef(memoryModel, geometryStretch.Outline.Shell);
            }
            if (profile == null)
                return null;
            var solid = memoryModel.ToIfcExtrudedAreaSolid(profile, ZAxis.Negated(), geometryStretch.Height);
            var geoSolid = slabxbimEngine.Engine.CreateSolid(solid);
            var realMove = moveVector;// + geometryStretch.ZAxis * geometryStretch.ZAxisOffSet;
            var trans = XbimMatrix3D.CreateTranslation(realMove.X, realMove.Y, realMove.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }

        private static IXbimSolid GetXBimSolid2x3(ThTCHPolyline polyline, XbimVector3D moveVector, XbimVector3D zAxis, double zHeight, ThXbimSlabEngine slabxbimEngine)
        {
            Xbim.Ifc2x3.ProfileResource.IfcProfileDef profile = ToIfcArbitraryClosedProfileDef(slabxbimEngine.Model, polyline);
            if (profile == null)
                return null;
            var solid = slabxbimEngine.Model.ToIfcExtrudedAreaSolid(profile, zAxis, zHeight);
            var geoSolid = slabxbimEngine.Engine.CreateSolid(solid);
            var trans = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
            geoSolid = geoSolid.Transform(trans) as IXbimSolid;
            return geoSolid;
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this MemoryModel model, ThTCHPolyline e)
        {
            return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
            {
                d.ProfileType = Xbim.Ifc2x3.ProfileResource.IfcProfileTypeEnum.AREA;
                d.OuterCurve = ToIfcCompositeCurve(model, e);
            });
        }

        public static IfcCompositeCurve ToIfcCompositeCurve(this MemoryModel model, ThTCHPolyline polyline)
        {
            var compositeCurve = CreateIfcCompositeCurve(model);
            var pts = polyline.Points;
            foreach (var segment in polyline.Segments)
            {
                var curveSegement = CreateIfcCompositeCurveSegment(model);
                if (segment.Index.Count == 2)
                {
                    //直线
                    var poly = model.Instances.New<IfcPolyline>();
                    poly.Points.Add(ToIfcCartesianPoint(model, pts[segment.Index[0].ToInt()].Point3D2XBimPoint()));
                    poly.Points.Add(ToIfcCartesianPoint(model, pts[segment.Index[1].ToInt()].Point3D2XBimPoint()));
                    curveSegement.ParentCurve = poly;
                    compositeCurve.Segments.Add(curveSegement);
                }
                else
                {
                    //圆弧
                    var pt1 = pts[segment.Index[0].ToInt()].Point3D2XBimPoint();
                    var pt2 = pts[segment.Index[2].ToInt()].Point3D2XBimPoint();
                    var midPt = pts[segment.Index[1].ToInt()].Point3D2XBimPoint();
                    //poly.Points.Add(ToIfcCartesianPoint(model, midPt));
                    //计算圆心，半径
                    var seg1 = midPt - pt1;
                    var seg1Mid = pt1 + seg1.Normalized() * (midPt.PointDistanceToPoint(pt1) / 2);
                    var seg2 = midPt - pt2;
                    var seg2Mid = pt2 + seg2.Normalized() * (midPt.PointDistanceToPoint(pt2) / 2);
                    var faceNormal = ZAxis;
                    var mid1Dir = seg1.Normalized().CrossProduct(faceNormal);
                    var mid2Dir = seg2.Normalized().CrossProduct(faceNormal);
                    if (FindIntersection(seg1Mid, mid1Dir, seg2Mid, mid2Dir, out XbimPoint3D arcCenter) == 1)
                    {
                        bool isCl = seg1.Normalized().CrossProduct(seg2.Normalized().Negated()).Z > 0;
                        var radius = arcCenter.PointDistanceToPoint(pt1);
                        var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
                        trimmedCurve.BasisCurve = model.Instances.New<IfcCircle>(c =>
                        {
                        c.Radius = radius;
                        c.Position = ToIfcAxis2Placement2D(model, arcCenter, XAxis);
                        });
                        trimmedCurve.MasterRepresentation = Xbim.Ifc2x3.GeometryResource.IfcTrimmingPreference.CARTESIAN;
                        trimmedCurve.SenseAgreement = isCl;
                        trimmedCurve.Trim1.Add(ToIfcCartesianPoint(model, pt1));
                        trimmedCurve.Trim2.Add(ToIfcCartesianPoint(model, pt2));
                        curveSegement.ParentCurve = trimmedCurve;
                        compositeCurve.Segments.Add(curveSegement);
                    }
                }
            }
            return compositeCurve;
        }

        private static IfcCompositeCurve CreateIfcCompositeCurve(MemoryModel model)
        {
            return model.Instances.New<IfcCompositeCurve>();
        }

        private static IfcCompositeCurveSegment CreateIfcCompositeCurveSegment(MemoryModel model)
        {
            return model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
        }

        public static IfcCartesianPoint ToIfcCartesianPoint(this MemoryModel model, XbimPoint3D point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }

        public static int ToInt(this uint value)
        {
            return int.Parse(value.ToString());
        }

        public static XbimPoint3D Point3D2XBimPoint(this ThTCHPoint3d point)
        {
            return new XbimPoint3D(point.X, point.Y, point.Z);
        }

        public static double PointDistanceToPoint(this XbimPoint3D point, XbimPoint3D targetPoint)
        {
            var disX = (point.X - targetPoint.X);
            var disY = (point.Y - targetPoint.Y);
            var disZ = (point.Z - targetPoint.Z);
            return Math.Sqrt(disX * disX + disY * disY + disZ + disZ);
        }

        /// <summary>
        /// 直线与直线相交(XOY平面)
        /// </summary>
        /// <param name="s0"></param>
        /// <param name="dir1"></param>
        /// <param name="s1"></param>
        /// <param name="dir2"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns>
        /// 0: 不相交
        /// 1: 只有一个交点
        /// 2: 共线
        /// </returns>
        public static int FindIntersection(XbimPoint3D s0, XbimVector3D dir1, XbimPoint3D s1, XbimVector3D dir2, out XbimPoint3D intersectionPoint)
        {
            intersectionPoint = XbimPoint3D.Zero;
            double Linear = 0.000000001;
            var P0 = s0;
            var D0 = dir1;
            var P1 = s1;
            var D1 = dir2;
            var E = P1 - P0;
            var kross = D0.X * D1.Y - D0.Y * D1.X;
            var sqrKross = kross * kross;
            var sqrLen0 = D0.X * D0.X + D0.Y * D0.Y;
            var sqrLen1 = D1.X * D1.X + D1.Y * D1.Y;
            var sqlEpsilon = Linear * Linear;
            //有一个交点
            if (sqrKross > sqlEpsilon * sqrLen0 * sqrLen1)
            {
                var s = (E.X * D1.Y - E.Y * D1.X) / kross;
                intersectionPoint = P0 + s * D0;
                return 1;
            }
            //如果线是平行的
            var sqrLenE = E.X * E.X + E.Y * E.Y;
            kross = E.X * D0.Y - E.Y * D0.X;
            sqrKross = kross * kross;

            var value = sqlEpsilon * sqrLen0 * sqrLenE;
            if (Math.Abs(sqrKross - value) > Linear && sqrKross > value)
                return 0;
            return 2;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this MemoryModel model, XbimPoint3D point, XbimVector3D direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = ToIfcCartesianPoint(model, new XbimPoint3D(point.X, point.Y, 0));
                p.RefDirection = ToIfcDirection(model, direction);
            });
        }

        public static IfcDirection ToIfcDirection(this MemoryModel model, XbimVector3D vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this MemoryModel model, IfcProfileDef profile, XbimVector3D direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = ToIfcDirection(model, direction);
                s.Position = ToIfcAxis2Placement3D(model, XbimPoint3D.Zero);
            });
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this MemoryModel model, XbimPoint3D point)
        {
            var placement = model.Instances.New<IfcAxis2Placement3D>();
            placement.Location = ToIfcCartesianPoint(model, point);
            return placement;
        }
        public static XbimVector3D Point3D2Vector(this XbimPoint3D point3D)
        {
            return new XbimVector3D(point3D.X, point3D.Y, point3D.Z);
        }

    }
}
