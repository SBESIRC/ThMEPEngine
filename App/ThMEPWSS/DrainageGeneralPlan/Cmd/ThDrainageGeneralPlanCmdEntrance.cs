using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageGeneralPlan
{
    public class ThDrainageGeneralPlanCmdEntrance
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThDrainageGeneralPlan", CommandFlags.Modal)]
        public void ThDrainageGeneralPlan()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var cmd = new ThDrainageGeneralPlanCmd())
                {
                    cmd.SubExecute();
                }
            }
        }
    }

}
