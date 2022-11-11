using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper;
using Dreambuild.AutoCAD;

using ThMEPEngineCore.Algorithm;


namespace ThMEPWSS.PumpSectionalView
{
    public class ThPumpSectionalCmdEntrance
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThHighFireWaterTankViewCreate", CommandFlags.Modal)]
        public void ThHighFireWaterTankView()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
               

                using (var cmd = new ThHighFireWaterTankCmd())
                {
                    cmd.SubExecute();
                }


            }
        }
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThLifePumpViewCreate", CommandFlags.Modal)]
        public void ThLifePumpView()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {


                using (var cmd = new ThLifePumpCmd())
                {
                    cmd.SubExecute();
                }


            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThFirePumpViewCreate", CommandFlags.Modal)]
        public void ThFirePumpView()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThFirePumpCmd())
                {
                    cmd.SubExecute();
                }


            }
        }

    }
}
