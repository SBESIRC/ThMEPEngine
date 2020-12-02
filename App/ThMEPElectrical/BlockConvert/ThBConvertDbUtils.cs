using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDbUtils
    {
        public static ObjectId BlockLayer(string name, short colorIndex)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                LayerTools.AddLayer(acadDatabase.Database, name);
                LayerTools.SetLayerColor(acadDatabase.Database, name, colorIndex);
                UpdateLayerSettings(name);
                return acadDatabase.Layers.ElementOrDefault(name).ObjectId;
            };
        }
        private static void UpdateLayerSettings(string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ltr = acadDatabase.Layers.ElementOrDefault(name, true);
                if (ltr != null)
                {
                    ltr.IsOff = false;
                    ltr.IsFrozen = false;
                    ltr.IsLocked = false;
                    ltr.IsPlottable = true;
                }
            }
        }
    }
}
