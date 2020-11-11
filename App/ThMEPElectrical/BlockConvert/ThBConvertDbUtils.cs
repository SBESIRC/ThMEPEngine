using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDbUtils
    {
        public static ObjectId BlockLayer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ObjectId layerId = LayerTools.AddLayer(acadDatabase.Database, ThBConvertCommon.LAYER_FAN_DEVICE);
                var ltr = acadDatabase.Element<LayerTableRecord>(layerId, true);
                ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 3);
                return layerId;
            };
        }
    }
}
