using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Linq;
using ThCADCore.NTS;
using ThMEPTCH.CAD;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    public static class ThProtoBuf2IFCSolid3dExtension
    {
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
                    // 首先拉伸板的轮廓，等所有轮廓都融合完成后再进行剪切
                    foreach (var data in slab.Descendings)
                    {
                        if (data.IsDescending)
                        {
                            var outLine = data.Outline.ToPolyline();
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
    }
}
