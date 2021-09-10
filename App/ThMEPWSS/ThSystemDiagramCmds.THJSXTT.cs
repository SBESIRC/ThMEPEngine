using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ThCADExtension;
using ThMEPWSS.Command;
using ThMEPWSS.Pipe.Engine;
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
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.model;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "-THJSXTT", CommandFlags.Modal)]
        public void THJSXTT()
        {
            using (var db = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个对象");//交互界面
            }
        }



        [CommandMethod("TIANHUACAD", "TestExractKitchens", CommandFlags.Modal)]
        public void TestExractKitchens()
        {
            using (var db = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个对象");//交互界面
            }
        }
    } 
}
