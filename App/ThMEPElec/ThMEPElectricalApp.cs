using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThMEPElectrical.Core;
using ThMEPElectrical.Assistant;

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
    }
}
