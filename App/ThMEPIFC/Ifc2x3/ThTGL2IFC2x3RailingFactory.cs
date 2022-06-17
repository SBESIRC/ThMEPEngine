using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.Ifc;
using Xbim.Ifc2x3.SharedBldgElements;
using ThMEPTCH.Model;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {
        public static IfcRailing CreateRailing(IfcStore model, ThTCHRailing railing, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Railing"))
            {
                var ret = model.Instances.New<IfcRailing>();
                ret.Name = "TH Railing";

                //create representation
                var centerline = railing.Outline as Polyline;
                var outlines = centerline.BufferFlatPL(railing.Thickness / 2.0);
                var profile = model.ToIfcArbitraryClosedProfileDef(outlines[0] as Entity);
                var solid = model.ToIfcExtrudedAreaSolid(profile, railing.ExtrudedDirection, railing.Depth);
                ret.Representation = CreateProductDefinitionShape(model, solid);

                //object placement
                ret.ObjectPlacement = model.ToIfcLocalPlacement(floor_origin);

                txn.Commit();
                return ret;
            }
        }
    }
}
