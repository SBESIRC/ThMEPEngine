using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThAutoFireAlarmSystemCmd
    {

        [CommandMethod("TIANHUACAD", "ThAFASF", CommandFlags.Modal)]
        public void ThAFASF()
        {
            using (var cmd = new ThAutoFireAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThAFASA", CommandFlags.Modal)]
        public void ThAFASA()
        {
            using (var cmd = new ThWholeFireSystemDiagramCommand())
            {
                cmd.Execute();
                //var dm = Application.DocumentManager;
                //foreach (Document doc in dm)
                //{
                //    using (var db = Linq2Acad.AcadDatabase.Use(doc.Database))
                //    {
                //        var brs = db.ModelSpace.OfType<BlockReference>();
                //    }
                //}
            }
        }
    }
}
