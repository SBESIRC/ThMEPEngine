using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanSelectionDbUtils
    {
        public static bool IsModelFoundation(Entity entity)
        {
            return entity.GetXDataForApplication(ThFanSelectionCommon.RegAppName_Model_Foundation) != null;
        }

        public static bool IsModelFoundationLayer(Entity entity)
        {
            return entity.Layer == ThFanSelectionCommon.FOUNDATION_LAYER;
        }
    }
}
