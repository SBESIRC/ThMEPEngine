using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.BoundaryRepresentation;
using Xbim.Ifc;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.GeometricModelResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3MeshExtension
    {
        private static IfcFace ToIfcFace(this IfcStore model, Point3dCollection vertices)
        {
            var ifcFace = model.Instances.New<IfcFace>();
            ifcFace.Bounds.Add(ToIfcFaceBound(model, vertices));
            return ifcFace;
        }

        private static IfcFaceBound ToIfcFaceBound(this IfcStore model, Point3dCollection vertices)
        {
            return model.Instances.New<IfcFaceBound>(b =>
            {
                b.Bound = model.ToIfcPolyLoop(vertices);
            });
        }

        private static IfcPolyLoop ToIfcPolyLoop(this IfcStore model, Point3dCollection vertices)
        {
            var polyLoop = model.Instances.New<IfcPolyLoop>();
            foreach (Point3d v in vertices)
            {
                polyLoop.Polygon.Add(model.ToIfcCartesianPoint(v));
            }
            return polyLoop;
        }

        public static IfcFaceBasedSurfaceModel ToIfcFaceBasedSurface(this IfcStore model, Solid3d solid)
        {
            // Reference：
            //  https://www.keanw.com/2011/03/generating-a-mesh-for-a-3d-solid-using-autocads-brep-api-from-net.html

            // Build the BRep topology object to traverse
            using (var brep = new Brep(solid))
            {
                // Create and set our mesh control object
                using (Mesh2dControl mc = new Mesh2dControl())
                {
                    // These settings seem extreme, but only result
                    // in ~500 faces for a sphere (during my testing,
                    // anyway). Other control settings are available
                    // TODO：这里需要根据具体情况设置
                    mc.MaxSubdivisions = 100;
                    mc.MaxNodeSpacing = Length(solid) / 2;

                    // Create a mesh filter object
                    using (Mesh2dFilter mf = new Mesh2dFilter())
                    {
                        // Use it to map our control settings to the Brep
                        mf.Insert(brep, mc);

                        // Generate a mesh using the filter
                        using (var m = new Mesh2d(mf))
                        {
                            var connectedFaceSet = model.Instances.New<IfcConnectedFaceSet>();
                            var faceBasedSurface = model.Instances.New<IfcFaceBasedSurfaceModel>();
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

                                connectedFaceSet.CfsFaces.Add(ToIfcFace(model, pts));
                            }
                            faceBasedSurface.FbsmFaces.Add(connectedFaceSet);
                            return faceBasedSurface;
                        }
                    }
                }
            }
        }

        private static double Length(Solid3d solid)
        {
            // Calculate the approximate size of our solid
            return solid.GeometricExtents.MinPoint.GetVectorTo(solid.GeometricExtents.MaxPoint).Length;
        }
    }
}
