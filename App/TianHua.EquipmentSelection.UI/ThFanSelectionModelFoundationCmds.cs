using AcHelper;
using Autodesk.AutoCAD.Runtime;
using TianHua.FanSelection.UI.CAD;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionModelFoundationCmds
    {
        [CommandMethod("TIANHUACAD", "THEXTRACTMODELFOUNDATION", CommandFlags.Modal)]
        public void ThExtractModelFoundation()
        {
            ThFanSelectionModelFoundationService.CleanAll(Active.Database);
            ThFanSelectionModelFoundationService.Generate(Active.Database);
        }
    }
}
