using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertDbUtils
    {
        public static void UpdateLayerSettings(string name)
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
