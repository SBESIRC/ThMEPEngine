using AcHelper;
using Autodesk.AutoCAD.Runtime;
using System.IO;

namespace ThMEPTCH
{
    class ThMEPTCHApp : IExtensionApplication
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
        [CommandMethod("TIANHUACAD", "THTCHPIPIMP", CommandFlags.Modal)]
        public void THTCHImportWaterPipe()
        {
            string cmdName = "TH2T20";
            string TCHDBPath = Path.GetTempPath() + "TG20.db";
#if ACAD_ABOVE_2014
            Active.Editor.Command(cmdName, TCHDBPath, " ");
#else
            ResultBuffer args = new ResultBuffer(
               new TypedValue((int)LispDataType.Text, string.Format("_.{0}", CmdName)),
               new TypedValue((int)LispDataType.Text, TCHDBPath),
               new TypedValue((int)LispDataType.Text, " "));
            Active.Editor.AcedCmd(args);
#endif
        }
    }
}
