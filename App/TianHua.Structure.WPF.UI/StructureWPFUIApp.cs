﻿using Autodesk.AutoCAD.Runtime;
using TianHua.Structure.WPF.UI.Command;

namespace TianHua.Structure.WPF.UI
{
    public class StructureWPFUIApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THZLSCUI", CommandFlags.Session)]
        public void THZLUI()
        {
            using (var cmd = new MainBeamCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THCLSCUI", CommandFlags.Session)]
        public void THCLUI()
        {
            using (var cmd = new SecondaryBeamCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THSXSCUI", CommandFlags.Session)]
        public void THSXUI()
        {
            using (var cmd = new BuildBeamCmd())
            {
                cmd.Execute();
            }
        }
    }
}
