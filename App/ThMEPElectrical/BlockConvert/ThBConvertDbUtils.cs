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
                if (ltr == null)
                {
                    return;
                }

                // 如果当前图层等于插入图层，暂不处理
                if (acadDatabase.Database.Clayer.Equals(ltr.ObjectId))
                {
                    return;
                }

                // 设置图层状态
                ltr.IsOff = false;
                ltr.IsFrozen = false;
                ltr.IsLocked = false;
                ltr.IsPlottable = true;
            }
        }
    }
}
