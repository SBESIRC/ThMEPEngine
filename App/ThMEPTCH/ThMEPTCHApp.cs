﻿using AcHelper;
using Linq2Acad;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using ThMEPTCH.Model;
using ThMEPTCH.Engine;

namespace ThMEPTCH
{
    public class ThMEPTCHApp : IExtensionApplication
    {
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        [CommandMethod("TIANHUACAD", "THTCHWALL", CommandFlags.Modal)]
        public void THTCHWALL()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var engine = new ThTCHArchWallRecognitionEngine())
            {
                engine.Recognize(acadDatabase.Database, new Point3dCollection());
                engine.Elements.OfType<ThTCHWall>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
    }
}
