using System;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.AFAS.Utils
{
    internal static class ThAFASUtils
    {
        public static void MoveToOrigin(this ThBuildingElementVisitorManager vm, ThMEPOriginTransformer transformer)
        {
            vm.DB3ArchWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3WindowVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3RailingVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ColumnVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.ShearWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3CurtainWallVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
            vm.DB3DoorMarkVisitor.Results.ForEach(o =>
            {
                if (o is ThRawDoorMark doorMark)
                {
                    transformer.Transform(doorMark.Data as Entity);
                }
                transformer.Transform(o.Geometry);
            });
            vm.DB3DoorStoneVisitor.Results.ForEach(o => transformer.Transform(o.Geometry));
        }

        public static void MoveToXYPlane(this List<ThGeometry> geos)
        {
            geos.ForEach(g =>
            {
                if (g.Boundary != null)
                {
                    if (g.Boundary is Polyline polyline)
                    {
                        if (polyline.NumberOfVertices == 0)
                        {
                            var a = 0;
                        }
                        else
                        {


                            var vec = new Vector3d(0, 0, -polyline.GetPoint3dAt(0).Z);
                            var mt = Matrix3d.Displacement(vec);
                            g.Boundary.TransformBy(mt);
                        }
                    }
                    else if (g.Boundary is MPolygon mPolygon)
                    {
                        if (mPolygon.Shell().NumberOfVertices == 0)
                        {
                            var a = 0;
                        }
                        else
                        {

                            var vec = new Vector3d(0, 0, -1.0 * mPolygon.Shell().GetPoint3dAt(0).Z);
                            var mt = Matrix3d.Displacement(vec);
                            g.Boundary.TransformBy(mt);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
            });
        }
    }
}
