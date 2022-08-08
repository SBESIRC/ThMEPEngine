using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThCADCore.NTS;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    public static class ThTGL2IFCSolid3dExtension
    {
        public static Solid3d CreateSolid3d(this ThTCHWall wall,Point3d floorOrigin)
        {
            Polyline outLine = null;
            var moveVector = floorOrigin.GetAsVector();
            if (wall.Outline is Polyline polyline)
            {
                moveVector += Vector3d.ZAxis.MultiplyBy(polyline.Elevation);
                outLine = polyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
            }
            else if (null == outLine) 
            {
                outLine = CenterPointToPolyline(wall.Origin,wall.XVector,wall.ExtrudedDirection,wall.Width,wall.Length);
                moveVector += Vector3d.ZAxis.MultiplyBy(wall.Origin.Z);
                outLine = outLine.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
            }
            if (null == outLine)
                return null;
            var solid = CreateExtrudedSolid(outLine, wall.Height, 0.0);
            foreach (var opening in wall.Openings) 
            {
                var openingSolid = CreateOpeningSolid(opening, floorOrigin);
                if (null == openingSolid)
                    continue;
                solid.BooleanOperation(BooleanOperationType.BoolSubtract, openingSolid);
            }
            return solid;
        }

        public static Solid3d CreateSolid3d(this ThTCHDoor door, Point3d floorOrigin)
        {
            Polyline outLine = null;
            var moveVector = floorOrigin.GetAsVector();
            if (null == door.Outline)
            {
                var centerPoint = door.Origin;
                var polyline = CenterPointToPolyline(centerPoint, door.XVector, door.ExtrudedDirection, door.Length, door.Width);
                moveVector += Vector3d.ZAxis.MultiplyBy(centerPoint.Z);
                outLine = polyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
            }
            if (null == outLine)
                return null;
            var solid = CreateExtrudedSolid(outLine, door.Height, 0.0);
            return solid;
        }

        public static Solid3d CreateSolid3d(this ThTCHWindow window, Point3d floorOrigin)
        {
            Polyline outLine = null;
            var moveVector = floorOrigin.GetAsVector();
            if (null == window.Outline)
            {
                var centerPoint = window.Origin;
                var polyline = CenterPointToPolyline(centerPoint, window.XVector, window.ExtrudedDirection, window.Length, window.Width);
                moveVector += Vector3d.ZAxis.MultiplyBy(centerPoint.Z);
                outLine = polyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
            }
            if (null == outLine)
                return null;
            var solid = CreateExtrudedSolid(outLine, window.Height, 0.0);
            return solid;
        }
        public static Solid3d CreateSolid3d(this ThTCHRailing railing, Point3d floorOrigin)
        {
            if (railing.Outline == null)
                return null;
            if (railing.Outline is Polyline centerline) 
            {
                var moveVector = floorOrigin.GetAsVector();
                moveVector += Vector3d.ZAxis.MultiplyBy(centerline.Elevation);
                var outlines = centerline.BufferFlatPL(railing.Width / 2.0)[0] as Polyline;
                var polyline = outlines.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
                var solid = CreateExtrudedSolid(polyline, railing.Height, 0.0);
                return solid;
            }
            return null;
        }
        public static Solid3d CreateSlabSolid(this ThTCHSlab slab, Point3d floorOrigin)
        {
            var moveVector = floorOrigin.GetAsVector();
            if (slab.Outline is Polyline pline)
            {
                moveVector += slab.ExtrudedDirection.MultiplyBy(pline.Elevation);
                var outPolyline =  pline.Clone() as Polyline;
                outPolyline.Elevation = 0;
                outPolyline = outPolyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
                var slabSolid = CreateExtrudedSolid(outPolyline, -slab.Height, 0.0);
                if (slabSolid != null)
                { 
                    // 首先拉伸板的轮廓，等所有轮廓都融合完成后再进行剪切
                    foreach (var data in slab.Descendings)
                    {
                        if (data.IsDescending)
                        {
                            var outLine = data.Outline.Clone() as Polyline;
                            outLine.Elevation = 0;
                            var wrap = outLine
                                .Buffer(data.DescendingWrapThickness)
                                .OfType<Polyline>()
                                .OrderByDescending(p => p.Area)
                                .FirstOrDefault();
                            wrap = wrap.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
                            var wrapSolid = CreateExtrudedSolid(wrap, -(data.DescendingHeight + data.DescendingThickness), 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolUnite, wrapSolid);
                        }
                    }
                    foreach (var data in slab.Descendings)
                    {
                        if (data.IsDescending)
                        {
                            var outLine = data.Outline.Clone() as Polyline;
                            outLine.Elevation = 0;
                            outLine = outLine.GetTransformedCopy(Matrix3d.Displacement(moveVector + slab.ExtrudedDirection.MultiplyBy(1))) as Polyline;
                            var descendingSolid = CreateExtrudedSolid(outLine, -data.DescendingHeight-1, 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolSubtract, descendingSolid);
                        }
                        else
                        {
                            var outLine = data.Outline.Clone() as Polyline;
                            outLine.Elevation = 0;
                            outLine = outLine.GetTransformedCopy(Matrix3d.Displacement(moveVector + slab.ExtrudedDirection.MultiplyBy(1))) as Polyline;
                            var holeSolid = CreateExtrudedSolid(outLine, -(slab.Height + 2), 0.0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolSubtract, holeSolid);
                        }
                    }
                }
                return slabSolid;
            }
            return null;
        }

        public static Solid3d CreateOpeningSolid(this ThTCHOpening opening, Point3d floorOrigin) 
        {
            Polyline outLine = null;
            var moveVector = floorOrigin.GetAsVector();
            if (null == opening.Outline)
            {
                var centerPoint = opening.Origin;
                var polyline = CenterPointToPolyline(centerPoint,opening.XVector,opening.ExtrudedDirection,opening.Length,opening.Width);
                moveVector += Vector3d.ZAxis.MultiplyBy(centerPoint.Z);
                outLine = polyline.GetTransformedCopy(Matrix3d.Displacement(moveVector)) as Polyline;
            }
            if (null == outLine)
                return null;
            var solid = CreateExtrudedSolid(outLine, opening.Height, 0.0);
            return solid;
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
        private static Polyline CenterPointToPolyline(Point3d center,Vector3d xAxis,Vector3d zAxis,double xLength,double yLength) 
        {
            var yAxis = zAxis.CrossProduct(xAxis);
            var sp = center - xAxis.MultiplyBy(xLength / 2);
            var ep = center + xAxis.MultiplyBy(xLength / 2);
            var spLeft = sp + yAxis.MultiplyBy(yLength / 2);
            var spRight = sp - yAxis.MultiplyBy(yLength / 2);
            var epLeft = ep + yAxis.MultiplyBy(yLength / 2);
            var epRight = ep - yAxis.MultiplyBy(yLength / 2);
            var polyline = new Polyline();
            polyline.AddVertexAt(0, spLeft.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, spRight.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(2, epRight.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(3, epLeft.ToPoint2D(), 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }
    }
}