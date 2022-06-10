using System;
using Xbim.Ifc;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
using BrFace = Autodesk.AutoCAD.BoundaryRepresentation.Face;
using BrShell = Autodesk.AutoCAD.BoundaryRepresentation.Shell;

namespace ThMEPIFC
{
    public static class ThTGL2IFCBrepExtension
    {
        /// <summary>
        /// CAD中的三维实体（Solid3d)到IFC Brep的表达转换
        /// </summary>
        /// <param name="model"></param>
        /// <param name="solid"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static IfcFacetedBrepWithVoids ToIfcFacetedBrep(this IfcStore model, Solid3d solid)
        {
            // Reference:
            // https://through-the-interface.typepad.com/through_the_interface/2008/09/traversing-a-3d.html

            // Build the BRep topology object to traverse
            var brep = new Brep(solid);

            // Get all the Complexes which are primary BRep
            // elements and represent a conceptual topological
            // entity of connected shell boundaries.
            var facetedBrepWithVoids = model.Instances.New<IfcFacetedBrepWithVoids>();
            foreach (var complex in brep.Complexes)
            {
                // Get all the shells within a complex. Shells
                // are secondary BRep entities that correspond
                // to a collection of neighboring surfaces on a
                // solid
                foreach (var shell in complex.Shells)
                {
                    if (shell.ShellType == ShellType.ShellExterior)
                    {
                        facetedBrepWithVoids.Outer = model.ToIfcClosedShell(shell);
                    }
                    else if (shell.ShellType == ShellType.ShellInterior)
                    {
                        facetedBrepWithVoids.Voids.Add(model.ToIfcClosedShell(shell));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            return facetedBrepWithVoids;
        }

        private static IfcPolyLoop ToIfcPolyLoop(this IfcStore model, BoundaryLoop boundaryLoop)
        {
            var polyLoop = model.Instances.New<IfcPolyLoop>();
            foreach (var v in boundaryLoop.Vertices)
            {
                polyLoop.Polygon.Add(model.ToIfcCartesianPoint(v.Point));
            }
            return polyLoop;
        }

        private static IfcFaceBound ToIfcFaceBound(this IfcStore model, BoundaryLoop boundaryLoop)
        {
            return model.Instances.New<IfcFaceBound>(b =>
            {
                b.Bound = model.ToIfcPolyLoop(boundaryLoop);
            });
        }

        private static IfcFace ToIfcFace(this IfcStore model, BrFace brFace)
        {
            var ifcFace = model.Instances.New<IfcFace>();
            foreach (BoundaryLoop lp in brFace.Loops)
            {
                ifcFace.Bounds.Add(ToIfcFaceBound(model, lp));
            }
            return ifcFace;
        }

        private static IfcClosedShell ToIfcClosedShell(this IfcStore model, BrShell shell)
        {
            var ifcClosedShell = model.Instances.New<IfcClosedShell>();
            foreach (var face in shell.Faces)
            {
                ifcClosedShell.CfsFaces.Add(ToIfcFace(model, face));
            }
            return ifcClosedShell;
        }
    }
}
