using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
using Dbg = ThMEPWSS.DebugNs.ThDebugTool;
using Linq2Acad;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThCADCore.NTS;
using DotNetARX;
using System;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.EditorInput;
using System.IO;
using AcHelper;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        /// <summary>
        /// Tian Hua Create water suply system diagram
        /// </summary>
        [CommandMethod("TIANHUACAD", "THCSSD", CommandFlags.Modal)]

        public void ThCreateWaterSuplySystemDiagram()
        {
            using (var cmd = new ThWaterSuplySystemDiagramCmd())
            {
                cmd.Execute();
            }
        }

        //public void ThCreateWaterSuplySystemDiagram()
        //{
            
        //    using (var db = Linq2Acad.AcadDatabase.Active())
            
        //    {
        //        var storey = new Storey();
        //        for (int i = 0; i < 32; i++)
        //        {

        //            storey.Draw(i);
        //        }
        //    }
            

        //}
    }
}
