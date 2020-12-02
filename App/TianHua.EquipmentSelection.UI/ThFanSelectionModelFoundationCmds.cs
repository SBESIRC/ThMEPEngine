using AcHelper;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Service.Hvac;

namespace TianHua.FanSelection.UI
{
    public class ThFanSelectionModelFoundationCmds
    {
        [CommandMethod("TIANHUACAD", "THEXTRACTMODELFOUNDATION", CommandFlags.Modal)]
        public void ThExtractModelFoundation()
        {
            ThHvacDbModelFoundationService.CleanAll(Active.Database);
            ThHvacDbModelFoundationService.Generate(Active.Database);
        }
    }
}
