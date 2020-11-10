using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThModelBlockCustomCommandMapper : ICustomCommandMapper
    {
        public string GetMappedCustomCommand(ObjectId entId)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(entId.Database))
            {
                return string.IsNullOrEmpty(entId.GetModelIdentifier()) ? string.Empty : ThFanSelectionCommon.CMD_MODEL_EDIT;
            }
        }
    }
}
