using System;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThCADCore.NTS;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC
{
    public static class ThTGL2IFCSolid3dExtension
    {
        public static Solid3d CreateSolid3d(this ThTCHWall wall)
        {
            throw new NotImplementedException();
        }

        public static Solid3d CreateSlabSolid(this ThTCHSlab slab)
        {
            // 首先拉伸板的轮廓
            if (slab.Outline is Polyline pline)
            {
                var slabSolid = CreateExtrudedSolid(pline, -slab.Thickness, 0.0);
                if (slabSolid != null)
                {
                    foreach (var data in slab.Descendings)
                    {
                        if (data.IsDescending)
                        {
                            var wrap = data.Outline
                                .Buffer(data.DescendingWrapThickness)
                                .OfType<Polyline>()
                                .OrderByDescending(p => p.Area)
                                .FirstOrDefault();
                            var wrapSolid = CreateExtrudedSolid(wrap, -(data.DescendingHeight + data.DescendingThickness), 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolUnite, wrapSolid);
                            var descendingSolid = CreateExtrudedSolid(data.Outline, -data.DescendingHeight, 0);
                            slabSolid.BooleanOperation(BooleanOperationType.BoolSubtract, descendingSolid);
                        }
                        else
                        {
                            var outLine = data.Outline.GetTransformedCopy(Matrix3d.Displacement(slab.ExtrudedDirection.MultiplyBy(1))) as Polyline;
                            var holeSolid = CreateExtrudedSolid(outLine, -(slab.Thickness + 2), 0.0);
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