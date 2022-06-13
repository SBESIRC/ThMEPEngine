using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using GeometryExtensions;
using Linq2Acad;
using ThMEPElectrical;
using ThMEPElectrical.CAD;
using ThMEPLighting.Command;

namespace ThMEPLighting
{
    public class ThMEPWiringCmds
    {
        [CommandMethod("TIANHUACAD", "THZMLX", CommandFlags.Modal)]
        public void ThLightingRoute()
        {
            using (var cmd = new ThLightingRouteComand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZLX", CommandFlags.Modal)]
        public void ThAFASRoute()
        {
            using (var cmd = new ThAFASRouteCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THLXUCS", CommandFlags.Modal)]
        public void ThLXUcsCompass()
        {
            while (true)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    Active.Database.ImportCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME);
                    var objId = Active.Database.InsertCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME,
                        null);
                    var ucs2Wcs = Active.Editor.UCS2WCS();
                    var compass = acadDatabase.Element<BlockReference>(objId, true);
                    compass.TransformBy(ucs2Wcs);
                    var jig = new ThCompassDrawJig(Point3d.Origin.TransformBy(ucs2Wcs));
                    jig.AddEntity(compass);
                    PromptResult pr = Active.Editor.Drag(jig);
                    if (pr.Status != PromptStatus.OK)
                    {
                        compass.Erase();
                        break;
                    }
                    jig.TransformEntities();
                }
            }
        }
    }
}
