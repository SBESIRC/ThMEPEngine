using AcHelper;
using Autodesk.AutoCAD.Runtime;

using ThMEPLighting.Command;
using ThMEPLighting.ViewModel;

namespace ThMEPLighting
{
    public class ThMEPGarageLayoutCmd
    {
        public static LightingViewModel UIConfigs;

        [CommandMethod("TIANHUACAD", "THMEPGARAGELAYOUT", CommandFlags.Modal)]
        public void THMEPGARAGELAYOUT()
        {
            if (UIConfigs == null)
            {
                return;
            }

            using (var cmd = new ThLightingLayoutCommand(UIConfigs))
            {
                FocusToCAD();
                cmd.Execute();
            }
        }

        void FocusToCAD()
        {
            //  https://adndevblog.typepad.com/autocad/2013/03/use-of-windowfocus-in-autocad-2014.html
#if ACAD2012
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
#else
            Active.Document.Window.Focus();
#endif
        }
    }
}
