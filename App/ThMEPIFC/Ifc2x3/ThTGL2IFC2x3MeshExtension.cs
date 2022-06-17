using System;
using Xbim.Ifc;
using Xbim.Ifc2x3.GeometricModelResource;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
using AcFace = Autodesk.AutoCAD.DatabaseServices.Face;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3MeshExtension
    {
        public static IfcFaceBasedSurfaceModel ToIfcFaceBasedSurface(this IfcStore model, Solid3d solid)
        {
            // Reference：
            //  https://through-the-interface.typepad.com/through_the_interface/2011/03/generating-a-mesh-for-a-3d-solid-using-autocads-brep-api-from-net.html

            // Build the BRep topology object to traverse
            using (var brep = new Brep(solid))
            {
                // Create and set our mesh control object
                using (Mesh2dControl mc = new Mesh2dControl())
                {
                    // These settings seem extreme, but only result
                    // in ~500 faces for a sphere (during my testing,
                    // anyway). Other control settings are available
                    mc.MaxSubdivisions = 100000000;
                    mc.MaxNodeSpacing = Length(solid) / 10000;

                    // Create a mesh filter object
                    using (Mesh2dFilter mf = new Mesh2dFilter())
                    {
                        // Use it to map our control settings to the Brep
                        mf.Insert(brep, mc);

                        // Generate a mesh using the filter
                        using (Mesh2d m = new Mesh2d(mf))
                        {
                            // Extract individual faces from the mesh data
                            foreach (Element2d e in m.Element2ds)
                            {
                                Point3dCollection pts = new Point3dCollection();
                                foreach (Node n in e.Nodes)
                                {
                                    pts.Add(n.Point);
                                    n.Dispose();
                                }
                                e.Dispose();

                                // A face could be a triangle or a quadrilateral
                                // (the Booleans indicate the edge visibility)
                                AcFace face = null;
                                if (pts.Count == 3)
                                {
                                    face = new AcFace(pts[0], pts[1], pts[2], true, true, true, true);
                                }
                                else if (pts.Count == 4)
                                {
                                    face = new AcFace(pts[0], pts[1], pts[2], pts[3], true, true, true, true);
                                }
                            }
                        }
                    }
                }
            }

            throw new NotImplementedException();
        }

        private static double Length(Solid3d solid)
        {
            // Calculate the approximate size of our solid
            return solid.GeometricExtents.MinPoint.GetVectorTo(solid.GeometricExtents.MaxPoint).Length;
        }
    }
}
