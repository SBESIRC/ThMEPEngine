using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPElectrical.Core;
using ThMEPElectrical.Assistant;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThMEPElectricalApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }


        [CommandMethod("TIANHUACAD", "THMainBeamRegion", CommandFlags.Modal)]
        public void ThBeamRegion()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                var polys = packageManager.DoMainBeamProfiles();
                DrawUtils.DrawProfile(polys.Polylines2Curves(), "MainBeamProfiles");
            }
        }

        [CommandMethod("TIANHUACAD", "THMainBeamPlace", CommandFlags.Modal)]
        public void ThProfilesPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THMainBeamRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager();
                packageManager.DoMainBeamRect();
            }
        }
    }
}
